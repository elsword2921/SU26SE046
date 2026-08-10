using DAL.Models;
using DAL.Models.Enum;

namespace BLL.DTOs
{
    public class CreateVoucherDto
    {
        public string Name { get; set; } = string.Empty;
        public string PartnerName { get; set; } = string.Empty;
        public string? VoucherUrl { get; set; }
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
        public string? TermsAndConditions { get; set; }
        public decimal Value { get; set; }
        public int RequiredPoints { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime ExpireDate { get; set; }
    }

    public class UpdateVoucherDto
    {
        public string Name { get; set; } = string.Empty;
        public string PartnerName { get; set; } = string.Empty;
        public string? VoucherUrl { get; set; }
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
        public string? TermsAndConditions { get; set; }
        public decimal Value { get; set; }
        public int RequiredPoints { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime ExpireDate { get; set; }
    }

    public class UpdateVoucherStatusDto
    {
        public VoucherStatus Status { get; set; }
    }

    public class AddVoucherCodesDto
    {
        public List<CreateVoucherCodeDto> Codes { get; set; } = new();
    }

    public class CreateVoucherCodeDto
    {
        public string Code { get; set; } = string.Empty;
        public DateTime ExpireDate { get; set; }
    }

    public class VoucherDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string PartnerName { get; set; } = string.Empty;
        public string? VoucherUrl { get; set; }
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
        public string? TermsAndConditions { get; set; }
        public decimal Value { get; set; }
        public int RequiredPoints { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime ExpireDate { get; set; }
        public VoucherStatus Status { get; set; }
        public int AvailableQuantity { get; set; }
    }

    public class VoucherCodeDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public DateTime ExpireDate { get; set; }
        public VoucherCodeStatus Status { get; set; }
        public Guid? RedeemedByUserId { get; set; }
        public DateTime? RedeemedAt { get; set; }
    }

    public class RedeemVoucherResultDto
    {
        public Guid VoucherId { get; set; }
        public string VoucherName { get; set; } = string.Empty;
        public string PartnerName { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public int PointsSpent { get; set; }
        public int RemainingPoints { get; set; }
        public DateTime ExpireDate { get; set; }
    }

    public class MyVoucherDto
    {
        public Guid VoucherCodeId { get; set; }
        public Guid VoucherId { get; set; }
        public string VoucherName { get; set; } = string.Empty;
        public string PartnerName { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public DateTime RedeemedAt { get; set; }
        public DateTime ExpireDate { get; set; }
    }

    public class VoucherRedemptionDto
    {
        public Guid Id { get; set; }
        public Guid VoucherId { get; set; }
        public string VoucherName { get; set; } = string.Empty;
        public int PointsSpent { get; set; }
        public DateTime RedeemedAt { get; set; }
    }
}