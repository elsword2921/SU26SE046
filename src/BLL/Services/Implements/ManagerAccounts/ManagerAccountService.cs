using System.Net.Mail;
using System.Text.RegularExpressions;
using BLL.DTOs;
using BLL.Services.Interfaces.ManagerAccounts;
using DAL;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services.Implements.ManagerAccounts;

public partial class ManagerAccountService(AppDbContext context) : IManagerAccountService
{
    private static readonly string[] AllowedRoles =
        ["Donor", "CharityOrganization", "RecyclingOrganization", "DisposalOrganization", "ReceivingStaff", "ClassificationStaff", "WarehouseStaff"];
    private static readonly string[] WarehouseRoles = ["ReceivingStaff", "ClassificationStaff", "WarehouseStaff"];

    public async Task<ManagerAccountPageDto> SearchAsync(Guid? warehouseId, string? role, string? search, int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 100);
        var query = context.Users.AsNoTracking().Include(x => x.Role).Include(x => x.Warehouse)
            .Where(x => x.IsActive != false && AllowedRoles.Contains(x.Role.RoleName));
        if (warehouseId.HasValue) query = query.Where(x => x.WarehouseId == warehouseId);
        if (!string.IsNullOrWhiteSpace(role)) query = query.Where(x => x.Role.RoleName == role);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x => x.FullName.ToLower().Contains(term) || x.PhoneNumber.Contains(term));
        }
        var total = await query.CountAsync();
        var items = await query.OrderBy(x => x.FullName).ThenBy(x => x.UserName)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new ManagerAccountDto(x.Id, x.FullName, x.UserName, x.Email, x.PhoneNumber,
                x.Role.RoleName, x.WarehouseId, x.Warehouse != null ? x.Warehouse.WarehouseName : null,
                x.Address, x.UserStatus, x.AvatarUrl, x.CreateAt)).ToListAsync();
        var roles = await context.Roles.AsNoTracking().Where(x => x.IsActive != false && AllowedRoles.Contains(x.RoleName))
            .OrderBy(x => x.RoleName).Select(x => new ManagerRoleOptionDto(x.Id, x.RoleName)).ToListAsync();
        return new ManagerAccountPageDto(items, total, page, pageSize, roles);
    }

    public async Task<Guid> CreateAsync(Guid managerId, CreateManagerAccountDto dto)
    {
        var role = await ValidateAsync(null, dto.FullName, dto.UserName, dto.Email, dto.PhoneNumber,
            dto.Password, dto.RoleId, dto.WarehouseId, dto.Address);
        var now = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(), FullName = dto.FullName.Trim(), UserName = dto.UserName.Trim(),
            Email = dto.Email.Trim().ToLower(), PhoneNumber = dto.PhoneNumber.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password), RoleId = role.Id,
            WarehouseId = WarehouseRoles.Contains(role.RoleName) ? dto.WarehouseId : null,
            Address = dto.Address.Trim(), UserStatus = "Active", EmailConfirmed = true,
            IsActive = true, CreateAt = now, CreatedBy = managerId
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user.Id;
    }

    public async Task UpdateAsync(Guid managerId, Guid userId, UpdateManagerAccountDto dto)
    {
        var user = await context.Users.Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == userId && x.IsActive != false)
            ?? throw new InvalidOperationException("Account not found.");
        EnsureManagedRole(user.Role.RoleName);
        var role = await ValidateAsync(userId, dto.FullName, dto.UserName, dto.Email, dto.PhoneNumber,
            dto.NewPassword, dto.RoleId, dto.WarehouseId, dto.Address);
        if (dto.UserStatus is not ("Active" or "Inactive"))
            throw new InvalidOperationException("Account status must be Active or Inactive.");
        user.FullName = dto.FullName.Trim();
        user.UserName = dto.UserName.Trim();
        user.Email = dto.Email.Trim().ToLower();
        user.PhoneNumber = dto.PhoneNumber.Trim();
        user.RoleId = role.Id;
        user.WarehouseId = WarehouseRoles.Contains(role.RoleName) ? dto.WarehouseId : null;
        user.Address = dto.Address.Trim();
        user.UserStatus = dto.UserStatus;
        if (!string.IsNullOrWhiteSpace(dto.NewPassword))
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.UpdateAt = DateTime.UtcNow;
        user.UpdatedBy = managerId;
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid managerId, Guid userId)
    {
        var user = await context.Users.Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == userId && x.IsActive != false)
            ?? throw new InvalidOperationException("Account not found.");
        EnsureManagedRole(user.Role.RoleName);
        user.IsActive = false;
        user.UserStatus = "Deleted";
        user.DeleteAt = DateTime.UtcNow;
        user.DeletedBy = managerId;
        await context.SaveChangesAsync();
    }

    public async Task SetLockedAsync(Guid managerId, Guid userId, bool locked)
    {
        var user = await context.Users.Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == userId && x.IsActive != false)
            ?? throw new InvalidOperationException("Account not found.");
        EnsureManagedRole(user.Role.RoleName);
        user.UserStatus = locked ? "Inactive" : "Active";
        user.UpdateAt = DateTime.UtcNow;
        user.UpdatedBy = managerId;
        await context.SaveChangesAsync();
    }

    private async Task<Role> ValidateAsync(Guid? userId, string fullName, string userName, string email,
        string phone, string? password, Guid roleId, Guid? warehouseId, string address)
    {
        if (string.IsNullOrWhiteSpace(fullName) || fullName.Trim().Length < 2)
            throw new InvalidOperationException("Full name is required.");
        if (!UserNameRegex().IsMatch(userName.Trim()))
            throw new InvalidOperationException("Username must contain 4-50 letters, numbers, dots or underscores.");
        try { _ = new MailAddress(email.Trim()); }
        catch { throw new InvalidOperationException("Email format is invalid."); }
        if (!PhoneRegex().IsMatch(phone.Trim()))
            throw new InvalidOperationException("Phone number must contain 10 digits and start with 0.");
        if (!string.IsNullOrWhiteSpace(password)
            && (password.Length < 8 || !password.Any(char.IsUpper) || !password.Any(char.IsDigit)
                || password.All(char.IsLetterOrDigit)))
            throw new InvalidOperationException("Password must have at least 8 characters, an uppercase letter, a number and a special character.");
        if (!userId.HasValue && string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Password is required.");
        if (string.IsNullOrWhiteSpace(address))
            throw new InvalidOperationException("Address is required.");

        var normalizedUserName = userName.Trim().ToLower();
        var normalizedEmail = email.Trim().ToLower();
        var normalizedPhone = phone.Trim();
        if (await context.Users.AnyAsync(x => x.Id != userId && x.IsActive != false
            && x.UserName.ToLower() == normalizedUserName))
            throw new InvalidOperationException("Username is already in use.");
        if (await context.Users.AnyAsync(x => x.Id != userId && x.IsActive != false
            && x.Email.ToLower() == normalizedEmail))
            throw new InvalidOperationException("Email is already in use.");
        if (await context.Users.AnyAsync(x => x.Id != userId && x.IsActive != false
            && x.PhoneNumber == normalizedPhone))
            throw new InvalidOperationException("Phone number is already in use.");

        var role = await context.Roles.FirstOrDefaultAsync(x => x.Id == roleId && x.IsActive != false)
            ?? throw new InvalidOperationException("Role not found.");
        EnsureManagedRole(role.RoleName);
        if (WarehouseRoles.Contains(role.RoleName)
            && (!warehouseId.HasValue || !await context.Warehouses.AnyAsync(x => x.Id == warehouseId && x.IsActive != false)))
            throw new InvalidOperationException("An active warehouse is required for this staff role.");
        return role;
    }

    private static void EnsureManagedRole(string role)
    {
        if (!AllowedRoles.Contains(role))
            throw new InvalidOperationException("Manager cannot manage Admin or Manager accounts.");
    }

    [GeneratedRegex(@"^[A-Za-z0-9._]{4,50}$")]
    private static partial Regex UserNameRegex();
    [GeneratedRegex(@"^0\d{9}$")]
    private static partial Regex PhoneRegex();
}
