using DAL.Models.Commons;
using DAL.Models.Enum;

namespace DAL.Models
{
    public class Inventory : BaseEntity
    {
        public List<Guid> ClassificationItemId { get; set; }
        public List<Guid> IntakeBatchId { get; set; }
        public Guid CategoryId { get; set; }
        public Category Category { get; set; }
        public ClothCondition ClothCondition { get; set; }  
        public ClothGender ClothGender { get; set; }
        public ClothSize ClothSize { get; set; }
        public ClothTargetAge ClothTargetUser { get; set; }
        public int TotalCloth { get; set; }
    }
}
