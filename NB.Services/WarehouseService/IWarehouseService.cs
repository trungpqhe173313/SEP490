using NB.Model.Entities;
using NB.Service.WarehouseService.Dto;
using NB.Service.Common;
using NB.Services.WarehouseService.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.WarehouseService
{
    public interface IWarehouseService : IService<Warehouse>
    {
        Task<PagedList<WarehouseDto?>> GetData(WarehouseSearch search);
        Task<WarehouseDto?> GetById(int id);
        Task<WarehouseDto?> GetByWarehouseName(string warehouseName);
        Task<WarehouseDto?> GetByWarehouseStatus(int status);

        Task<WarehouseImportResultVM> ImportFromExcelAsync(Stream excelStream);
    }
}
