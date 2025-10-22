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
                        where p.Id == id
                        select new ProductDto
                        {
                            Id = p.Id,
                            Name = p.Name,
                            Code = p.Code,
                            Price = p.Price,
                            StockQuantity = p.StockQuantity,
                            CreatedAt = p.CreatedAt,
                            UpdatedAt = p.UpdatedAt
                        };
            return await Task.FromResult(query.FirstOrDefault());
        }
    }
}
