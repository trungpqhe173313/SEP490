using NB.Model.Entities;
using NB.Repository.WarehouseRepository.Dto;
using NB.Service.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.WarehouseService
{
    public interface IWarehouseService : IService<Warehouse>
    {
        Task<PagedList<WarehouseProductDto>> GetProducts(WarehouseProductSearch search);
        Task<WarehouseDto?> GetDto(int id);
        Task<Warehouse> Create(Warehouse warehouse);
        Task<Warehouse> Update(int id, Warehouse warehouse);
        Task<bool> Delete(int id);
    }
}
