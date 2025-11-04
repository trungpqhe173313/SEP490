using NB.Service.Dto;


namespace NB.Service.ProductService.Dto
{
    public  class ProductSearch : SearchBase
    {
        public string? ProductName { get; set; }

        public int? SupplierId { get; set; }

        public bool? IsAvailable { get; set; }
    }
}
