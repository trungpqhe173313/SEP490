using Microsoft.EntityFrameworkCore;
using NB.Model.Entities;
using NB.Repository.Common;
using NB.Service.Common;
using NB.Service.FinancialTransactionService.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.FinancialTransactionService
{
    public class FinancialTransactionService : Service<FinancialTransaction>, IFinancialTransactionService
    {
        public FinancialTransactionService(IRepository<FinancialTransaction> repository) : base(repository)
        {
        }

        public async Task<List<FinancialTransactionDto>> GetByRelatedTransactionID(int id)
        {
            var query = from ft in GetQueryable()
                        where ft.RelatedTransactionId.HasValue
                        && ft.RelatedTransactionId == id
                        select new FinancialTransactionDto
                        {
                            FinancialTransactionId = ft.FinancialTransactionId,
                            Amount = ft.Amount,
                            Description = ft.Description,
                            PaymentMethod = ft.PaymentMethod,
                            Type = ft.Type,
                            RelatedTransactionId = ft.RelatedTransactionId,
                            TransactionDate = ft.TransactionDate,
                            CreatedBy = ft.CreatedBy,
                            PayrollId = ft.PayrollId
                        };
            query = query.OrderByDescending(ft => ft.TransactionDate);
            return await query.ToListAsync();
        }
    }       
}
