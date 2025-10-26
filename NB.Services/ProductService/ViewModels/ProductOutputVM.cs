using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.ProductService.ViewModels
{
    public class ProductOutputVM
    {
        public int ProductId { get; set; }
        public string Code { get; set; } = null!;
        public string ProductName { get; set; } = null!;
        public int SupplierId { get; set; }
        public int CategoryId { get; set; }
        public string? Description { get; set; }
        public decimal? Quantity { get; set; }

        public decimal? WeightPerUnit { get; set; }
       // Thời gian tạo
        public DateTime? CreatedAt { get; set; }
    }
}
