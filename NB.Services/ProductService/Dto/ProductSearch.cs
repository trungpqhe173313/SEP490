using NB.Service.Dto;


namespace NB.Service.ProductService.Dto
{
    public  class ProductSearch : SearchBase
    {
        public int? WarehouseId { get; set; }

        public string? ProductName { get; set; }

        public int? SupplierId { get; set; }

        public bool? IsAvailable { get; set; }

        public int? CategoryId { get; set; }

        public decimal? MinWeightPerUnit { get; set; }

        public decimal? MaxWeightPerUnit { get; set; }

        public DateTime? CreatedFrom { get; set; }

        public DateTime? CreatedTo { get; set; }
    }
}
