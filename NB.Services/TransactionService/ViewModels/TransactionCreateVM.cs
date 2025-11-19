using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.TransactionService.ViewModels
{
    public class TransactionCreateVM
    {
        public int? CustomerId { get; set; }

        public int? WarehouseInId { get; set; }

        public int? SupplierId { get; set; }

        public int WarehouseId { get; set; }

        public decimal? ConversionRate { get; set; }

        public string? Note { get; set; }

        public decimal? TotalCost { get; set; }

        public int? PriceListId { get; set; }
    }
}
