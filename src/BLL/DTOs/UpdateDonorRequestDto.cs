namespace BLL.DTOs
{
    public class UpdateDonorRequestDto
    {
        public DateTime PickupDate { get; set; }

        public string Description { get; set; }

        public List<string>? ImageUrls { get; set; }

        public decimal EstimateWeight { get; set; }

        public string PickupAddress { get; set; }

        public Guid WarehouseId { get; set; }
    }
}