using NB.Model.Entities;
using NB.Service.Common;
using NB.Service.ContractService.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.ContractService
{
    public interface IContractService : IService<Contract>
    {
        Task<List<ContractDto?>> GetData(ContractSearch search);

        Task<ContractDto?> GetByContractId(int contractId);
    }
}
