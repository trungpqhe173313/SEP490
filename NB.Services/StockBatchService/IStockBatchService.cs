using NB.Model.Entities;
using NB.Service.Common;
using NB.Service.StockBatchService.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.StockBatchService
{
    public interface IStockBatchService : IService<StockBatch>
    {
        Task<PagedList<StockBatchDto?>> GetData(StockBatchSearch search);

        Task<List<StockBatchDto>> GetByTransactionId(int id);
        //Duc Anh
        Task<List<StockBatchDto>> GetByProductIdForOrder(List<int> ids);

        Task<StockBatchDto?> GetByName(string name);
        //Task<List<StockBatchDto?>> GetByProductIdForTransaction(int productId, int transactionId);
        Task<StockBatchDto> GetByBatchId(int id);

        Task<string> GetMaxBatchCodeByPrefix(string code);
    }
}
