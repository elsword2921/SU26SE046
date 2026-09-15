namespace BLL.DTOs;

public record ManagerRoleOptionDto(Guid Id, string Name);
public record ManagerAccountDto(Guid Id, string FullName, string UserName, string Email,
    string PhoneNumber, string Role, Guid? WarehouseId, string? WarehouseName,
    string Address, string UserStatus, string? AvatarUrl, DateTime? CreatedAt);
public record ManagerAccountPageDto(IReadOnlyList<ManagerAccountDto> Items, int TotalCount,
    int Page, int PageSize, IReadOnlyList<ManagerRoleOptionDto> Roles);

public class CreateManagerAccountDto
{
    public string FullName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public Guid RoleId { get; set; }
    public Guid? WarehouseId { get; set; }
    public string Address { get; set; } = string.Empty;
}

public class UpdateManagerAccountDto
{
    public string FullName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public Guid RoleId { get; set; }
    public Guid? WarehouseId { get; set; }
    public string Address { get; set; } = string.Empty;
    public string UserStatus { get; set; } = "Active";
    public string? NewPassword { get; set; }
}

public record SetManagerAccountStatusDto(bool Locked);
