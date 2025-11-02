using Microsoft.AspNetCore.Mvc;
using NB.Repository.WarehouseRepository;
using NB.Service.Common;
using NB.Service.Core.Forms;
using NB.Service.Core.Mapper;
using NB.Service.Dto;
using NB.Service.InventoryService;
using NB.Service.InventoryService.Dto;
using NB.Service.ProductService;
using NB.Service.StockBatchService;
using NB.Service.StockBatchService.Dto;
using NB.Service.StockBatchService.ViewModels;
using NB.Service.TransactionDetailService;
using NB.Service.TransactionService;
using NB.Service.WarehouseService;
using NB.Services.StockBatchService.ViewModels;
using OfficeOpenXml;
using System.Globalization;

namespace NB.API.Controllers
{
    [Route("api/stockinput")]
    public class StockInputController : Controller
    {
        private readonly IInventoryService _inventoryService;
        private readonly ITransactionService _transactionService;
        private readonly ITransactionDetailService _transactionDetailService;
        private readonly IWarehouseService _warehouseService;
        private readonly IProductService _productService;
        private readonly IStockBatchService _stockBatchService;
        private readonly ILogger<StockInputController> _logger;
        private readonly IMapper _mapper;

        public StockInputController(IInventoryService inventoryService, 
                                    ITransactionService transactionService, 
                                    ITransactionDetailService transactionDetailService,
                                    IWarehouseService warehouseService,
                                    IProductService productService,
                                    IStockBatchService stockBatchService,
                                    ILogger<StockInputController> logger,
                                    IMapper mapper)
        {
            _inventoryService = inventoryService;
            _transactionService = transactionService;
            _transactionDetailService = transactionDetailService;
            _warehouseService = warehouseService;
            _productService = productService;
            _stockBatchService = stockBatchService;
            _logger = logger;
            _mapper = mapper;
        }
        [HttpPost("GetData")]
        public async Task<IActionResult> GetData([FromBody] StockBatchSearch search)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ", 400));
            }
            try
            {
                var list = await _stockBatchService.GetData(search);
                List<StockOutputVM>? result = new List<StockOutputVM>();
                if (list != null)
                {
                    foreach (var item in list.Items)
                    {
                        StockOutputVM resultItem = new StockOutputVM();
                        resultItem.BatchId = item.BatchId;
                        resultItem.WarehouseId = item.WarehouseId;
                        resultItem.WarehouseName = (await _warehouseService.GetById(item.WarehouseId))?.WarehouseName;
                        resultItem.ProductName = (await _productService.GetById(item.ProductId))?.ProductName;
                        resultItem.TransactionId = item.TransactionId;
                        resultItem.ProductionFinishName = item.ProductionFinishId != null ? (await _productService.GetById(item.ProductionFinishId.Value))?.ProductName : null;
                        resultItem.BatchCode = item.BatchCode;
                        resultItem.ImportDate = item.ImportDate;
                        resultItem.ExpireDate = item.ExpireDate;
                        resultItem.QuantityIn = item.QuantityIn;
                        resultItem.Status = item.Status;
                        resultItem.IsActive = item.IsActive ?? false;
                        resultItem.Note = item.Note;
                        result.Add(resultItem);
                    }

                }
                else if (result == null || result.Count == 0)
                {
                    return NotFound(ApiResponse<PagedList<StockOutputVM>>.Fail("Không tìm thấy lô hàng nào.", 404));
                }
                var pagedResult = PagedList<StockOutputVM>.CreateFromList(result, search);
                return Ok(ApiResponse<PagedList<StockOutputVM>>.Ok(pagedResult));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy dữ liệu lô hàng");
                return BadRequest(ApiResponse<PagedList<StockOutputVM>>.Fail("Có lỗi xảy ra khi lấy dữ liệu"));
            }
        }

        
            
        [HttpPost("CreateStockInputs")]
        public async Task<IActionResult> CreateStockInputs([FromBody] StockBatchCreateVM model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ", 400));
            }

            if (model.WarehouseId <= 0)
            {
                return BadRequest(ApiResponse<object>.Fail($"ID kho {model.WarehouseId} không hợp lệ", 400));
            }
            var existWarehouse = await _warehouseService.GetById(model.WarehouseId);
            if (existWarehouse == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Không tìm thấy kho với ID: {model.WarehouseId}", 404));
            }

            if (model.TransactionId <= 0)
            {
                return BadRequest(ApiResponse<object>.Fail($"ID đơn nhập {model.TransactionId} không hợp lệ", 400));
            }
            var existTransaction = await _transactionService.GetByTransactionId(model.TransactionId);
            if (existTransaction == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Không tìm thấy đơn nhập với ID: {model.TransactionId}", 404));
            }
            if (model.ProductionFinishId != null)
            {
                return BadRequest(ApiResponse<object>.Fail("Đơn nhập không có sản phẩm sau xử lí.", 400));
            }


            if (model.BatchCode == null || model.BatchCode.Trim().Replace(" ", "").Length == 0)
            {
                return BadRequest(ApiResponse<object>.Fail("Mã lô không được để trống.", 400));
            }
            var existBatchCode = $"{model.BatchCode}{1:D4}";
            if(await _stockBatchService.GetByName(existBatchCode) != null)
            {
                return BadRequest(ApiResponse<object>.Fail("Mã lô này đã tồn tại"));
            }

            if (model.ExpireDate == null)
            {
                return BadRequest(ApiResponse<object>.Fail("Ngày hết hạn không được để trống.", 400));
            }

            if (model.ExpireDate <= DateTime.UtcNow)
            {
                return BadRequest(ApiResponse<object>.Fail("Ngày hết hạn phải sau ngày nhập.", 400));
            }


            try
            {
                var listTransactionDetails = await _transactionDetailService.GetById(model.TransactionId);
                int batchCounter = 1;
                List<StockOutputVM> list = new List<StockOutputVM>();
                foreach (var item in listTransactionDetails)
                {
                    
                    if (await _productService.GetById(item.ProductId) == null)
                    {
                        return NotFound(ApiResponse<object>.Fail($"Không tìm thấy sản phẩm với ID: {item.ProductId}", 404));
                    }else if (item.Quantity <= 0)
                    {
                        return BadRequest(ApiResponse<object>.Fail($"Số lượng sản phẩm với ID: {item.ProductId} không hợp lệ", 400));
                    }
                    else
                    {
                        string uniqueBatchCode = $"{model.BatchCode.Trim().Replace(" ", "")}{batchCounter:D4}"; // D4 = 4 chữ số: 0001, 0002, 0003...
                        var newStockBatch = _mapper.Map<StockBatchCreateVM, StockBatchDto>(model);
                        {
                            newStockBatch.WarehouseId = model.WarehouseId;
                            newStockBatch.ProductId = item.ProductId;
                            newStockBatch.TransactionId = model.TransactionId;
                            newStockBatch.BatchCode = uniqueBatchCode;
                            newStockBatch.ImportDate = DateTime.UtcNow;
                            newStockBatch.ExpireDate = model.ExpireDate;
                            newStockBatch.QuantityIn = item.Quantity;
                            newStockBatch.Status = 1; // Đặt trạng thái là 'Đã nhập kho'
                            newStockBatch.IsActive = true;
                            newStockBatch.LastUpdated = DateTime.UtcNow;
                            newStockBatch.Note = model.Note;
                        }

                        await _stockBatchService.CreateAsync(newStockBatch);

                        var existInventory = await _inventoryService.GetByWarehouseAndProductId(model.WarehouseId, item.ProductId);
                        if (existInventory == null)
                        {
                            // Tạo mới tồn kho nếu chưa có
                            var newInventory = _mapper.Map<StockBatchCreateVM, InventoryDto>(model);
                            {
                                newInventory.WarehouseId = model.WarehouseId;
                                newInventory.ProductId = item.ProductId;
                                newInventory.Quantity = item.Quantity;
                                newInventory.AverageCost = newInventory.AverageCost; // Giữ nguyên giá vốn trung bình ban đầu
                                newInventory.LastUpdated = DateTime.UtcNow;
                            }
                            ;
                            await _inventoryService.CreateAsync(newInventory);

                        }
                        else
                        {
                            // Cập nhật tồn kho nếu đã có
                            existInventory.AverageCost = existInventory.AverageCost;
                            existInventory.Quantity += item.Quantity;
                            existInventory.LastUpdated = DateTime.UtcNow;
                            await _inventoryService.UpdateAsync(existInventory);
                        }
                        StockOutputVM resultItem = new StockOutputVM();
                        resultItem.BatchId = newStockBatch.BatchId;
                        resultItem.WarehouseId = newStockBatch.WarehouseId;
                        resultItem.WarehouseName = (await _warehouseService.GetById(newStockBatch.WarehouseId))?.WarehouseName;
                        resultItem.ProductName = (await _productService.GetById(newStockBatch.ProductId))?.ProductName;
                        resultItem.TransactionId = newStockBatch.TransactionId;
                        resultItem.ProductionFinishName = newStockBatch.ProductionFinishId != null ? (await _productService.GetById(newStockBatch.ProductionFinishId.Value))?.ProductName : null;
                        resultItem.BatchCode = newStockBatch.BatchCode;
                        resultItem.ImportDate = newStockBatch.ImportDate;
                        resultItem.ExpireDate = newStockBatch.ExpireDate;
                        resultItem.QuantityIn = newStockBatch.QuantityIn;
                        resultItem.QuantityOut = newStockBatch.QuantityOut;
                        resultItem.Status = newStockBatch.Status;
                        resultItem.IsActive = newStockBatch.IsActive ?? false;
                        resultItem.Note = newStockBatch.Note;
                        list.Add(resultItem);
                        batchCounter++;
                    }
                }

                return Ok(ApiResponse<List<StockOutputVM>>.Ok(list));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo lô nhập kho mới");
                return BadRequest(ApiResponse<StockBatchDto>.Fail("Có lỗi xảy ra khi tạo lô nhập kho.", 400));
            }
        }

        [HttpPost("ImportFromExcel")]
        public async Task<IActionResult> ImportFromExcel(IFormFile file)
        {
            try
            {
                // Validate file
                if (file == null || file.Length == 0)
                {
                    return BadRequest(ApiResponse<StockBatchImportResultVM>.Fail("File không được để trống", 400));
                }

                // Validate file extension
                var allowedExtensions = new[] { ".xlsx", ".xls" };
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest(ApiResponse<StockBatchImportResultVM>.Fail("Chỉ chấp nhận file Excel (.xlsx, .xls)", 400));
                }

                // Validate file size (max 10MB)
                if (file.Length > 10 * 1024 * 1024)
                {
                    return BadRequest(ApiResponse<StockBatchImportResultVM>.Fail("Kích thước file không được vượt quá 10MB", 400));
                }

                var result = new StockBatchImportResultVM();
                var validationErrors = new List<string>();

                // Class để chứa dữ liệu đã validate
                var validatedRows = new List<(
                    int rowNumber,
                    int warehouseId,
                    int productId,
                    int quantity,
                    string batchCode,
                    DateTime expireDate,
                    int transactionId,
                    string note,
                    string warehouseName,
                    string productName
                )>();

                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    stream.Position = 0;

                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets[0];
                        var rowCount = worksheet.Dimension?.Rows ?? 0;

                        if (rowCount < 2)
                        {
                            return BadRequest(ApiResponse<StockBatchImportResultVM>.Fail("File Excel không có dữ liệu hoặc chỉ có header.", 400));
                        }

                        result.TotalRows = rowCount - 1;

                        // VALIDATE TẤT CẢ CÁC DÒNG
                        for (int row = 3; row <= rowCount; row++)
                        {
                            try
                            {
                                // Đọc dữ liệu trong file Excel
                                var warehouseIdStr = worksheet.Cells[row, 1].Value?.ToString()?.Trim();
                                var productIdStr = worksheet.Cells[row, 2].Value?.ToString()?.Trim();
                                var quantityStr = worksheet.Cells[row, 3].Value?.ToString()?.Trim();
                                var batchCode = worksheet.Cells[row, 4].Value?.ToString()?.Trim();
                                var expireDateCell = worksheet.Cells[row, 5];
                                var transactionIdStr = worksheet.Cells[row, 6].Value?.ToString()?.Trim();
                                var note = worksheet.Cells[row, 7].Value?.ToString()?.Trim();

                                // Validate các trường trong file excel - Thu thập TẤT CẢ lỗi của dòng
                                var rowErrors = new List<string>();

                                // Parse và validate format
                                int warehouseId = 0;
                                bool warehouseIdValid = !string.IsNullOrWhiteSpace(warehouseIdStr) && int.TryParse(warehouseIdStr, out warehouseId) && warehouseId > 0;
                                if (!warehouseIdValid)
                                {
                                    rowErrors.Add($"Dòng {row}: WarehouseId phải là số nguyên lớn hơn 0");
                                }

                                int productId = 0;
                                bool productIdValid = !string.IsNullOrWhiteSpace(productIdStr) && int.TryParse(productIdStr, out productId) && productId > 0;
                                if (!productIdValid)
                                {
                                    rowErrors.Add($"Dòng {row}: ProductId phải là số nguyên lớn hơn 0");
                                }

                                int quantity = 0;
                                bool quantityValid = !string.IsNullOrWhiteSpace(quantityStr) && int.TryParse(quantityStr, out quantity) && quantity > 0;
                                if (!quantityValid)
                                {
                                    rowErrors.Add($"Dòng {row}: Số lượng phải là số nguyên lớn hơn 0");
                                }

                                bool batchCodeValid = !string.IsNullOrWhiteSpace(batchCode);
                                if (!batchCodeValid)
                                {
                                    rowErrors.Add($"Dòng {row}: Mã lô không được để trống");
                                }

                                DateTime expireDate = DateTime.MinValue;
                                bool isValidDate = false;

                                // OLE Automation Date
                                if (expireDateCell.Value is double doubleValue)
                                {
                                    try
                                    {
                                        expireDate = DateTime.FromOADate(doubleValue);
                                        isValidDate = true;
                                    }
                                    catch
                                    {
                                        isValidDate = false;
                                    }
                                }

                                if (!isValidDate)
                                {
                                    rowErrors.Add($"Dòng {row}: Ngày hết hạn không hợp lệ. Giá trị: '{expireDateCell.Value}'");
                                }
                                else if (expireDate <= DateTime.UtcNow)
                                {
                                    rowErrors.Add($"Dòng {row}: Ngày hết hạn phải sau ngày hiện tại");
                                }

                                int transactionId = 0;
                                bool transactionIdValid = !string.IsNullOrWhiteSpace(transactionIdStr) && int.TryParse(transactionIdStr, out transactionId) && transactionId > 0;
                                if (!transactionIdValid)
                                {
                                    rowErrors.Add($"Dòng {row}: Mã giao dịch phải là số nguyên lớn hơn 0");
                                }

                                // Validate exist trong DB (chỉ khi parse thành công)
                                var existWarehouse = warehouseIdValid ? await _warehouseService.GetById(warehouseId) : null;
                                if (warehouseIdValid && existWarehouse == null)
                                {
                                    rowErrors.Add($"Dòng {row}: Không tìm thấy kho với ID: {warehouseId}");
                                }

                                var existProduct = productIdValid ? await _productService.GetById(productId) : null;
                                if (productIdValid && existProduct == null)
                                {
                                    rowErrors.Add($"Dòng {row}: Không tìm thấy sản phẩm với ID: {productId}");
                                }

                                var existTransaction = transactionIdValid ? await _transactionService.GetByTransactionId(transactionId) : null;
                                if (transactionIdValid && existTransaction == null)
                                {
                                    rowErrors.Add($"Dòng {row}: Không tìm thấy đơn nhập với ID: {transactionId}");
                                }

                                // Nếu có bất kỳ lỗi nào trong dòng này, thêm TẤT CẢ vào validationErrors
                                if (rowErrors.Any())
                                {
                                    validationErrors.AddRange(rowErrors);
                                    continue;
                                }

                                // Nếu tất cả đều hợp lệ, thêm vào list
                                validatedRows.Add((
                                    row,
                                    warehouseId,
                                    productId,
                                    quantity,
                                    batchCode,
                                    expireDate,
                                    transactionId,
                                    note,
                                    existWarehouse.WarehouseName,
                                    existProduct.ProductName
                                ));
                            }
                            catch (Exception ex)
                            {
                                validationErrors.Add($"Dòng {row}: {ex.Message}");
                            }
                        }

                        // Nếu có bất kỳ lỗi nào, return BadRequest, hủy toàn bộ quá trình import
                        if (validationErrors.Any())
                        {
                            result.TotalRows = rowCount - 1;
                            result.FailedCount = result.TotalRows;
                            result.SuccessCount = 0;
                            result.ErrorMessages = validationErrors;

                            return BadRequest(ApiResponse<StockBatchImportResultVM>.Fail(
                                validationErrors,
                                400));
                        }

                        // Dữ liệu hợp lệ, tiến hành tạo StockBatch và cập nhật Inventory
                        // Nhóm các hàng theo BatchCode để tạo batchcode riêng biệt
                        Dictionary<string, int> batchCodeCounters = new Dictionary<string, int>();
                        // Nhóm các Inventory
                        Dictionary<string, InventoryDto> inventoryCache = new Dictionary<string, InventoryDto>();

                        foreach (var validRow in validatedRows)
                        {
                            // Tạo số thứ tự dạng chuỗi cho BatchCode
                            string cleanBatchCode = validRow.batchCode.Trim().Replace(" ", "");
                            string uniqueBatchCode;

                            if (!batchCodeCounters.ContainsKey(cleanBatchCode))
                            {
                                // Tìm BatchCode cao nhất trong DB cho prefix này
                                var maxExistingBatchCode = await _stockBatchService.GetMaxBatchCodeByPrefix(cleanBatchCode);

                                int startCounter = 1;
                                if (maxExistingBatchCode != null)
                                {
                                    // Lấy 4 số cuối của BatchCode cao nhất
                                    string numberPart = maxExistingBatchCode.Substring(cleanBatchCode.Length);
                                    if (int.TryParse(numberPart, out int maxNumber))
                                    {
                                        startCounter = maxNumber + 1;
                                    }
                                }

                                batchCodeCounters[cleanBatchCode] = startCounter;
                            }

                            uniqueBatchCode = $"{cleanBatchCode}{batchCodeCounters[cleanBatchCode]:D4}";
                            batchCodeCounters[cleanBatchCode]++;

                            // Kiểm tra lại lần nữa để đảm bảo không trùng
                            while (await _stockBatchService.GetByName(uniqueBatchCode) != null)
                            {
                                uniqueBatchCode = $"{cleanBatchCode}{batchCodeCounters[cleanBatchCode]:D4}";
                                batchCodeCounters[cleanBatchCode]++;
                            }

                            // Tạo StockBatch
                            var newStockBatch = new StockBatchDto
                            {
                                WarehouseId = validRow.warehouseId,
                                ProductId = validRow.productId,
                                TransactionId = validRow.transactionId,
                                BatchCode = uniqueBatchCode,
                                ImportDate = DateTime.UtcNow,
                                ExpireDate = validRow.expireDate,
                                QuantityIn = validRow.quantity,
                                Status = 1,
                                IsActive = true,
                                LastUpdated = DateTime.UtcNow,
                                Note = validRow.note
                            };

                            await _stockBatchService.CreateAsync(newStockBatch);

                            // Xử lý Inventory cache
                            string inventoryKey = $"{validRow.warehouseId}_{validRow.productId}";

                            if (!inventoryCache.ContainsKey(inventoryKey))
                            {
                                // Lần đầu gặp combination này, load từ DB
                                var existInventory = await _inventoryService.GetByWarehouseAndProductId(validRow.warehouseId, validRow.productId);

                                if (existInventory == null)
                                {
                                    // Tạo mới trong cache
                                    var newInventory = new InventoryDto
                                    {
                                        WarehouseId = validRow.warehouseId,
                                        ProductId = validRow.productId,
                                        Quantity = validRow.quantity,
                                        LastUpdated = DateTime.UtcNow
                                    };
                                    inventoryCache[inventoryKey] = newInventory;
                                }
                                else
                                {
                                    // Đã tồn tại, cộng thêm số lượng
                                    existInventory.Quantity += validRow.quantity;
                                    existInventory.LastUpdated = DateTime.UtcNow;
                                    inventoryCache[inventoryKey] = existInventory;
                                }
                            }
                            else
                            {
                                // Đã gặp rồi trong file Excel, cộng dồn trong cache
                                inventoryCache[inventoryKey].Quantity += validRow.quantity;
                                inventoryCache[inventoryKey].LastUpdated = DateTime.UtcNow;
                            }

                            // Thêm StockBatch vào result
                            var resultItem = new StockOutputVM
                            {
                                BatchId = newStockBatch.BatchId,
                                WarehouseId = newStockBatch.WarehouseId,
                                WarehouseName = validRow.warehouseName,
                                ProductName = validRow.productName,
                                TransactionId = newStockBatch.TransactionId,
                                ProductionFinishName = null,
                                BatchCode = newStockBatch.BatchCode,
                                ImportDate = newStockBatch.ImportDate,
                                ExpireDate = newStockBatch.ExpireDate,
                                QuantityIn = newStockBatch.QuantityIn,
                                Status = newStockBatch.Status,
                                IsActive = newStockBatch.IsActive ?? false,
                                Note = newStockBatch.Note
                            };
                            result.ImportedStockBatches.Add(resultItem);
                            result.SuccessCount++;
                        }

                        // Update hoặc create inventory
                        foreach (var inv in inventoryCache)
                        {
                            var inventory = inv.Value;

                            if (inventory.InventoryId == 0) // InventoryId = 0 nghĩa là mới tạo
                            {
                                await _inventoryService.CreateAsync(inventory);
                            }
                            else
                            {
                                await _inventoryService.UpdateAsync(inventory);
                            }
                        }
                    }
                }
                // Hoàn tất import và trả về kết quả
                result.ErrorMessages = new List<string>();
                result.FailedCount = 0;
                return Ok(ApiResponse<StockBatchImportResultVM>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi import file Excel");
                return BadRequest(ApiResponse<StockBatchImportResultVM>.Fail($"Có lỗi xảy ra: {ex.Message}", 400));
            }
        }

        [HttpGet("DownloadTemplate")]
        public IActionResult DownloadTemplate()
        {
            try
            {
                var stream = ExcelTemplateGenerator.GenerateStockInputTemplate();
                var fileName = $"StockInput_Import_Template_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                return File(
                    stream,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo file template");
                return BadRequest(ApiResponse<object>.Fail($"Có lỗi xảy ra khi tạo template: {ex.Message}"));
            }
        }
    }
}
