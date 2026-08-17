namespace BLL.DTOs;

public class UpdateShippingInfoDto
{
    public string CarrierName { get; set; } = string.Empty;
    public string TrackingCode { get; set; } = string.Empty;
    public DateTime ExpectedArrivalAt { get; set; }
}
