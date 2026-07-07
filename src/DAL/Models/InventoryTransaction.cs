using DAL.Models.Commons;

namespace DAL.Models
{
    public class InventoryTransaction : BaseEntity
    {
        public Inventory Inventory { get; set; }
        public Guid InventoryId { get; set; }
        public IntakeBatch IntakeBatch { get; set; }
        public Guid IntakeBatchId { get; set; }
        public int Quantity { get; set; }
        public int PrevQuantity { get; set; }
        public int NewQuantity { get; set; }
    }
}
