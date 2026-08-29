using DAL.Models.Commons;

namespace DAL.Models;

public class DonationPointRule : BaseEntity
{
    public int PointsPerKg { get; set; }
}
