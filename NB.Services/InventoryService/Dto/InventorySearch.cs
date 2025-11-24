using NB.Service.Dto;

namespace NB.Service.InventoryService.Dto
{
    public class InventorySearch : SearchBase
    {
        public int? WarehouseId { get; set; }
        public int? ProductId { get; set; }
        public string? ProductName { get; set; }
    }
}
