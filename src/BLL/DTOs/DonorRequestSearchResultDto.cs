namespace BLL.DTOs
{
    public class DonorRequestSearchResultDto
    {
        public Guid Id { get; set; }

        public string Code { get; set; } = string.Empty;

        public string DonorName { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public string? Description { get; set; }

        public List<string>? ImageUrls { get; set; }

        public decimal EstimateWeight { get; set; }

        public decimal? ActualWeight { get; set; }

        public string PickupAddress { get; set; } = string.Empty;

        public DateTime? PickupDate { get; set; }

        public Guid WarehouseId { get; set; }

        public string WarehouseAddress { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string StatusText { get; set; } = string.Empty;

        public DateTime? CreatedAt { get; set; }
    }
}