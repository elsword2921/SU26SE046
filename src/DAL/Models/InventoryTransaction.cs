using DAL.Models.Commons;

namespace DAL.Models
{
    public class InventoryTransaction : BaseEntity
    {
        public Guid WarehouseId { get; set; }
        public string TransactionCode { get; set; } = string.Empty;
        public string TransactionType { get; set; } = string.Empty;
        public string? ReferenceType { get; set; }
        public Guid? ReferenceId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public virtual Warehouse Warehouse { get; set; } = null!;
        public virtual ICollection<TransactionItem> Items { get; set; } = new List<TransactionItem>();
    }
}
