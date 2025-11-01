using NB.Model.Entities;
using NB.Service.Common;
using NB.Service.InventoryService.Dto;
using NB.Service.ProductService.Dto;
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

        Task<ProductDto?> GetById(int id);
        Task<ProductDto?> GetByProductId(int id);

        Task<List<ProductDto>> GetByIds(List<int> ids);

        Task<List<ProductDto>> GetByInventory(List<InventoryDto> list);

        Task<List<ProductDto>> GetData();

        Task<List<ProductDetailDto>> GetDataWithDetails();

        Task<List<ProductInWarehouseDto>> GetProductsByWarehouseId(int warehouseId);

        Task<ProductDto?> GetByCode(string code);

        Task<ProductDto?> GetByProductName(string productName);

        Task<PagedList<ProductDto>> GetData(ProductSearch search);

    }
}
