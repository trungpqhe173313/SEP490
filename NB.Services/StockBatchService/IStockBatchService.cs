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
    }
}
