using NB.Model.Entities;
using NB.Repository.Common;
using NB.Service.Common;
using NB.Service.ProductService.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.ProductService
{
    public class ProductService : Service<Product>, IProductService
    {
        public ProductService(IRepository<Product> serviceProvider) : base(serviceProvider)
        {
        }

        public async Task<ProductDto?> GetProductById(int id)
        {            
            var query = from p in GetQueryable()
                        where p.ProductId == id
                        select new ProductDto
                        {
                            ProductName = p.ProductName,
                            Code = p.Code,
                            CategoryId = p.CategoryId,
                            ImageUrl = p.ImageUrl,
                            Description = p.Description,
                            WeightPerUnit = p.WeightPerUnit,
                            IsAvailable = p.IsAvailable,
                            CreatedAt = p.CreatedAt,
                            UpdatedAt = p.UpdatedAt
                        };
            return await Task.FromResult(query.FirstOrDefault());
        }

    }
}
