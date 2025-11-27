using NB.Model.Entities;
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
        public string SupplierName { get; set; } = null!;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = null!;
        public string ImageUrl { get; set; } = null!;
        public string? Description { get; set; }
        public decimal? SellingPrice { get; set; }
        public bool? IsAvailable { get; set; }
        public decimal? WeightPerUnit { get; set; }
       // Thời gian tạo
        public DateTime? CreatedAt { get; set; }
    }
}
