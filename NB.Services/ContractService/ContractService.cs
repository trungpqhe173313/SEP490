using Microsoft.EntityFrameworkCore;
using NB.Model.Entities;
using NB.Repository.Common;
using NB.Repository.ContractRepository;
using NB.Service.AccountService;
using NB.Service.Common;
using NB.Service.ContractService.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.ContractService
{
    public class ContractService : Service<Contract>, IContractService
    {
        private readonly IRepository<Contract> _contractRepository;
        private readonly IRepository<User> _userService;
        private readonly IRepository<Supplier> _supplierRepository;

        public ContractService(
            IRepository<Contract> contractRepository,
            IRepository<User> userRepository,
            IRepository<Supplier> supplierRepository) :base(contractRepository)
        {
            _contractRepository = contractRepository;
            _userService = userRepository;
            _supplierRepository = supplierRepository;
        }

        public async Task<List<ContractDto?>> GetData(ContractSearch search)
        {
            var query = from c in GetQueryable()
                        select new ContractDto
                        {
                            ContractId = c.ContractId,
                            UserId = c.UserId,
                            User = c.User,
                            SupplierId = c.SupplierId,
                            Supplier = c.Supplier,
                            Image = c.Image,
                            Pdf = c.Pdf,
                            IsActive = c.IsActive,
                            CreatedAt = c.CreatedAt,
                            UpdatedAt = c.UpdatedAt
                        };
            if (search != null)
            {

                if (search.CustomerId.HasValue && !search.SupplierId.HasValue)
                {
                    //Lấy records của customer 
                    query = query.Where(c => c.UserId == search.CustomerId && c.SupplierId == null);
                }
                else if (!search.CustomerId.HasValue && search.SupplierId.HasValue)
                {
                    //Lấy records của supplier 
                    query = query.Where(c => c.SupplierId == search.SupplierId && c.UserId == null);
                }
                else if (search.CustomerId.HasValue && search.SupplierId.HasValue)
                {
                    //Lấy records có cả customer, supplier
                    query = query.Where(c => c.UserId == search.CustomerId && c.SupplierId == search.SupplierId);
                }
                if (search.FromDate.HasValue)
                {
                    query = query.Where(c => c.CreatedAt >= search.FromDate.Value);
                }
                if (search.ToDate.HasValue)
                {
                    query = query.Where(c => c.CreatedAt <= search.ToDate.Value);
                }
            }
            return await Task.FromResult(query.ToList());
        }

        public async Task<ContractDto?> GetByContractId(int contractId)
        {
            var query = from c in GetQueryable()
                        where c.ContractId == contractId
                        select new ContractDto
                        {
                            ContractId = c.ContractId,
                            UserId = c.UserId,
                            User = c.User,
                            SupplierId = c.SupplierId,
                            Supplier = c.Supplier,
                            Image = c.Image,
                            Pdf = c.Pdf,
                            IsActive = c.IsActive,
                            CreatedAt = c.CreatedAt,
                            UpdatedAt = c.UpdatedAt
                        };
            var contract = await Task.FromResult(query.FirstOrDefault());

            return await Task.FromResult(contract);
        }
    }

    
}
