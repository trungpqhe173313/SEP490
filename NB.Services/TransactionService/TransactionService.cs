using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using NB.Model.Entities;
using NB.Repository.Common;
using NB.Service.Common;
using NB.Service.TransactionService.Dto;


namespace NB.Service.TransactionService
{
    public class TransactionService : Service<Transaction>, ITransactionService
    {
        public TransactionService(IRepository<Transaction> repository) : base(repository)
        {
        }
        public async Task<List<TransactionDto>> GetById(int? id)
        {
            var query = from t in GetQueryable()
                        where t.TransactionId == id
                        select new TransactionDto()
                        {
                            TransactionId = t.TransactionId,
                            CustomerId = t.CustomerId,
                            WarehouseInId = t.WarehouseInId,
                            SupplierId = t.SupplierId,
                            WarehouseId = t.WarehouseId,
                            ConversionRate = t.ConversionRate,
                            Type = t.Type,
                            Status = t.Status,
                            TransactionDate = t.TransactionDate,
                            Note = t.Note
                        };
            return await query.ToListAsync();
        }

        public async Task<TransactionDto?> GetByTransactionId(int? id)
        {
            var query = from t in GetQueryable()
                        where t.TransactionId == id
                        select new TransactionDto()
                        {
                            TransactionId = t.TransactionId,
                            CustomerId = t.CustomerId,
                            WarehouseInId = t.WarehouseInId,
                            SupplierId = t.SupplierId,
                            WarehouseId = t.WarehouseId,
                            ConversionRate = t.ConversionRate,
                            Type = t.Type,
                            Status = t.Status,
                            TransactionDate = t.TransactionDate,
                            Note = t.Note
                        };

            return await query.FirstOrDefaultAsync();
        }

        public async Task<PagedList<TransactionDto>> GetData(TransactionSearch search)
        {


            var query = from t in GetQueryable()
                        select new TransactionDto()
                        {
                            TransactionId = t.TransactionId,
                            CustomerId = t.CustomerId,
                            WarehouseInId = t.WarehouseInId,
                            SupplierId = t.SupplierId,
                            WarehouseId = t.WarehouseId,
                            ConversionRate = t.ConversionRate,
                            Type = t.Type,
                            Status = t.Status,
                            TransactionDate = t.TransactionDate,
                            Note = t.Note
                        };
            if (search != null)
            {

                if (search.Status.HasValue)
                {
                    query = query.Where(t => t.Status == search.Status);
                }
                if (!string.IsNullOrEmpty(search.Type))
                {
                    var keyword = search.Type.Trim();
                    query = query.Where(t => EF.Functions.Collate(t.Type, "SQL_Latin1_General_CP1_CI_AI")
                    .Contains(keyword));
                }
                if (search.TransactionFromDate.HasValue)
                {
                    query = query.Where(t => t.TransactionDate >= search.TransactionFromDate);
                }
                if (search.TransactionToDate.HasValue)
                {
                    query = query.Where(t => t.TransactionDate <= search.TransactionToDate);
                }
            }

            query = query.OrderByDescending(t => t.TransactionDate);
            return await PagedList<TransactionDto>.CreateAsync(query, search);
        }
    

        public async Task<PagedList<TransactionDto>> GetDataForExport(TransactionSearch search)
        {
            var query = from t in GetQueryable()
                        where t.Type == "Export"
                        select new TransactionDto()
                        {
                            TransactionId = t.TransactionId,
                            CustomerId = t.CustomerId,
                            WarehouseInId = t.WarehouseInId,
                            SupplierId = t.SupplierId,
                            WarehouseId = t.WarehouseId,
                            ConversionRate = t.ConversionRate,
                            Type = t.Type,
                            Status = t.Status,
                            TransactionDate = t.TransactionDate,
                            Note = t.Note
                        };
            if(search != null)
            {
                if (search.Status.HasValue)
                {
                    query = query.Where(t => t.Status.Value == search.Status);
                }
                if (!string.IsNullOrEmpty(search.Type))
                {
                    var keyword = search.Type.Trim();
                    query = query.Where(t => EF.Functions.Collate(t.Type, "SQL_Latin1_General_CP1_CI_AI")
                    .Contains(keyword));
                }
                if (search.TransactionFromDate.HasValue)
                {
                    query = query.Where(t => t.TransactionDate >= search.TransactionFromDate);
                }
                if (search.TransactionToDate.HasValue)
                {
                    query = query.Where(t => t.TransactionDate <= search.TransactionToDate);
                }
            }

            query = query.OrderByDescending(t => t.TransactionDate);
            return await PagedList<TransactionDto>.CreateAsync(query,search);
        }
    }
}
