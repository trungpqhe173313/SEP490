using NB.Model.Entities;
using NB.Service.WarehouseService.Dto;
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
        Task<List<WarehouseDto?>> GetData();
        Task<WarehouseDto?> GetById(int id);
        Task<WarehouseDto?> GetByWarehouseStatus(int status);

    }
}
