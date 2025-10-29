using NB.Model.Entities;
using NB.Repository.Common;
using NB.Service.Common;
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
    }
}
