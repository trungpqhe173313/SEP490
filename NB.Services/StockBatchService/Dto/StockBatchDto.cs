using NB.Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.StockBatchService.Dto
{
    public class StockBatchDto : StockBatch
    {
        public string? WarehouseName { get; set; }      
        public string? ProductName { get; set; }        

    }
}
