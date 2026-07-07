using DAL.Models.Commons;
using DAL.Models.Enum;

namespace DAL.Models
{
    public class ClassificationItem : BaseEntity
    {
        public IntakeBatch IntakeBatch { get; set; }
        public Guid IntakeBatchId { get; set; }
        public int MyProperty { get; set; }
        public ClothCondition ClothCondition { get; set; }
        public ClothGender ClothGender { get; set; }
        public ClothSize ClothSize { get; set; }
        public ClothTargetAge ClothTargetUser { get; set; }
        public int Quantity { get; set; }
        public string? DamagedNote { get; set; }
        public DateTime ClassifyAt { get; set; }
        public Category Category { get; set; }
        public Guid CategoryId { get; set; }
    }
}
