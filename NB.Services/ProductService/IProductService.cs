using NB.Model.Entities;
using NB.Service.Common;
using NB.Service.ProductService.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.ProductService
{
    public interface IProductService : IService<Product>
    {
       
        Task<Product?> GetProductById(int id);

    }
}
