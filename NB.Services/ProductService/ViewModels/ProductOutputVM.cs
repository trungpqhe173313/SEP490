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

        // Khóa ngoại
        public int SupplierId { get; set; }
        public int CategoryId { get; set; }

        // Thông tin cơ bản của sản phẩm
        public string Code { get; set; } = null!;
        public string ProductName { get; set; } = null!;
        public decimal? Price { get; set; }
        public int? StockQuantity { get; set; }

        public decimal weightPerUnit { get; set; }

        // Thời gian tạo
        public DateTime? CreatedAt { get; set; }
    }
}
