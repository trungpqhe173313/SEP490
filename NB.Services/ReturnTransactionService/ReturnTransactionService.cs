using Microsoft.EntityFrameworkCore;
using NB.Model.Entities;
using NB.Repository.Common;
using NB.Service.Common;
using NB.Service.ReturnTransactionService.Dto;
using NB.Service.TransactionService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.ReturnTransactionService
{
    public class ReturnTransactionService : Service<ReturnTransaction>, IReturnTransactionService
    {
        private readonly ITransactionService _transactionService;

        public ReturnTransactionService(IRepository<ReturnTransaction> repository, ITransactionService transactionService) : base(repository)
        {
            _transactionService = transactionService;
        }

        public async Task<PagedList<ReturnOrderDto>> GetData(ReturnOrderSearch search)
        {
            // Query ReturnTransaction với Transaction
            var query = from rt in GetQueryable()
                       join t in _transactionService.GetQueryable() on rt.TransactionId equals t.TransactionId
                       select new ReturnOrderDto
                       {
                           ReturnTransactionId = rt.ReturnTransactionId,
                           TransactionId = rt.TransactionId,
                           Reason = rt.Reason,
                           CreatedAt = rt.CreatedAt,
                           TransactionType = t.Type,
                           TransactionDate = t.TransactionDate,
                           WarehouseId = t.WarehouseId,
                           CustomerId = t.CustomerId,
                           SupplierId = t.SupplierId,
                           Status = t.Status
                       };

            // Filter theo Type nếu có (Import hoặc Export)
            if (search != null && !string.IsNullOrEmpty(search.Type))
            {
                var type = search.Type.Trim();
                if (type.Equals("Import", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(x => x.TransactionType == "Import");
                }
                else if (type.Equals("Export", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(x => x.TransactionType == "Export");
                }
            }

            // Filter theo TransactionId nếu có
            if (search != null && search.TransactionId.HasValue && search.TransactionId.Value > 0)
            {
                query = query.Where(x => x.TransactionId == search.TransactionId.Value);
            }

            // Order by CreatedAt descending
            query = query.OrderByDescending(x => x.CreatedAt);

            return await PagedList<ReturnOrderDto>.CreateAsync(query, search);
        }
    }
}
