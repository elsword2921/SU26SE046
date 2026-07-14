using DAL.Models.Commons;
using DAL.Models.Enum;

namespace DAL.Models
{
    public class Inventory : BaseEntity
    {
        public Guid WarehouseId { get; set; }
        public Guid? AreaGroupId { get; set; }
        public Guid ProfileId { get; set; }
        public int ConditionRating { get; set; }
        public int Quantity { get; set; }
        public decimal TotalWeight { get; set; }
        public virtual Warehouse Warehouse { get; set; } = null!;
        public virtual AreaGroup? AreaGroup { get; set; }
        public virtual Profile Profile { get; set; } = null!;
        public virtual ICollection<TransactionItem> TransactionItems { get; set; } = new List<TransactionItem>();
    }
}
