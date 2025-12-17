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
            // Join các bảng
            var baseQuery = GetQueryable()
                .Include(sb => sb.Warehouse)
                .Include(sb => sb.Product)
                .Include(sb => sb.Transaction)
                .Include(sb => sb.ProductionFinish)  
                .AsQueryable();

            // Apply filters
            if (search != null)
            {
                if (!(search.BatchId <= 0))
                {
                    baseQuery = baseQuery.Where(sb => sb.BatchId == search.BatchId);
                }
                if (!string.IsNullOrEmpty(search.BatchCode))
                {
                    var keyword = search.BatchCode.Trim();
                    baseQuery = baseQuery.Where(sb => EF.Functions.Collate(sb.BatchCode, "SQL_Latin1_General_CP1_CI_AI")
                        .Contains(keyword));
                }
                if(!(search.TransactionId <= 0))
                {
                    baseQuery = baseQuery.Where(sb => sb.TransactionId == search.TransactionId);
                }
            }

            var query = baseQuery.Select(sb => new StockBatchDto()
            {
                BatchId = sb.BatchId,
                WarehouseId = sb.WarehouseId,
                WarehouseName = sb.Warehouse.WarehouseName, 
                ProductId = sb.ProductId,
                ProductName = sb.Product.ProductName,        
                TransactionId = sb.TransactionId,
                TransactionDate = sb.Transaction.TransactionDate,
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
            });

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
                            WarehouseName = sb.Warehouse.WarehouseName,
                            ProductId = sb.ProductId,
                            ProductName = sb.Product.ProductName,
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

        public async Task<StockBatchDto?> GetFirstByTransactionId(int transactionId)
        {
            var query = from sb in GetQueryable()
                        where sb.TransactionId == transactionId
                        select new StockBatchDto()
                        {
                            BatchId = sb.BatchId,
                            WarehouseId = sb.WarehouseId,
                            WarehouseName = sb.Warehouse.WarehouseName,
                            ProductId = sb.ProductId,
                            ProductName = sb.Product.ProductName,
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

        public async Task<StockBatchDto?> GetByName(string name)
        {
            var normalizedSearchName = name.Replace(" ", "").ToLower();

            var query = from sb in GetQueryable()
                        where sb.BatchCode.Replace(" ", "").ToLower() == normalizedSearchName
                        select new StockBatchDto()
                        {
                            BatchId = sb.BatchId,
                            WarehouseId = sb.WarehouseId,
                            WarehouseName = sb.Warehouse.WarehouseName,
                            ProductId = sb.ProductId,
                            ProductName = sb.Product.ProductName,
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

        public async Task<string> GetMaxBatchCodeByPrefix(string prefix)
        {
            var query = from sb in GetQueryable()
                        where sb.BatchCode.StartsWith(prefix)
                        orderby sb.BatchCode descending
                        select sb.BatchCode;
            return await query.FirstOrDefaultAsync();
        }

        public async Task<StockBatchDto> GetByBatchId(int id)
        {
            var query = from sb in GetQueryable()
                        where sb.BatchId == id
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


        /// <summary>
        /// Duc Anh
        /// Lay ra tat ca cac stock Batch trong list Product Id 
        /// có hạn sử dụng nhỏ hơn ngày hiện tại
        /// và vẫn còn sản phẩm
        /// </summary>
        /// <param name="ids">list các sản phẩm</param>
        /// <returns>trả về các stock batch theo product</returns>
        public async Task<List<StockBatchDto>> GetByProductIdForOrder(List<int> ids)
        {
            var query = from sb in GetQueryable().AsNoTracking()
                        where ids.Contains(sb.ProductId)
                        //where sb.ExpireDate > DateTime.Today
                        //where sb.QuantityIn > sb.QuantityOut
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

            query = query.OrderBy(sb => sb.ProductId)
                .ThenBy(sb => sb.ImportDate);
            return await query.ToListAsync();
        }

        public async Task<List<StockBatchDto>> GetByProductId(int? ids)
        {
            var query = from sb in GetQueryable().AsNoTracking()
                        where sb.ProductId == ids
                        //where sb.ExpireDate > DateTime.Today
                        //where sb.QuantityIn > sb.QuantityOut
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

            query = query
                .OrderByDescending(sb => sb.ImportDate);
            return await query.ToListAsync();
        }
    }
}
