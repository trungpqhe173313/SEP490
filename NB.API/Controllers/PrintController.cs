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
using QRCoder;
using NB.API.Utils;

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
        private readonly ICloudinaryService _cloudinaryService;
        private readonly ILogger<PrintController> _logger;

        public PrintController(
            ITransactionService transactionService,
            ITransactionDetailService transactionDetailService,
            IWarehouseService warehouseService,
            IProductService productService,
            IStockBatchService stockBatchService,
            ISupplierService supplierService,
            IUserService userService,
            ICloudinaryService cloudinaryService,
            ILogger<PrintController> logger)
        {
            _transactionService = transactionService;
            _transactionDetailService = transactionDetailService;
            _warehouseService = warehouseService;
            _productService = productService;
            _stockBatchService = stockBatchService;
            _supplierService = supplierService;
            _userService = userService;
            _cloudinaryService = cloudinaryService;
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

                // Tải QR Code thanh toán từ VietQR API (chỉ cho đơn Export)
                byte[]? qrCodeBytes = null;

                // Chỉ tạo QR code nếu là đơn Export
                if (transaction.Type?.Equals("Export", StringComparison.OrdinalIgnoreCase) == true)
                {
                    try
                    {
                        // Thông tin tài khoản Vietcombank
                        var bankAccount = "1077909999"; // STK Vietcombank
                        var bankCode = "970436"; // Mã ngân hàng Vietcombank
                        var accountName = "CONG TY TNHH TM DV QUANG THANH";
                        var amount = ((int)(transaction.TotalCost ?? 0)).ToString(); // Số tiền thanh toán
                        var transactionCode = transaction.TransactionCode ?? transaction.TransactionId.ToString();
                        var description = $"Thanh toan {transactionCode}";

                        // VietQR API URL - Tải QR image trực tiếp từ VietQR
                        // Format: https://img.vietqr.io/image/{BANK_CODE}-{ACCOUNT_NUMBER}-compact2.png?amount={AMOUNT}&addInfo={DESCRIPTION}&accountName={ACCOUNT_NAME}
                        var vietQRUrl = $"https://img.vietqr.io/image/{bankCode}-{bankAccount}-compact2.png?amount={amount}&addInfo={Uri.EscapeDataString(description)}&accountName={Uri.EscapeDataString(accountName)}";

                        // Tải QR code image từ VietQR API
                        using (var httpClient = new HttpClient())
                        {
                            httpClient.Timeout = TimeSpan.FromSeconds(10);
                            var response = await httpClient.GetAsync(vietQRUrl);

                            if (response.IsSuccessStatusCode)
                            {
                                qrCodeBytes = await response.Content.ReadAsByteArrayAsync();
                                _logger.LogInformation($"Downloaded VietQR image successfully for transaction {transactionId}");
                            }
                            else
                            {
                                _logger.LogWarning($"Failed to download VietQR image: {response.StatusCode}");
                            }
                        }

                        // Upload QR code lên Cloudinary
                        if (qrCodeBytes != null && qrCodeBytes.Length > 0)
                        {
                            var qrFileName = $"QR_Transaction_{transactionCode}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                            var qrUrl = await _cloudinaryService.UploadImageFromBytesAsync(qrCodeBytes, qrFileName, "transactions/qrcodes");

                            if (!string.IsNullOrEmpty(qrUrl))
                            {
                                // Lưu URL QR code vào Transaction
                                var transactionEntity = await _transactionService.GetByIdAsync(transactionId);
                                if (transactionEntity != null)
                                {
                                    transactionEntity.TransactionQr = qrUrl;
                                    await _transactionService.UpdateAsync(transactionEntity);
                                    _logger.LogInformation($"QR code uploaded and saved for transaction {transactionId}: {qrUrl}");
                                }
                            }
                        }
                    }
                    catch (Exception qrEx)
                    {
                        _logger.LogWarning(qrEx, $"Không thể tạo QR code cho giao dịch {transactionId}");
                        // Tiếp tục tạo PDF ngay cả khi QR code fail
                    }
                }

                // Tạo PDF với QR code
                var pdfBytes = TransactionPdfGenerator.GenerateTransactionPdf(printVM, qrCodeBytes);

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
