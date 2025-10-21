using NB.Model.Entities;
using NB.Repository.Common;
using NB.Service.Common;
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

        public async Task<Product?> GetProductById(int id)
        {            
            return await base.GetByIdAsync(id);
        }
    }
}
