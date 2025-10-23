using NB.Model.Entities;
using NB.Service.Common;
using NB.Service.SupplierService.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.SupplierService
{
    public interface ISupplierService : IService<Supplier>
    {
        Task<PagedList<SupplierDto>> GetData(SupplierSearch search);
        Task<SupplierDto?> GetBySupplierId(int id);  
    }
}
