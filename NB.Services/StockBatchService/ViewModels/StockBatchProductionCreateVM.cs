using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.StockBatchService.ViewModels
{
    public class StockBatchProductionCreateVM
    {
        public int WarehouseId { get; set; }
        public int ProductId { get; set; }
        public int? ProductionFinishId { get; set; }
        public string? BatchCode { get; set; }
        public DateTime? ImportDate { get; set; }
        public decimal? QuantityIn { get; set; }
        public int? Status { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? LastUpdated { get; set; }
        public string? Note { get; set; }
    }
}

