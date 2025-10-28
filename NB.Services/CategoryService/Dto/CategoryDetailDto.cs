using NB.Service.ProductService.Dto;
using System;
using System.Collections.Generic;

namespace NB.Service.CategoryService.Dto
{
    public class CategoryDetailDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = null!;
        public string? Description { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdateAt { get; set; }

        // Danh sách products thuộc category này với SupplierName và CategoryName
        public List<ProductDetailDto> Products { get; set; } = new List<ProductDetailDto>();
    }
}
