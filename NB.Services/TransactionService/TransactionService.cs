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
    }
}
