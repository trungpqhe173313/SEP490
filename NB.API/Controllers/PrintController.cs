using Microsoft.AspNetCore.Mvc;
using NB.Service.Common;
using NB.Service.Core.Forms;
using NB.Service.Dto;
using NB.Service.ProductService;
using NB.Service.StockBatchService;
using NB.Service.SupplierService;
using NB.Service.TransactionDetailService;
using NB.Service.TransactionDetailService.ViewModels;
using NB.Service.TransactionService;
using NB.Service.TransactionService.ViewModels;
using NB.Service.UserService;
using NB.Service.WarehouseService;

namespace NB.API.Controllers
{
    [Route("api/print")]
    public class PrintController : Controller
    {
        private readonly ITransactionService _transactionService;
        private readonly ITransactionDetailService _transactionDetailService;
        private readonly IWarehouseService _warehouseService;
        private readonly IProductService _productService;
        private readonly IStockBatchService _stockBatchService;
        private readonly ISupplierService _supplierService;
        private readonly IUserService _userService;
        private readonly ILogger<PrintController> _logger;

        public PrintController(
            ITransactionService transactionService,
            ITransactionDetailService transactionDetailService,
            IWarehouseService warehouseService,
            IProductService productService,
            IStockBatchService stockBatchService,
            ISupplierService supplierService,
            IUserService userService,
            ILogger<PrintController> logger)
        {
            _transactionService = transactionService;
            _transactionDetailService = transactionDetailService;
            _warehouseService = warehouseService;
            _productService = productService;
            _stockBatchService = stockBatchService;
            _supplierService = supplierService;
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// API để in phiếu giao dịch (Import/Export) ra PDF
        /// </summary>
        /// <param name="transactionId">ID của đơn giao dịch cần in</param>
        /// <returns>File PDF phiếu giao dịch</returns>
        [HttpGet("Print/{transactionId}")]
        public async Task<IActionResult> Print(int transactionId)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ", 400));
            }

            try
            {
                // Validate transaction ID
                if (transactionId <= 0)
                {
                    return BadRequest(ApiResponse<object>.Fail("ID giao dịch không hợp lệ", 400));
                }

                // Lấy thông tin transaction
                var transaction = await _transactionService.GetByTransactionId(transactionId);
                if (transaction == null)
                {
                    return NotFound(ApiResponse<object>.Fail("Không tìm thấy đơn giao dịch", 404));
                }

                // Tạo TransactionPrintVM
                var printVM = new TransactionPrintVM
                {
                    TransactionId = transaction.TransactionId,
                    TransactionCode = transaction.TransactionCode,
                    TransactionDate = transaction.TransactionDate ?? DateTime.MinValue,
                    Type = transaction.Type,
                    Status = transaction.Status,
                    WarehouseName = (await _warehouseService.GetById(transaction.WarehouseId))?.WarehouseName ?? "N/A",
                    Note = transaction.Note,
                    TotalCost = transaction.TotalCost
                };

                // Lấy thông tin Nhà cung cấp (nếu là đơn Import)
                if (transaction.Type?.Equals("Import", StringComparison.OrdinalIgnoreCase) == true && transaction.SupplierId.HasValue)
                {
                    var supplier = await _supplierService.GetBySupplierId(transaction.SupplierId.Value);
                    if (supplier != null)
                    {
                        printVM.SupplierName = supplier.SupplierName;
                        printVM.SupplierPhone = supplier.Phone;
                        printVM.SupplierEmail = supplier.Email;
                    }
                }

                // Lấy thông tin Khách hàng (nếu là đơn Export)
                if (transaction.Type?.Equals("Export", StringComparison.OrdinalIgnoreCase) == true && transaction.CustomerId.HasValue)
                {
                    var customer = await _userService.GetByIdAsync(transaction.CustomerId.Value);
                    if (customer != null)
                    {
                        printVM.CustomerId = customer.UserId;
                        printVM.CustomerName = customer.FullName;
                        printVM.CustomerPhone = customer.Phone;
                        printVM.CustomerEmail = customer.Email;
                    }
                }

                // Lấy chi tiết các sản phẩm trong đơn
                var productDetails = await _transactionDetailService.GetByTransactionId(transactionId);
                if (productDetails == null || productDetails.Count == 0)
                {
                    return NotFound(ApiResponse<object>.Fail("Không có sản phẩm trong đơn giao dịch này", 404));
                }

                // Lấy thông tin sản phẩm và tạo danh sách
                foreach (var item in productDetails)
                {
                    var product = await _productService.GetById(item.ProductId);
                    item.ProductName = product != null ? product.ProductName : "N/A";
                    item.Code = product != null ? product.ProductCode : "N/A";

                    printVM.ProductList.Add(new TransactionDetailOutputVM
                    {
                        TransactionDetailId = item.Id,
                        ProductId = item.ProductId,
                        Code = item.Code,
                        ProductName = item.ProductName,
                        UnitPrice = item.UnitPrice,
                        WeightPerUnit = item.WeightPerUnit,
                        Quantity = item.Quantity
                    });
                }

                // Tạo PDF
                var pdfBytes = TransactionPdfGenerator.GenerateTransactionPdf(printVM);

                // Tạo tên file
                var filePrefix = transaction.Type?.Equals("Import", StringComparison.OrdinalIgnoreCase) == true
                    ? "Phieu_Nhap_Kho"
                    : "Phieu_Xuat_Kho";
                var fileName = $"{filePrefix}_{transaction.TransactionCode ?? transaction.TransactionId.ToString()}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

                // Trả về file PDF
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi in phiếu giao dịch {transactionId}");
                return BadRequest(ApiResponse<object>.Fail("Có lỗi xảy ra khi tạo file in"));
            }
        }
    }
}
