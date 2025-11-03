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
using NB.Service.SupplierService;
using NB.Services.StockBatchService.ViewModels;
using OfficeOpenXml;
using System.Globalization;
using NB.Service.TransactionService.Dto;
using NB.Service.TransactionDetailService.Dto;
using NB.Model.Entities;

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
        private readonly ISupplierService _supplierService;
        private readonly ILogger<StockInputController> _logger;
        private readonly IMapper _mapper;

        public StockInputController(IInventoryService inventoryService,
                                    ITransactionService transactionService,
                                    ITransactionDetailService transactionDetailService,
                                    IWarehouseService warehouseService,
                                    IProductService productService,
                                    IStockBatchService stockBatchService,
                                    ISupplierService supplierService,
                                    ILogger<StockInputController> logger,
                                    IMapper mapper)
        {
            _inventoryService = inventoryService;
            _transactionService = transactionService;
            _transactionDetailService = transactionDetailService;
            _warehouseService = warehouseService;
            _productService = productService;
            _stockBatchService = stockBatchService;
            _supplierService = supplierService;
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
                var pagedResult = await _stockBatchService.GetData(search);

                if (pagedResult == null || pagedResult.Items.Count == 0)
                {
                    return NotFound(ApiResponse<PagedList<StockBatchDto>>.Fail("Không tìm thấy lô hàng nào.", 404));
                }


                var result = pagedResult.Items.Select(item => new StockOutputVM
                {
                    BatchId = item.BatchId,
                    WarehouseId = item.WarehouseId,
                    WarehouseName = item.WarehouseName,
                    ProductName = item.ProductName,
                    TransactionId = item.TransactionId,
                    ProductionFinishId = item.ProductionFinishId,
                    BatchCode = item.BatchCode,
                    ImportDate = item.ImportDate,
                    ExpireDate = item.ExpireDate,
                    QuantityIn = item.QuantityIn,
                    Status = item.Status,
                    IsActive = item.IsActive ?? false,
                    Note = item.Note
                }).ToList();

                var finalResult = new PagedList<StockOutputVM>(
                    items: result,
                    pageIndex: pagedResult.PageIndex,
                    pageSize: pagedResult.PageSize,
                    totalCount: pagedResult.TotalCount
                );
                return Ok(ApiResponse<PagedList<StockOutputVM>>.Ok(finalResult));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy dữ liệu lô hàng");
                return BadRequest(ApiResponse<PagedList<StockOutputVM>>.Fail("Có lỗi xảy ra khi lấy dữ liệu"));
            }
        }



        [HttpPost("CreateStockInputs")]
        public async Task<IActionResult> CreateStockInputs([FromBody] StockBatchCreateWithProductsVM model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ", 400));
            }

            // Validate Warehouse
            var existWarehouse = await _warehouseService.GetById(model.WarehouseId);
            if (existWarehouse == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Không tìm thấy kho với ID: {model.WarehouseId}", 404));
            }

            // Validate Supplier
            var existSupplier = await _supplierService.GetBySupplierId(model.SupplierId);
            if (existSupplier == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Không tìm thấy nhà cung cấp với ID: {model.SupplierId}", 404));
            }

            // Validate ExpireDate
            if (model.ExpireDate <= DateTime.UtcNow)
            {
                return BadRequest(ApiResponse<object>.Fail("Ngày hết hạn phải sau ngày hiện tại.", 400));
            }

            // Validate List Products
            if (model.Products == null || model.Products.Count == 0)
            {
                return BadRequest(ApiResponse<object>.Fail("Danh sách sản phẩm không được để trống.", 400));
            }

            // Validate từng product trong list
            foreach (var product in model.Products)
            {
                var existProduct = await _productService.GetById(product.ProductId);
                if (existProduct == null)
                {
                    return NotFound(ApiResponse<object>.Fail($"Không tìm thấy sản phẩm với ID: {product.ProductId}", 404));
                }
            }

            try
            {
                // Tạo Transaction
                var newTransaction = new TransactionDto
                {
                    SupplierId = model.SupplierId,
                    WarehouseId = model.WarehouseId,
                    Type = "Import",
                    Status = 1, // Mặc định
                    TransactionDate = DateTime.UtcNow,
                    Note = model.Note
                };
                await _transactionService.CreateAsync(newTransaction);
                int transactionId = newTransaction.TransactionId;

                // Tao TransactionDetails
                foreach (var product in model.Products)
                {
                    var transactionDetail = new TransactionDetailDto
                    {
                        TransactionId = transactionId,
                        ProductId = product.ProductId,
                        Quantity = product.Quantity,
                        UnitPrice = product.UnitPrice,
                        Subtotal = product.Quantity * product.UnitPrice
                    };
                    await _transactionDetailService.CreateAsync(transactionDetail);
                }

                
                string batchCodePrefix = "BATCH-NUMBER";

                // Update Inventory và tạo StockBatch cho từng product
                int batchCounter = 1;
                List<StockOutputVM> resultList = new List<StockOutputVM>();

                // Inventory cache để xử lý trường hợp nhiều item cùng ProductId trong 1 request
                Dictionary<string, InventoryDto> inventoryCache = new Dictionary<string, InventoryDto>();

                foreach (var product in model.Products)
                {

                    // Tạo BatchCode
                    string uniqueBatchCode = $"{batchCodePrefix}{batchCounter:D4}";

                    while (await _stockBatchService.GetByName(uniqueBatchCode) != null)
                    {
                        batchCounter++;
                        uniqueBatchCode = $"{batchCodePrefix}{batchCounter:D4}";
                    }

                    // Tạo StockBatch
                    var newStockBatch = new StockBatchDto
                    {
                        WarehouseId = model.WarehouseId,
                        ProductId = product.ProductId,
                        TransactionId = transactionId,
                        BatchCode = uniqueBatchCode,
                        ImportDate = DateTime.UtcNow,
                        ExpireDate = model.ExpireDate,
                        QuantityIn = product.Quantity,
                        Status = 1, // Đã nhập kho
                        IsActive = true,
                        LastUpdated = DateTime.UtcNow,
                        Note = model.Note
                    };
                    await _stockBatchService.CreateAsync(newStockBatch);

                    // Update Inventory with Cache 
                    string inventoryKey = $"{model.WarehouseId}_{product.ProductId}";

                    if (!inventoryCache.ContainsKey(inventoryKey))
                    {
                        // Lần đầu gặp ProductId này: Load từ DB
                        var existInventory = await _inventoryService.GetByWarehouseAndProductId(model.WarehouseId, product.ProductId);

                        if (existInventory == null)
                        {
                            // Tạo mới Inventory trong cache
                            var newInventory = new InventoryDto
                            {
                                WarehouseId = model.WarehouseId,
                                ProductId = product.ProductId,
                                Quantity = product.Quantity,
                                AverageCost = product.UnitPrice,
                                LastUpdated = DateTime.UtcNow
                            };
                            inventoryCache[inventoryKey] = newInventory;
                        }
                        else
                        {
                            // Đã tồn tại trong DB: Update với weighted average cost
                            decimal? totalCost = (existInventory.Quantity * existInventory.AverageCost) + (product.Quantity * product.UnitPrice);
                            decimal? totalQuantity = existInventory.Quantity + product.Quantity;
                            existInventory.AverageCost = totalCost / totalQuantity;
                            existInventory.Quantity = totalQuantity;
                            existInventory.LastUpdated = DateTime.UtcNow;
                            inventoryCache[inventoryKey] = existInventory;
                        }
                    }
                    else
                    {
                        // Đã gặp ProductId này trong request : Cộng dồn trong cache
                        var cachedInventory = inventoryCache[inventoryKey];
                        decimal? totalCost = (cachedInventory.Quantity * cachedInventory.AverageCost) + (product.Quantity * product.UnitPrice);
                        decimal? totalQuantity = cachedInventory.Quantity + product.Quantity;
                        cachedInventory.AverageCost = totalCost / totalQuantity;
                        cachedInventory.Quantity = totalQuantity;
                        cachedInventory.LastUpdated = DateTime.UtcNow;
                    }

                    var resultItem = new StockOutputVM
                    {
                        BatchId = newStockBatch.BatchId,
                        WarehouseId = newStockBatch.WarehouseId,
                        WarehouseName = existWarehouse.WarehouseName,
                        ProductName = (await _productService.GetById(product.ProductId))?.ProductName,
                        TransactionId = transactionId,
                        ProductionFinishId = null,
                        BatchCode = uniqueBatchCode,
                        ImportDate = newStockBatch.ImportDate,
                        ExpireDate = newStockBatch.ExpireDate,
                        QuantityIn = newStockBatch.QuantityIn,
                        QuantityOut = newStockBatch.QuantityOut,
                        Status = newStockBatch.Status,
                        IsActive = newStockBatch.IsActive ?? false,
                        Note = newStockBatch.Note
                    };
                    resultList.Add(resultItem);
                    batchCounter++;
                }

                // Load Inventory vào db
                foreach (var inv in inventoryCache)
                {
                    var inventory = inv.Value;

                    if (inventory.InventoryId == 0)
                    {
                        // InventoryId = 0 nghĩa là mới tạo, chưa có trong DB
                        await _inventoryService.CreateAsync(inventory);
                    }
                    else
                    {
                        // Đã tồn tại, update
                        await _inventoryService.UpdateAsync(inventory);
                    }
                }

                return Ok(ApiResponse<List<StockOutputVM>>.Ok(resultList));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo lô nhập kho mới");
                return BadRequest(ApiResponse<object>.Fail("Có lỗi xảy ra khi tạo lô nhập kho.", 400));
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

                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    stream.Position = 0;

                    using (var package = new ExcelPackage(stream))
                    {
                        // Đọc Sheet "Thông tin chung"
                        var infoSheet = package.Workbook.Worksheets.FirstOrDefault(s => s.Name == "Thông tin chung");
                        if (infoSheet == null)
                        {
                            return BadRequest(ApiResponse<StockBatchImportResultVM>.Fail("Không tìm thấy sheet 'Thông tin chung'", 400));
                        }

                        // Đọc dòng 3 (dữ liệu)
                        string warehouseName = infoSheet.Cells[3, 1].Value?.ToString()?.Trim();
                        string supplierName = infoSheet.Cells[3, 2].Value?.ToString()?.Trim();
                        var expireDateCell = infoSheet.Cells[3, 3];

                        // Validate thông tin chung
                        if (string.IsNullOrWhiteSpace(warehouseName))
                            validationErrors.Add("Sheet 'Thông tin chung': WarehouseName không được để trống");

                        if (string.IsNullOrWhiteSpace(supplierName))
                            validationErrors.Add("Sheet 'Thông tin chung': SupplierName không được để trống");

                        DateTime expireDate = DateTime.MinValue;
                        bool isValidDate = false;
                        if (expireDateCell.Value is double doubleValue)
                        {
                            try
                            {
                                expireDate = DateTime.FromOADate(doubleValue);
                                isValidDate = true;
                            }
                            catch { isValidDate = false; }
                        }

                        if (!isValidDate)
                            validationErrors.Add($"Sheet 'Thông tin chung': ExpireDate không hợp lệ. Giá trị: '{expireDateCell.Value}'");
                        else if (expireDate <= DateTime.UtcNow)
                            validationErrors.Add("Sheet 'Thông tin chung': ExpireDate phải sau ngày hiện tại");

                        // Lookup Warehouse và Supplier
                        var warehouse = !string.IsNullOrWhiteSpace(warehouseName)
                            ? await _warehouseService.GetByWarehouseName(warehouseName)
                            : null;
                        if (warehouse == null && !string.IsNullOrWhiteSpace(warehouseName))
                            validationErrors.Add($"Không tìm thấy kho với tên: {warehouseName}");

                        var supplier = !string.IsNullOrWhiteSpace(supplierName)
                            ? await _supplierService.GetByName(supplierName)
                            : null;
                        if (supplier == null && !string.IsNullOrWhiteSpace(supplierName))
                            validationErrors.Add($"Không tìm thấy nhà cung cấp với tên: {supplierName}");

                        // Nếu có lỗi ở thông tin chung, dừng luôn
                        if (validationErrors.Any())
                        {
                            return BadRequest(ApiResponse<StockBatchImportResultVM>.Fail(validationErrors, 400));
                        }

                        // Đọc Sheet "Danh sách sản phẩm"
                        var productSheet = package.Workbook.Worksheets.FirstOrDefault(s => s.Name == "Danh sách sản phẩm");
                        if (productSheet == null)
                        {
                            return BadRequest(ApiResponse<StockBatchImportResultVM>.Fail("Không tìm thấy sheet 'Danh sách sản phẩm'", 400));
                        }

                        var rowCount = productSheet.Dimension?.Rows ?? 0;
                        if (rowCount < 3)
                        {
                            return BadRequest(ApiResponse<StockBatchImportResultVM>.Fail("Sheet 'Danh sách sản phẩm' không có dữ liệu", 400));
                        }

                        result.TotalRows = rowCount - 2; // Trừ header và description

                        // Class để chứa dữ liệu sản phẩm đã validate
                        var validatedProducts = new List<(
                            int rowNumber,
                            int productId,
                            int quantity,
                            decimal unitPrice,
                            string note,
                            string productName
                        )>();

                        // VALIDATE TẤT CẢ CÁC DÒNG SẢN PHẨM (Sheet 2)
                        for (int row = 3; row <= rowCount; row++)
                        {
                            try
                            {
                                // Đọc dữ liệu: ProductName, Quantity, UnitPrice, Note
                                var productName = productSheet.Cells[row, 1].Value?.ToString()?.Trim();
                                var quantityStr = productSheet.Cells[row, 2].Value?.ToString()?.Trim();
                                var unitPriceStr = productSheet.Cells[row, 3].Value?.ToString()?.Trim();
                                var note = productSheet.Cells[row, 4].Value?.ToString()?.Trim();

                                var rowErrors = new List<string>();

                                // Validate ProductName
                                bool productNameValid = !string.IsNullOrWhiteSpace(productName);
                                if (!productNameValid)
                                {
                                    rowErrors.Add($"Dòng {row}: Tên sản phẩm không được để trống");
                                }

                                // Validate Quantity
                                int quantity = 0;
                                bool quantityValid = !string.IsNullOrWhiteSpace(quantityStr) && int.TryParse(quantityStr, out quantity) && quantity > 0;
                                if (!quantityValid)
                                {
                                    rowErrors.Add($"Dòng {row}: Số lượng phải là số nguyên lớn hơn 0");
                                }

                                // Validate UnitPrice
                                decimal unitPrice = 0;
                                bool unitPriceValid = !string.IsNullOrWhiteSpace(unitPriceStr) && decimal.TryParse(unitPriceStr, out unitPrice) && unitPrice > 0;
                                if (!unitPriceValid)
                                {
                                    rowErrors.Add($"Dòng {row}: Giá nhập phải là số lớn hơn 0");
                                }

                                // Lookup Product
                                var existProduct = productNameValid ? await _productService.GetByProductName(productName) : null;
                                if (productNameValid && existProduct == null)
                                {
                                    rowErrors.Add($"Dòng {row}: Không tìm thấy sản phẩm với tên: {productName}");
                                }

                                // Nếu có lỗi, thêm vào list và skip
                                if (rowErrors.Any())
                                {
                                    validationErrors.AddRange(rowErrors);
                                    continue;
                                }

                                // Thêm vào list đã validate
                                validatedProducts.Add((
                                    row,
                                    existProduct.ProductId,
                                    quantity,
                                    unitPrice,
                                    note,
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
                            result.TotalRows = validatedProducts.Count;
                            result.FailedCount = result.TotalRows;
                            result.SuccessCount = 0;
                            result.ErrorMessages = validationErrors;

                            return BadRequest(ApiResponse<StockBatchImportResultVM>.Fail(
                                validationErrors,
                                400));
                        }

                        // Tạo Transaction
                        var newTransaction = new TransactionDto
                        {
                            SupplierId = supplier.SupplierId,
                            WarehouseId = warehouse.WarehouseId,
                            Type = "Import",
                            Status = 1, // Completed
                            TransactionDate = DateTime.UtcNow,
                            Note = $"Import từ Excel - NCC: {supplier.SupplierName} → Kho: {warehouse.WarehouseName}"
                        };
                        await _transactionService.CreateAsync(newTransaction);
                        int transactionId = newTransaction.TransactionId;

                        // Inventory cache
                        Dictionary<string, InventoryDto> inventoryCache = new Dictionary<string, InventoryDto>();

                        // BatchCode prefix
                        string batchCodePrefix = "BATCH-NUMBER";
                        int batchCounter = 1;

                        // Tạo TransactionDetails, StockBatches và cập nhật Inventory
                        foreach (var product in validatedProducts)
                        {
                            // Tạo TransactionDetail
                            var transactionDetail = new TransactionDetailDto
                            {
                                TransactionId = transactionId,
                                ProductId = product.productId,
                                Quantity = product.quantity,
                                UnitPrice = product.unitPrice,
                                Subtotal = product.quantity * product.unitPrice
                            };
                            await _transactionDetailService.CreateAsync(transactionDetail);

                            // Tạo BatchCode
                            string uniqueBatchCode = $"{batchCodePrefix}{batchCounter:D4}";
                            while (await _stockBatchService.GetByName(uniqueBatchCode) != null)
                            {
                                batchCounter++;
                                uniqueBatchCode = $"{batchCodePrefix}{batchCounter:D4}";
                            }

                            // Tạo StockBatch
                            var newStockBatch = new StockBatchDto
                            {
                                WarehouseId = warehouse.WarehouseId,
                                ProductId = product.productId,
                                TransactionId = transactionId,
                                BatchCode = uniqueBatchCode,
                                ImportDate = DateTime.UtcNow,
                                ExpireDate = expireDate,
                                QuantityIn = product.quantity,
                                Status = 1,
                                IsActive = true,
                                LastUpdated = DateTime.UtcNow,
                                Note = product.note
                            };
                            await _stockBatchService.CreateAsync(newStockBatch);

                            // Update Inventory cache
                            string inventoryKey = $"{warehouse.WarehouseId}_{product.productId}";

                            if (!inventoryCache.ContainsKey(inventoryKey))
                            {
                                // Load từ DB lần đầu
                                var existInventory = await _inventoryService.GetByWarehouseAndProductId(warehouse.WarehouseId, product.productId);

                                if (existInventory == null)
                                {
                                    // Tạo mới Inventory
                                    var newInventory = new InventoryDto
                                    {
                                        WarehouseId = warehouse.WarehouseId,
                                        ProductId = product.productId,
                                        Quantity = product.quantity,
                                        AverageCost = product.unitPrice,
                                        LastUpdated = DateTime.UtcNow
                                    };
                                    inventoryCache[inventoryKey] = newInventory;
                                }
                                else
                                {
                                    // Update với weighted average cost
                                    decimal? totalCost = (existInventory.Quantity * existInventory.AverageCost) + (product.quantity * product.unitPrice);
                                    decimal? totalQuantity = existInventory.Quantity + product.quantity;
                                    existInventory.AverageCost = totalCost / totalQuantity;
                                    existInventory.Quantity = totalQuantity;
                                    existInventory.LastUpdated = DateTime.UtcNow;
                                    inventoryCache[inventoryKey] = existInventory;
                                }
                            }
                            else
                            {
                                // Cộng dồn trong cache với weighted average
                                var cachedInventory = inventoryCache[inventoryKey];
                                decimal? totalCost = (cachedInventory.Quantity * cachedInventory.AverageCost) + (product.quantity * product.unitPrice);
                                decimal? totalQuantity = cachedInventory.Quantity + product.quantity;
                                cachedInventory.AverageCost = totalCost / totalQuantity;
                                cachedInventory.Quantity = totalQuantity;
                                cachedInventory.LastUpdated = DateTime.UtcNow;
                            }

                            // Tạo kết quả trả về 
                            var resultItem = new StockOutputVM
                            {
                                BatchId = newStockBatch.BatchId,
                                WarehouseId = newStockBatch.WarehouseId,
                                WarehouseName = warehouse.WarehouseName,
                                ProductName = product.productName,
                                TransactionId = transactionId,
                                ProductionFinishId = null,
                                BatchCode = uniqueBatchCode,
                                ImportDate = newStockBatch.ImportDate,
                                ExpireDate = newStockBatch.ExpireDate,
                                QuantityIn = newStockBatch.QuantityIn,
                                Status = newStockBatch.Status,
                                IsActive = newStockBatch.IsActive ?? false,
                                Note = newStockBatch.Note
                            };
                            result.ImportedStockBatches.Add(resultItem);
                            result.SuccessCount++;
                            batchCounter++;
                        }

                        // Load Inventory từ cache vào DB
                        foreach (var inv in inventoryCache)
                        {
                            var inventory = inv.Value;

                            if (inventory.InventoryId == 0)
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
