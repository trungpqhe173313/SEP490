using NB.Model.Entities;
using NB.Service.Common;
using NB.Service.FinancialTransactionService.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.FinancialTransactionService
{
    public interface IFinancialTransactionService : IService<FinancialTransaction>
    {
        public Task<List<FinancialTransactionDto>> GetByRelatedTransactionID(int id);
        public Task<PagedList<FinancialTransactionDto>> GetData(FinancialTransactionSearch search);
        public Task<FinancialTransactionDto?> GetByIdAsync(int id);
    }
}
