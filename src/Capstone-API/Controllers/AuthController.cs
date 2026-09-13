using BLL.DTOs;
using BLL.Services.Interfaces.AuthService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Capstone_API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IWebHostEnvironment environment;

        public AuthController(IAuthService authService, IWebHostEnvironment environment)
        {
            _authService = authService;
            this.environment = environment;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            return Ok(await _authService.RegisterAsync(request));
        }

        [HttpPost("register-organization")]
        [RequestSizeLimit(6_000_000)]
        [AllowAnonymous]
        public async Task<IActionResult> RegisterOrganization(
            [FromForm] RegisterOrganizationRequest request, IFormFile? certificateFile)
        {
            if (certificateFile is null || certificateFile.Length == 0)
                throw new InvalidOperationException("A certificate file is required.");
            if (certificateFile.Length > 5_000_000)
                throw new InvalidOperationException("Certificate file must not exceed 5MB.");
            var extension = Path.GetExtension(certificateFile.FileName).ToLowerInvariant();
            if (extension is not (".pdf" or ".jpg" or ".jpeg" or ".png"))
                throw new InvalidOperationException("Certificate file must be a PDF, JPG, JPEG or PNG file.");
            var folder = Path.Combine(environment.ContentRootPath, "wwwroot", "uploads", "certificates");
            Directory.CreateDirectory(folder);
            var fileName = $"{Guid.NewGuid():N}{extension}";
            await using (var stream = System.IO.File.Create(Path.Combine(folder, fileName)))
                await certificateFile.CopyToAsync(stream);
            request.CertificateImageUrl = $"/uploads/certificates/{fileName}";
            return Ok(await _authService.RegisterOrganizationAsync(request));
        }

        [HttpPost("verify-registration")]
        public async Task<IActionResult> VerifyRegistration(VerifyRegistrationRequest request) =>
            Ok(await _authService.VerifyRegistrationAsync(request));

        [HttpPost("resend-verification")]
        public async Task<IActionResult> ResendVerification(ResendVerificationRequest request)
        {
            await _authService.ResendVerificationAsync(request);
            return Ok(new { message = "A new verification code was sent." });
        }

        [HttpPost("login")]
        public async Task<AuthResponse> Login(LoginRequest request)
        {
            return await _authService.LoginAsync(request);
        }

        [HttpPost("forgot-password")]
        public async Task<ForgotPasswordResponse> ForgotPassword(ForgotPasswordRequest request) =>
            await _authService.ForgotPasswordAsync(request);

        [HttpPost("reset-password")]
        public async Task<ResetPasswordResponse> ResetPassword(ResetPasswordRequest request) =>
            await _authService.ResetPasswordAsync(request);

        [Authorize]
        [HttpGet("me")]
        public async Task<CurrentUserProfileDto> Me()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            return await _authService.GetCurrentUserProfileAsync(userId);
        }
    }
}
