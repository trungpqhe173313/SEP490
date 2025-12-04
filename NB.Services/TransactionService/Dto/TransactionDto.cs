using NB.Model.Entities;
using NB.Model.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.TransactionService.Dto
{
    public class TransactionDto : Transaction
    {
        public string? WarehouseName { get; set; }
        public string? WarehouseInName { get; set; }
        public string? StatusName { get; set; }

        public string? FullName { get; set; }
    }

    public class TransactionDetailResponseDto
    {
        public int TransactionId { get; set; }
        public string Type { get; set; } = string.Empty; // Import, Export, Transfer
        public string Status { get; set; } = string.Empty;
        public DateTime? TransactionDate { get; set; }
        public string? TransactionCode { get; set; }
        public string? Note { get; set; }
        public decimal? TotalWeight { get; set; }
        public decimal? TotalCost { get; set; }
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public int? WarehouseInId { get; set; }
        public string? WarehouseInName { get; set; }
        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public int? SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public int? PriceListId { get; set; }
        public List<TransactionDetailItemDto> Details { get; set; } = new List<TransactionDetailItemDto>();
    }

    public class TransactionDetailItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; } // = Quantity * UnitPrice
        public decimal? TotalWeight { get; set; } // = Quantity * WeightPerUnit (tấn)
    }

    public class ImportWeightSummaryDto
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalWeight { get; set; }
        public int TransactionCount { get; set; }
        public List<ImportWeightDetailDto> Details { get; set; } = new List<ImportWeightDetailDto>();
    }

    public class ImportWeightDetailDto
    {
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public decimal TotalWeight { get; set; }
        public int TransactionCount { get; set; }
    }

    public class ExportWeightSummaryDto
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalWeight { get; set; }
        public int TransactionCount { get; set; }
        public List<ExportWeightDetailDto> Details { get; set; } = new List<ExportWeightDetailDto>();
    }

    public class ExportWeightDetailDto
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalWeight { get; set; }
        public int TransactionCount { get; set; }
    }
}
