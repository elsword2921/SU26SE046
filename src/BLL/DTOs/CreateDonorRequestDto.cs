namespace BLL.DTOs
{
    public class CreateDonorRequestDto
    {
        public DateTime? PickupDate { get; set; }

        public string ContactName { get; set; } = string.Empty;

        public string ContactPhoneNumber { get; set; } = string.Empty;

        public string DeliveryMethod { get; set; } = "StaffPickup";

        public Guid? WarehouseId { get; set; }

        public string? DropOffMethod { get; set; }

        public string? CarrierName { get; set; }

        public string? TrackingCode { get; set; }

        public string Description { get; set; }

        public List<string>? ImageUrls { get; set; }

        public decimal EstimateWeight { get; set; }

        public string? PickupAddress { get; set; }

        public double? PickupLatitude { get; set; }

        public double? PickupLongitude { get; set; }
    }
}
