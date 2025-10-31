using Microsoft.EntityFrameworkCore;
using NB.Model.Entities;
using NB.Repository.Common;
using NB.Service.Common;
using NB.Service.Dto;
using NB.Service.StockBatchService.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.StockBatchService
{
    public class StockBatchService : Service<StockBatch>, IStockBatchService
    {
        public StockBatchService(IRepository<StockBatch> repository) : base(repository)
        {
        }

        public async Task<PagedList<StockBatchDto?>> GetData(StockBatchSearch search)
        {
            var query = from sb in GetQueryable()
                        select new StockBatchDto()
                        {
                            BatchId = sb.BatchId,
                            WarehouseId = sb.WarehouseId,
                            ProductId = sb.ProductId,
                            TransactionId = sb.TransactionId,
                            ProductionFinishId = sb.ProductionFinishId,
                            BatchCode = sb.BatchCode,
                            ImportDate = sb.ImportDate,
                            ExpireDate = sb.ExpireDate,
                            QuantityIn = sb.QuantityIn,
                            QuantityOut = sb.QuantityOut,
                            Status = sb.Status,
                            IsActive = sb.IsActive,
                            Note = sb.Note,
                            LastUpdated = sb.LastUpdated
                        };
            if(search != null)
            {
                if(!(search.BatchId <= 0))
                {
                    query = query.Where(sb => sb.BatchId == search.BatchId);
                }
                if (!string.IsNullOrEmpty(search.BatchCode))
                {
                    var keyword = search.BatchCode.Trim();
                    query = query.Where(sb => EF.Functions.Collate(sb.BatchCode, "SQL_Latin1_General_CP1_CI_AI")
                    .Contains(keyword));
                }
            }
            return await PagedList<StockBatchDto?>.CreateAsync(query, search);
        }

        public async Task<List<StockBatchDto?>> GetByTransactionId(int id)
        {
            var query = from sb in GetQueryable()
                        where sb.TransactionId == id
                        select new StockBatchDto()
                        {
                            BatchId = sb.BatchId,
                            WarehouseId = sb.WarehouseId,
                            ProductId = sb.ProductId,
                            TransactionId = sb.TransactionId,
                            ProductionFinishId = sb.ProductionFinishId,
                            BatchCode = sb.BatchCode,
                            ImportDate = sb.ImportDate,
                            ExpireDate = sb.ExpireDate,
                            QuantityIn = sb.QuantityIn,
                            QuantityOut = sb.QuantityOut,
                            Status = sb.Status,
                            IsActive = sb.IsActive,
                            Note = sb.Note,
                            LastUpdated = sb.LastUpdated
                        };
            return await query.ToListAsync();
        }

        public async Task<StockBatchDto?> GetByName(string name)
        {
            var normalizedSearchName = name.Replace(" ", "").ToLower();

            var query = from sb in GetQueryable()
                        where sb.BatchCode.Replace(" ", "").ToLower() == normalizedSearchName
                        select new StockBatchDto()
                        {
                            BatchId = sb.BatchId,
                            WarehouseId = sb.WarehouseId,
                            ProductId = sb.ProductId,
                            TransactionId = sb.TransactionId,
                            ProductionFinishId = sb.ProductionFinishId,
                            BatchCode = sb.BatchCode,
                            ImportDate = sb.ImportDate,
                            ExpireDate = sb.ExpireDate,
                            QuantityIn = sb.QuantityIn,
                            QuantityOut = sb.QuantityOut,
                            Status = sb.Status,
                            IsActive = sb.IsActive,
                            Note = sb.Note,
                            LastUpdated = sb.LastUpdated
                        };
            return await query.FirstOrDefaultAsync();
        }
    }
}
