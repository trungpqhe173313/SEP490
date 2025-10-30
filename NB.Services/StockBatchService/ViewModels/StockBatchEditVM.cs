using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.StockBatchService.ViewModels
{
    public class StockBatchEditVM
    {
        public int BatchId { get; set; }

        public int WarehouseId { get; set; }

        public int ProductId { get; set; }

        public int? TransactionId { get; set; }

        public int? ProductionFinishId { get; set; }

        public string? BatchCode { get; set; }

        public DateTime? ImportDate { get; set; }

        public DateTime? ExpireDate { get; set; }

        public decimal? QuantityIn { get; set; }

        public decimal? QuantityOut { get; set; }

        public int? Status { get; set; }

        public bool? IsActive { get; set; }

        public string? Note { get; set; }

    }
}
