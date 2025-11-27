using NB.Service.TransactionDetailService.ViewModels;
using System;
using System.Collections.Generic;

namespace NB.Service.TransactionService.ViewModels
{
    /// <summary>
    /// ViewModel dành riêng cho in ấn phiếu giao dịch
    /// </summary>
    public class TransactionPrintVM
    {
        // Thông tin cơ bản
        public int TransactionId { get; set; }
        public string? TransactionCode { get; set; }
        public DateTime TransactionDate { get; set; }
        public string? Type { get; set; }
        public int? Status { get; set; }

        // Thông tin kho
        public string? WarehouseName { get; set; }

        // Thông tin Nhà cung cấp (dành cho Import)
        public string? SupplierName { get; set; }
        public string? SupplierPhone { get; set; }
        public string? SupplierEmail { get; set; }

        // Thông tin Khách hàng (dành cho Export)
        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerEmail { get; set; }

        // Chi tiết sản phẩm
        public List<TransactionDetailOutputVM> ProductList { get; set; } = new List<TransactionDetailOutputVM>();

        // Tổng tiền
        public decimal? TotalCost { get; set; }

        // Ghi chú
        public string? Note { get; set; }
    }
}
