using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.StockBatchService.ViewModels
{
    public class StockBatchCreateVM
    {

        public int WarehouseId { get; set; }


        public int TransactionId { get; set; }

        public int? ProductionFinishId { get; set; }

        public string? BatchCode { get; set; }

        public DateTime? ExpireDate { get; set; }

        public string? Note { get; set; }

    }
}
