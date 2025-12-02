using Microsoft.EntityFrameworkCore;
using NB.Model.Entities;
using NB.Model.Enums;
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

        public async Task<PagedList<FinancialTransactionDto>> GetData(FinancialTransactionSearch search)
        {
            var query = from ft in GetQueryable()
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

            if (search != null)
            {
                if (search.Type.HasValue)
                {
                    // Chuyển đổi enum int sang string để so sánh với Type trong database
                    var typeEnum = (FinancialTransactionType)search.Type.Value;
                    var typeString = typeEnum.ToString();
                    query = query.Where(ft => ft.Type == typeString);
                }
                if (search.RelatedTransactionId.HasValue && search.RelatedTransactionId.Value > 0)
                {
                    query = query.Where(ft => ft.RelatedTransactionId == search.RelatedTransactionId.Value);
                }
                if (search.PayrollId.HasValue && search.PayrollId.Value > 0)
                {
                    query = query.Where(ft => ft.PayrollId == search.PayrollId.Value);
                }
                if (search.CreatedBy.HasValue && search.CreatedBy.Value > 0)
                {
                    query = query.Where(ft => ft.CreatedBy == search.CreatedBy.Value);
                }
                if (search.TransactionFromDate.HasValue)
                {
                    query = query.Where(ft => ft.TransactionDate >= search.TransactionFromDate.Value);
                }
                if (search.TransactionToDate.HasValue)
                {
                    var toDate = search.TransactionToDate.Value.Date.AddDays(1);
                    query = query.Where(ft => ft.TransactionDate < toDate);
                }
            }

            query = query.OrderByDescending(ft => ft.TransactionDate);
            return await PagedList<FinancialTransactionDto>.CreateAsync(query, search);
        }

        public async Task<FinancialTransactionDto?> GetByIdAsync(int id)
        {
            var query = from ft in GetQueryable()
                        where ft.FinancialTransactionId == id
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
            return await query.FirstOrDefaultAsync();
        }
    }       
}
