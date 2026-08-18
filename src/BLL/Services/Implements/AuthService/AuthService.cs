using System.IdentityModel.Tokens.Jwt;
using System.Security.Authentication;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using BLL.Common;
using BLL.DTOs;
using BLL.Services.Interfaces.AuthService;
using DAL;
using DAL.Models;
using DAL.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace BLL.Services.Implements.AuthService;

public partial class AuthService(
    IUnitOfWork unitOfWork, AppDbContext dbContext, IConfiguration configuration,
    IEmailVerificationSender emailSender) : IAuthService
{
    private const string RegistrationPurpose = "Registration";
    private const string PasswordResetPurpose = "PasswordReset";

    public async Task<CurrentUserProfileDto> GetCurrentUserProfileAsync(Guid userId)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .Include(x => x.Role)
            .Include(x => x.Warehouse)
            .SingleOrDefaultAsync(x => x.Id == userId && x.IsActive == true)
            ?? throw new KeyNotFoundException("User account was not found.");

        return new CurrentUserProfileDto(
            user.Id,
            user.FullName,
            user.UserName,
            user.Email,
            user.PhoneNumber,
            user.Address,
            user.Role.RoleName,
            user.UserStatus,
            user.AvatarUrl,
            user.WarehouseId,
            user.Warehouse?.WarehouseName,
            user.Warehouse?.Address,
            user.EmailConfirmed,
            user.CreateAt);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var name = request.UserName.Trim().ToLowerInvariant();
        var user = await unitOfWork.UserRepository.GetWithConditionAsync(
            x => x.UserName.ToLower() == name, false, x => x.Role);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid username or password.");
        if (!user.EmailConfirmed || user.UserStatus != "Active")
            throw new AuthenticationException("Account verification is incomplete. Please verify your email.");

        return new AuthResponse
        {
            Token = GenerateToken(user), ExpiredAt = DateTime.UtcNow.AddHours(2),
            UserId = user.Id, FullName = user.FullName, UserName = user.UserName,
            AvatarUrl = user.AvatarUrl, Role = user.Role.RoleName
        };
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
        NormalizeAndValidate(request);
        var name = request.UserName.ToLowerInvariant();
        var email = request.Email.ToLowerInvariant();
        if (await dbContext.Users.AnyAsync(x => x.UserName.ToLower() == name))
            throw new InvalidOperationException("Username already exists.");
        if (await dbContext.Users.AnyAsync(x => x.Email.ToLower() == email))
            throw new InvalidOperationException("Email already exists.");
        if (await dbContext.Users.AnyAsync(x => x.PhoneNumber == request.PhoneNumber))
            throw new InvalidOperationException("Phone number already exists.");

        var role = await unitOfWork.RoleRepository.GetWithConditionAsync(x => x.RoleName == "Donor")
            ?? throw new InvalidOperationException("Donor role is not configured.");
        var user = new User
        {
            FullName = request.FullName, UserName = request.UserName, Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password), RoleId = role.Id,
            Address = request.Address, PhoneNumber = request.PhoneNumber,
            UserStatus = "PendingVerification", EmailConfirmed = false,
            IsActive = false, CreateAt = VietnamTime.Now
        };
        dbContext.Users.Add(user);
        var code = CreateCode(user.Id, RegistrationPurpose);
        await dbContext.SaveChangesAsync();
        try
        {
            await emailSender.SendAsync(user.Email, user.FullName, code);
        }
        catch
        {
            throw new InvalidOperationException("Account was created, but verification delivery failed. Please resend the verification codes.");
        }
        return new RegisterResponse(user.Id,
            "Registration successful. A verification code was sent by email.");
    }

    public async Task<VerificationResponse> VerifyRegistrationAsync(VerifyRegistrationRequest request)
    {
        if (!SixDigitCodeRegex().IsMatch(request.Code ?? ""))
            throw new InvalidOperationException("Verification code must contain exactly 6 digits.");
        var user = await dbContext.Users.FindAsync(request.UserId)
            ?? throw new InvalidOperationException("Account was not found.");
        var verification = await dbContext.UserVerificationCodes
            .Where(x => x.UserId == request.UserId && x.Purpose == RegistrationPurpose && x.IsActive == true)
            .OrderByDescending(x => x.CreateAt).FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("No active verification code was found.");
        if (verification.ExpiresAt <= VietnamTime.Now)
            throw new InvalidOperationException("Verification code has expired. Please request a new code.");
        if (verification.FailedAttempts >= 5)
            throw new InvalidOperationException("Too many failed attempts. Please request a new code.");
        if (!CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(verification.CodeHash),
                Convert.FromHexString(HashCode(request.Code))))
        {
            verification.FailedAttempts++;
            await dbContext.SaveChangesAsync();
            throw new InvalidOperationException("Verification code is incorrect.");
        }

        verification.VerifiedAt = VietnamTime.Now;
        verification.IsActive = false;
        user.EmailConfirmed = true;
        var activated = user.EmailConfirmed;
        if (activated) { user.UserStatus = "Active"; user.IsActive = true; }
        user.UpdateAt = VietnamTime.Now;
        await dbContext.SaveChangesAsync();
        return new VerificationResponse(user.EmailConfirmed, activated,
            activated ? "Account verification completed." : "Email verification failed.");
    }

    public async Task ResendVerificationAsync(ResendVerificationRequest request)
    {
        var user = await dbContext.Users.FindAsync(request.UserId)
            ?? throw new InvalidOperationException("Account was not found.");
        if (user.EmailConfirmed)
            throw new InvalidOperationException("Email is already verified.");
        var latest = await dbContext.UserVerificationCodes.Where(x => x.UserId == request.UserId)
            .Where(x => x.Purpose == RegistrationPurpose)
            .OrderByDescending(x => x.CreateAt).FirstOrDefaultAsync();
        if (latest?.CreateAt > VietnamTime.Now.AddMinutes(-1))
            throw new InvalidOperationException("Please wait one minute before requesting another code.");
        await dbContext.UserVerificationCodes
            .Where(x => x.UserId == request.UserId && x.Purpose == RegistrationPurpose && x.IsActive == true)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, false));
        var code = CreateCode(user.Id, RegistrationPurpose);
        await dbContext.SaveChangesAsync();
        await emailSender.SendAsync(user.Email, user.FullName, code);
    }

    public async Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        const string message = "If the email belongs to an active account, a password reset code has been sent.";
        if (!EmailRegex().IsMatch(email) || email.Length > 254)
            return new ForgotPasswordResponse(message);

        var user = await dbContext.Users.SingleOrDefaultAsync(x =>
            x.Email.ToLower() == email && x.EmailConfirmed && x.IsActive == true && x.UserStatus == "Active");
        if (user is null) return new ForgotPasswordResponse(message);

        var latest = await dbContext.UserVerificationCodes
            .Where(x => x.UserId == user.Id && x.Purpose == PasswordResetPurpose)
            .OrderByDescending(x => x.CreateAt).FirstOrDefaultAsync();
        if (latest?.CreateAt > VietnamTime.Now.AddMinutes(-1))
            return new ForgotPasswordResponse(message);

        await dbContext.UserVerificationCodes
            .Where(x => x.UserId == user.Id && x.Purpose == PasswordResetPurpose && x.IsActive == true)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, false));
        var code = CreateCode(user.Id, PasswordResetPurpose);
        await dbContext.SaveChangesAsync();
        await emailSender.SendPasswordResetAsync(user.Email, user.FullName, code);
        return new ForgotPasswordResponse(message);
    }

    public async Task<ResetPasswordResponse> ResetPasswordAsync(ResetPasswordRequest request)
    {
        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        var code = request.Code ?? string.Empty;
        if (!EmailRegex().IsMatch(email) || !SixDigitCodeRegex().IsMatch(code))
            throw new InvalidOperationException("Email or verification code is invalid.");
        ValidatePassword(request.NewPassword);

        var user = await dbContext.Users.SingleOrDefaultAsync(x =>
            x.Email.ToLower() == email && x.EmailConfirmed && x.IsActive == true && x.UserStatus == "Active")
            ?? throw new InvalidOperationException("Email or verification code is invalid.");
        var verification = await dbContext.UserVerificationCodes
            .Where(x => x.UserId == user.Id && x.Purpose == PasswordResetPurpose && x.IsActive == true)
            .OrderByDescending(x => x.CreateAt).FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("Email or verification code is invalid.");
        if (verification.ExpiresAt <= VietnamTime.Now)
            throw new InvalidOperationException("Verification code has expired. Please request a new code.");
        if (verification.FailedAttempts >= 5)
            throw new InvalidOperationException("Too many failed attempts. Please request a new code.");
        if (!CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(verification.CodeHash),
                Convert.FromHexString(HashCode(code))))
        {
            verification.FailedAttempts++;
            await dbContext.SaveChangesAsync();
            throw new InvalidOperationException("Email or verification code is invalid.");
        }

        verification.VerifiedAt = VietnamTime.Now;
        verification.IsActive = false;
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdateAt = VietnamTime.Now;
        await dbContext.SaveChangesAsync();
        return new ResetPasswordResponse("Password reset successfully. You can now sign in with your new password.");
    }

    private string CreateCode(Guid userId, string purpose)
    {
        var code = RandomNumberGenerator.GetInt32(1_000_000).ToString("D6");
        dbContext.UserVerificationCodes.Add(new UserVerificationCode
        {
            UserId = userId, CodeHash = HashCode(code), Purpose = purpose,
            ExpiresAt = VietnamTime.Now.AddMinutes(5), CreateAt = VietnamTime.Now, IsActive = true
        });
        return code;
    }
    private static string HashCode(string code) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code)));
    private static void NormalizeAndValidate(RegisterRequest r)
    {
        r.FullName = r.FullName.Trim(); r.UserName = r.UserName.Trim();
        r.Email = r.Email.Trim().ToLowerInvariant();
        r.PhoneNumber = Regex.Replace(r.PhoneNumber ?? "", @"[\s.\-()]", "");
        r.Address = r.Address.Trim();
        if (!UserNameRegex().IsMatch(r.UserName))
            throw new InvalidOperationException("Username must be 3-30 characters and contain only letters, numbers, dots or underscores.");
        if (!EmailRegex().IsMatch(r.Email) || r.Email.Length > 254)
            throw new InvalidOperationException("Email format is invalid.");
        if (!VietnamPhoneRegex().IsMatch(r.PhoneNumber))
            throw new InvalidOperationException("Phone number must be a valid Vietnamese mobile number.");
        if (r.PhoneNumber.StartsWith("+84")) r.PhoneNumber = "0" + r.PhoneNumber[3..];
        if (r.FullName.Length is < 2 or > 100) throw new InvalidOperationException("Full name must contain 2-100 characters.");
        ValidatePassword(r.Password);
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < 8 || !password.Any(char.IsUpper) ||
            !password.Any(char.IsDigit) || password.All(char.IsLetterOrDigit))
            throw new InvalidOperationException("Password must be at least 8 characters and include an uppercase letter, a number and a special character.");
    }

    private string GenerateToken(User user)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName), new(ClaimTypes.Role, user.Role.RoleName), new("username", user.UserName) };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
        var token = new JwtSecurityToken(configuration["Jwt:Issuer"], configuration["Jwt:Audience"], claims,
            expires: DateTime.UtcNow.AddHours(2), signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [GeneratedRegex(@"^[A-Za-z0-9._]{3,30}$")] private static partial Regex UserNameRegex();
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase)] private static partial Regex EmailRegex();
    [GeneratedRegex(@"^(?:\+84|0)(?:3|5|7|8|9)\d{8}$")] private static partial Regex VietnamPhoneRegex();
    [GeneratedRegex(@"^\d{6}$")] private static partial Regex SixDigitCodeRegex();
}
