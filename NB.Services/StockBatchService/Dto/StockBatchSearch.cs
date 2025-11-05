using NB.Service.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.StockBatchService.Dto
{
    public class StockBatchSearch : SearchBase
    {
        public int? TransactionId { get; set; }
        public int? BatchId { get; set; }

        public string? BatchCode { get; set; }
    }
}
