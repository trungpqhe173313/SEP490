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
            return await PagedList<StockBatchDto?>.CreateAsync(query, search);
        }
    }
}
