using System.ComponentModel.DataAnnotations;


namespace NB.Service.ProductService.ViewModels
{
    public class ProductCreateVM
    {
        public int WarehouseId { get; set; }
        public int CategoryId { get; set; }
        [Required(ErrorMessage = "Mã sản phẩm không được trống")]
        public string Code { get; set; } = null!;
        [Required(ErrorMessage = "Tên sản phẩm không được trống")]
        public int SupplierId { get; set; }
        public string ProductName { get; set; } = null!;
        public string Description { get; set; } = null!;
        public decimal? Quantity { get; set; }

        public string ImageUrl { get; set; } = null!;
        public decimal WeightPerUnit { get; set; }

    }
}
