using NB.Model.Entities;
using NB.Service.Common;
using NB.Service.SupplierService.Dto;

namespace NB.Service.SupplierService
{
    public interface ISupplierService : IService<Supplier>
    {
        Task<PagedList<SupplierDto>> GetData(SupplierSearch search);
        Task<SupplierDto?> GetBySupplierId(int id);
        Task<SupplierDto?> GetByEmail(string email);
        Task<SupplierDto?> GetByPhone(string phone);
        Task<SupplierDto?> GetByName(string name);
    }
}
