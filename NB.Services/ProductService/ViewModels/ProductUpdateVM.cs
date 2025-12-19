using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;


namespace NB.Service.ProductService.ViewModels
{
    public class ProductUpdateVM
    {
        public string? code { get; set; }
        public string? productName { get; set; }
        public int? supplierId { get; set; }
        public int? categoryId { get; set; }
        public string? description { get; set; }
        public bool? isAvailable { get; set; }
        public IFormFile? image { get; set; }
        public decimal? weightPerUnit { get; set; }
        public decimal? sellingPrice { get; set; }
        public DateTime? updatedAt { get; set; }
    }
}
