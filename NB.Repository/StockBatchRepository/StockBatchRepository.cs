using Microsoft.EntityFrameworkCore;
using NB.Model.Entities;
using NB.Repository.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Repository.StockBatchRepository
{
    public class StockBatchRepository : Repository<StockBatch>, IStockBatchRepository
    {
        public StockBatchRepository(DbContext context) : base(context)
        {
        }
    }
}
