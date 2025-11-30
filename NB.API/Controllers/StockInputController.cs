using Microsoft.AspNetCore.Mvc;
using NB.Model.Entities;
using NB.Model.Enums;
using NB.Repository.WarehouseRepository;
using NB.Service.Common;
using NB.Service.Core.Enum;
using NB.Service.Core.Forms;
using NB.Service.Core.Mapper;
using NB.Service.Dto;
using NB.Service.InventoryService;
using NB.Service.InventoryService.Dto;
using NB.Service.ProductService;
using NB.Service.StockBatchService;
using NB.Service.StockBatchService.Dto;
using NB.Service.StockBatchService.ViewModels;
using NB.Service.SupplierService;
using NB.Service.SupplierService.Dto;
using NB.Service.SupplierService.ViewModels;
using NB.Service.TransactionDetailService;
using NB.Service.TransactionDetailService.Dto;
using NB.Service.TransactionDetailService.ViewModels;
using NB.Service.TransactionService;
using NB.Service.TransactionService.Dto;
using NB.Service.TransactionService.ViewModels;
using NB.Service.UserService.Dto;
using NB.Service.WarehouseService;
using NB.Services.StockBatchService.ViewModels;
using NB.Service.ReturnTransactionService;
using NB.Service.ReturnTransactionService.ViewModels;
using NB.Service.ReturnTransactionDetailService;
using NB.Service.ReturnTransactionDetailService.ViewModels;
using OfficeOpenXml;
using System.Globalization;
using System.Security.Permissions;
using NB.Service.FinancialTransactionService.ViewModels;
using NB.Service.FinancialTransactionService;


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
        private readonly IReturnTransactionService _returnTransactionService;
        private readonly IReturnTransactionDetailService _returnTransactionDetailService;
        private readonly IFinancialTransactionService _financialTransactionService;
        private readonly ILogger<StockInputController> _logger;
        private readonly IMapper _mapper;
        private readonly string transactionType = "Import";

        public StockInputController(IInventoryService inventoryService,
                                    ITransactionService transactionService,
                                    ITransactionDetailService transactionDetailService,
                                    IWarehouseService warehouseService,
                                    IProductService productService,
                                    IStockBatchService stockBatchService,
                                    ISupplierService supplierService,
                                    IReturnTransactionService returnTransactionService,
                                    IReturnTransactionDetailService returnTransactionDetailService,
                                    IFinancialTransactionService financialTransactionService,
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
            _returnTransactionService = returnTransactionService;
            _returnTransactionDetailService = returnTransactionDetailService;
            _financialTransactionService = financialTransactionService;
            _logger = logger;
            _mapper = mapper;
        }

        [HttpPost("GetData")]
        public async Task<IActionResult> GetData([FromBody] TransactionSearch search)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ", 400));
            }

            try
            {

                var result = await _transactionService.GetData(search);
                List<TransactionOutputVM> list = new List<TransactionOutputVM>();
                foreach (var item in result.Items)
                {
                    list.Add(new TransactionOutputVM
                    {
                        TransactionId = item.TransactionId,
                        CustomerId = item.CustomerId,
                        TransactionDate = item.TransactionDate ?? DateTime.MinValue,
                        WarehouseName = (await _warehouseService.GetById(item.WarehouseId))?.WarehouseName ?? "N/A",
                        SupplierName = (await _supplierService.GetBySupplierId(item.SupplierId ?? 0))?.SupplierName ?? "N/A",
                        Type = item.Type,
                        Status = item.Status,
                        Note = item.Note,
                        TotalCost = item.TotalCost
                    });
                }

                var pagedList = new PagedList<TransactionOutputVM>(
                    items: list,
                    pageIndex: result.PageIndex,
                    pageSize: result.PageSize,
                    totalCount: result.TotalCount
                );
                return Ok(ApiResponse<PagedList<TransactionOutputVM>>.Ok(pagedList));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy dữ liệu đơn hàng");
                return BadRequest(ApiResponse<PagedList<TransactionOutputVM>>.Fail("Có lỗi xảy ra khi lấy dữ liệu"));
            }
        }
        [HttpGet("GetDetail/{Id}")]
        public async Task<IActionResult> GetDetail(int Id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ", 400));
            }

            try
            {
                var transaction = new FullTransactionVM();
                if (Id > 0)
                {
                    var detail = await _transactionService.GetByTransactionId(Id);
                    if (detail != null)
                    {
                        transaction.TransactionId = detail.TransactionId;
                        transaction.TransactionDate = detail.TransactionDate ?? DateTime.MinValue;
                        transaction.WarehouseName = (await _warehouseService.GetById(detail.WarehouseId))?.WarehouseName ?? "N/A";
                        transaction.Status = detail.Status;
                        transaction.Note = detail.Note;
                        transaction.TotalCost = detail.TotalCost;
                        int id = detail.SupplierId ?? 0;
                        var supplier = await _supplierService.GetBySupplierId(id);
                        if(supplier != null)
                        {
                            
                            var supplierResult = new SupplierOutputVM
                            {
                                SupplierId = supplier.SupplierId,
                                SupplierName = supplier.SupplierName,
                                Email = supplier.Email,
                                Phone = supplier.Phone,
                                Status = supplier.IsActive switch
                                {
                                    false => "Ngừng hoạt động",
                                    true => "Đang hoạt động"
                                }
                            };
                            transaction.Supplier = supplierResult;
                        }
                        else
                        {
                            var supplierResult = new SupplierOutputVM
                            {
                                SupplierId = 0,
                                SupplierName = "N/A",
                                Email = "N/A",
                                Phone = "N/A",
                                Status = "N/A"
                            };
                            transaction.Supplier = supplierResult;
                        }                         
                    }
                    else
                    {
                        return NotFound(ApiResponse<FullTransactionVM>.Fail("Không tìm thấy đơn hàng.", 404));
                    }
                }
                else if(Id <=0)
                {
                    return BadRequest(ApiResponse<FullTransactionVM>.Fail("Id không hợp lệ", 400));
                }

                var productDetails = await _transactionDetailService.GetByTransactionId(Id);
                if(productDetails.Count == 0)
                {
                    return NotFound(ApiResponse<FullTransactionVM>.Fail("Không có thông tin cho giao dịch này.", 400));
                }
                foreach (var item in productDetails)
                {
                    var product = await _productService.GetById(item.ProductId);
                    item.ProductName = product != null ? product.ProductName : "N/A";
                    item.Code = product != null ? product.ProductCode : "N/A";
                }
                var batches = await _stockBatchService.GetByTransactionId(Id);
                if(batches.Count == 0)
                {
                    return NotFound(ApiResponse<FullTransactionVM>.Fail("Không có lô hàng nào cho giao dịch này.", 400));
                }
                var batch = batches.FirstOrDefault();


                var listResult = productDetails.Select(item => new TransactionDetailOutputVM
                {
                    TransactionDetailId = item.Id,
                    ProductId = item.ProductId,
                    Code = item.Code ?? "N/A",
                    ProductName = item.ProductName ?? "N/A",
                    UnitPrice = item.UnitPrice,
                    WeightPerUnit = item.WeightPerUnit,
                    Quantity = item.Quantity,
                    ExpireDate = batch.ExpireDate,
                    Note = batch.Note
                }).ToList();

                transaction.list = listResult;
                return Ok(ApiResponse<FullTransactionVM>.Ok(transaction));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy dữ liệu đơn hàng");
                return BadRequest(ApiResponse<PagedList<SupplierDto>>.Fail("Có lỗi xảy ra khi lấy dữ liệu"));
            }
        }


        [HttpPost("GetStockBatch")]
        public async Task<IActionResult> GetStockBatch([FromBody] StockBatchSearch search)
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

                    // Lấy TransactionDetails để map UnitPrice
                    var transactionDetails = await _transactionDetailService.GetByTransactionId(search.TransactionId);
                    var unitPriceMap = transactionDetails.ToDictionary(td => td.ProductId, td => td.UnitPrice);

                    var list = pagedResult.Items.Select(item => new StockOutputVM
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
                        Status = item.Status switch
                        {
                            0 => "Đã hết hàng",
                            1 => "Còn hàng",
                            2 => "Hết hạn",
                            3 => "Lô hỏng",
                            _ => "Không xác định"
                        },
                        IsActive = item.IsActive switch
                        {
                            false => "Đã đóng",
                            true => "Đang hoạt động"
                        },
                        Note = item.Note,
                        UnitPrice = unitPriceMap.ContainsKey(item.ProductId) ? unitPriceMap[item.ProductId] : (decimal?)null,
                        WeightPerUnit = _productService.GetById(item.ProductId).Result?.WeightPerUnit
                    }).ToList();

                    var result = new PagedList<StockOutputVM>(
                        items: list,
                        pageIndex: pagedResult.PageIndex,
                        pageSize: pagedResult.PageSize,
                        totalCount: pagedResult.TotalCount
                    );
                    return Ok(ApiResponse<PagedList<StockOutputVM>>.Ok(result));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi lấy dữ liệu lô hàng");
                    return BadRequest(ApiResponse<StockOutputVM>.Fail("Có lỗi xảy ra khi lấy dữ liệu"));
                }
            }

        [HttpGet("GetStockBatchById/{Id}")]
        public async Task<IActionResult> GetStockBatchById(int Id)
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ", 400));
                }
                try
                {
                    if (Id <= 0)
                    {
                        return BadRequest(ApiResponse<object>.Fail("Id không hợp lệ", 400));
                    }
                    var result = await _stockBatchService.GetByTransactionId(Id);
                    if (result == null || result.Count == 0)
                    {
                        return NotFound(ApiResponse<List<StockBatchDto>>.Fail("Không tìm thấy lô hàng nào.", 404));
                    }

                    return Ok(ApiResponse<List<StockBatchDto>>.Ok(result));

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi lấy dữ liệu lô hàng");
                    return BadRequest(ApiResponse<List<StockBatchDto>>.Fail("Có lỗi xảy ra khi lấy dữ liệu"));
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

                decimal totalCost = 0;
                decimal totalWeight = 0;
                // Validate từng product trong list và tính tổng trọng lượng
                foreach (var product in model.Products)
                {
                    if(product.Quantity <= 0)
                    {
                        return BadRequest(ApiResponse<object>.Fail($"Số lượng sản phẩm với ID: {product.ProductId} phải lớn hơn 0.", 400));
                    }
                    var existProduct = await _productService.GetById(product.ProductId);
                    if (existProduct == null)
                    {
                        return NotFound(ApiResponse<object>.Fail($"Không tìm thấy sản phẩm với ID: {product.ProductId}", 404));
                    }
                    totalWeight += product.Quantity * (existProduct.WeightPerUnit ?? 0);
                    totalCost += product.Quantity * product.UnitPrice;
                }

                try
                {
                    // Tạo Transaction
                    var newTransaction = new TransactionDto
                    {
                        SupplierId = model.SupplierId,
                        WarehouseId = model.WarehouseId,
                        Type = "Import",
                        Status = 1, // Mặc định - Đang kiểm
                        TransactionDate = DateTime.UtcNow,
                        TotalWeight = totalWeight,
                        TotalCost = totalCost,
                        TransactionCode = $"IMPORT-{DateTime.UtcNow:yyyyMMddHHmmss}",
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
                            UnitPrice = product.UnitPrice
                            //,Subtotal = product.Quantity * product.UnitPrice
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
                                    LastUpdated = DateTime.UtcNow
                                };
                                inventoryCache[inventoryKey] = newInventory;
                            }
                            else
                            {
                                // Đã tồn tại trong DB: Update với weighted average cost
                                decimal? totalQuantity = existInventory.Quantity + product.Quantity;
                                existInventory.Quantity = totalQuantity;
                                existInventory.LastUpdated = DateTime.UtcNow;
                                inventoryCache[inventoryKey] = existInventory;
                            }
                        }
                        else
                        {
                            // Đã gặp ProductId này trong request : Cộng dồn trong cache
                            var cachedInventory = inventoryCache[inventoryKey];
                            decimal? totalQuantity = cachedInventory.Quantity + product.Quantity;
                            cachedInventory.Quantity = totalQuantity;
                            cachedInventory.LastUpdated = DateTime.UtcNow;
                        }

                        // Lấy thông tin Product để có WeightPerUnit
                        var productInfo = await _productService.GetById(product.ProductId);

                        var resultItem = new StockOutputVM
                        {
                            BatchId = newStockBatch.BatchId,
                            WarehouseId = newStockBatch.WarehouseId,
                            WarehouseName = existWarehouse.WarehouseName,
                            ProductName = productInfo?.ProductName,
                            TransactionId = transactionId,
                            ProductionFinishId = null,
                            BatchCode = uniqueBatchCode,
                            ImportDate = newStockBatch.ImportDate,
                            ExpireDate = newStockBatch.ExpireDate,
                            QuantityIn = newStockBatch.QuantityIn,
                            QuantityOut = newStockBatch.QuantityOut,
                            Status = newStockBatch.Status switch
                            {
                                0 => "Đã hết hàng",
                                1 => "Còn hàng",
                                2 => "Hết hạn",
                                3 => "Lô hỏng",
                                _ => "Không xác định"
                            },
                            IsActive = newStockBatch.IsActive switch
                            {
                                false => "Đã đóng",
                                true => "Đang hoạt động"
                            },
                            Note = newStockBatch.Note,
                            UnitPrice = product.UnitPrice,
                            WeightPerUnit = productInfo?.WeightPerUnit
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
                            // Đọc Sheet "Nhập kho" (gộp thông tin chung và danh sách sản phẩm)
                            var mainSheet = package.Workbook.Worksheets.FirstOrDefault(s => s.Name == "Nhập kho");
                            if (mainSheet == null)
                            {
                                return BadRequest(ApiResponse<StockBatchImportResultVM>.Fail("Không tìm thấy sheet 'Nhập kho'", 400));
                            }

                            // Kiểm tra có dữ liệu không
                            var rowCount = mainSheet.Dimension?.Rows ?? 0;
                            if (rowCount < 3)
                            {
                                return BadRequest(ApiResponse<StockBatchImportResultVM>.Fail("Sheet 'Nhập kho' không có dữ liệu", 400));
                            }

                            // Đọc thông tin chung từ dòng 3, cột A, B, C
                            string warehouseName = mainSheet.Cells[3, 1].Value?.ToString()?.Trim();
                            string supplierName = mainSheet.Cells[3, 2].Value?.ToString()?.Trim();
                            var expireDateCell = mainSheet.Cells[3, 3];

                            // Validate thông tin chung
                            if (string.IsNullOrWhiteSpace(warehouseName))
                                validationErrors.Add("Dòng 3: WarehouseName không được để trống");

                            if (string.IsNullOrWhiteSpace(supplierName))
                                validationErrors.Add("Dòng 3: SupplierName không được để trống");

                            DateTime expireDate = DateTime.MinValue;
                            bool isValidDate = false;

                            // Thử parse từ nhiều định dạng khác nhau
                            if (expireDateCell.Value is double doubleValue)
                            {
                                // Trường hợp 1: Excel lưu dưới dạng số (OADate)
                                try
                                {
                                    expireDate = DateTime.FromOADate(doubleValue);
                                    isValidDate = true;
                                }
                                catch { isValidDate = false; }
                            }
                            else if (expireDateCell.Value is string stringValue && !string.IsNullOrWhiteSpace(stringValue))
                            {
                                // Trường hợp 2: Người dùng nhập text (hỗ trợ nhiều format)
                                var formats = new[]
                                {
                                    "MM/dd/yyyy",   // 12/31/2025
                                    "MM-dd-yyyy",   // 12-31-2025
                                    "dd/MM/yyyy",   // 31/12/2025
                                    "dd-MM-yyyy",   // 31-12-2025
                                    "yyyy-MM-dd",   // 2025-12-31
                                    "yyyy/MM/dd",   // 2025/12/31
                                    "M/d/yyyy",     // 12/31/2025 (không có số 0 đầu)
                                    "d/M/yyyy"      // 31/12/2025 (không có số 0 đầu)
                                };

                                isValidDate = DateTime.TryParseExact(
                                    stringValue.Trim(),
                                    formats,
                                    CultureInfo.InvariantCulture,
                                    DateTimeStyles.None,
                                    out expireDate
                                );

                                // Nếu TryParseExact không thành công, thử parse tự động
                                if (!isValidDate)
                                {
                                    isValidDate = DateTime.TryParse(stringValue.Trim(), out expireDate);
                                }
                            }
                            else if (expireDateCell.Value is DateTime dateTimeValue)
                            {
                                // Trường hợp 3: Đã là DateTime
                                expireDate = dateTimeValue;
                                isValidDate = true;
                            }

                            if (!isValidDate)
                                validationErrors.Add($"Dòng 3: ExpireDate không hợp lệ. Giá trị: '{expireDateCell.Value}'. Vui lòng nhập theo định dạng MM/DD/YYYY (ví dụ: 12/31/2025)");
                            else if (expireDate <= DateTime.UtcNow)
                                validationErrors.Add("Dòng 3: ExpireDate phải sau ngày hiện tại");

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


                            // VALIDATE TẤT CẢ CÁC DÒNG SẢN PHẨM (từ dòng 3, cột D, E, F, G)
                            decimal totalCost = 0;
                            decimal totalWeight = 0;
                            for (int row = 3; row <= rowCount; row++)
                            {
                                try
                                {
                                    // Đọc dữ liệu sản phẩm từ cột D, E, F, G (4, 5, 6, 7)
                                    var productName = mainSheet.Cells[row, 4].Value?.ToString()?.Trim();  // Cột D: ProductName
                                    var quantityStr = mainSheet.Cells[row, 5].Value?.ToString()?.Trim();  // Cột E: Quantity
                                    var unitPriceStr = mainSheet.Cells[row, 6].Value?.ToString()?.Trim(); // Cột F: UnitPrice
                                    var note = mainSheet.Cells[row, 7].Value?.ToString()?.Trim();         // Cột G: Note

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
                                    totalWeight += quantity * (existProduct?.WeightPerUnit ?? 0);
                                    totalCost += quantity * unitPrice;

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
                                TotalWeight = totalWeight,
                                TotalCost = totalCost,
                                TransactionCode = $"IMPORT-{DateTime.UtcNow:yyyyMMdd}",
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
                                    UnitPrice = product.unitPrice
                                    //,Subtotal = product.quantity * product.unitPrice
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
                                            LastUpdated = DateTime.UtcNow
                                        };
                                        inventoryCache[inventoryKey] = newInventory;
                                    }
                                    else
                                    {
                                        // Update với weighted average cost
                                        decimal? totalQuantity = existInventory.Quantity + product.quantity;
                                        existInventory.Quantity = totalQuantity;
                                        existInventory.LastUpdated = DateTime.UtcNow;
                                        inventoryCache[inventoryKey] = existInventory;
                                    }
                                }
                                else
                                {
                                    // Cộng dồn trong cache với weighted average
                                    var cachedInventory = inventoryCache[inventoryKey];
                                    decimal? totalQuantity = cachedInventory.Quantity + product.quantity;
                                    cachedInventory.Quantity = totalQuantity;
                                    cachedInventory.LastUpdated = DateTime.UtcNow;
                                }

                                // Lấy thông tin Product từ database để có WeightPerUnit
                                var productInfo = await _productService.GetById(product.productId);

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
                                    Status = newStockBatch.Status switch
                                    {
                                        0 => "Đã hết hàng",
                                        1 => "Còn hàng",
                                        2 => "Hết hạn",
                                        3 => "Lô hỏng",
                                        _ => "Không xác định"
                                    },
                                    IsActive = newStockBatch.IsActive switch
                                    {
                                        false => "Đã đóng",
                                        true => "Đang hoạt động"
                                    },
                                    Note = newStockBatch.Note,
                                    UnitPrice = product.unitPrice,
                                    WeightPerUnit = productInfo?.WeightPerUnit
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

        [HttpPut("UpdateImportTransaction/{transactionId}")]
        public async Task<IActionResult> UpdateImport(int transactionId, [FromBody] TransactionEditVM model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ", 400));
            }

            var listProductOrder = model.ListProductOrder;
            try
            {
                var transaction = await _transactionService.GetByIdAsync(transactionId);
                if (transaction == null)
                    return NotFound(ApiResponse<string>.Fail("Không tìm thấy đơn hàng nhập kho.", 404));
                if(transaction.Status == 6)
                    return BadRequest(ApiResponse<string>.Fail("Không thể cập nhật đơn hàng đã kiểm.", 400));
                if(transaction.Type != null && transaction.Type == "Export")
                    return BadRequest(ApiResponse<string>.Fail("Không thể cập nhật đơn hàng này.", 400));
                var oldDetails = await _transactionDetailService.GetByTransactionId(transactionId);
                if (oldDetails == null || !oldDetails.Any())
                {
                    return NotFound(ApiResponse<string>.Fail("Không tìm thấy chi tiết đơn hàng.", 404));
                }

                await _transactionDetailService.DeleteRange(oldDetails);

                var listProductId = listProductOrder.Select(p => p.ProductId).ToList();

                decimal totalCost = 0;
                decimal totalWeight = 0;
                // Tính tổng trọng lượng và tổng chi phí từ danh sách sản phẩm
                foreach (var po in listProductOrder)
                {
                    var quantity = (decimal)(po.Quantity ?? 0);
                    var unitPrice = (decimal)(po.UnitPrice ?? 0);

                    if (quantity <= 0)
                    {
                        return BadRequest(ApiResponse<string>.Fail($"Số lượng sản phẩm với ID: {po.ProductId} phải lớn hơn 0.", 400));
                    }

                    var existProduct = await _productService.GetById(po.ProductId);
                    if (existProduct == null)
                    {
                        return NotFound(ApiResponse<string>.Fail($"Không tìm thấy sản phẩm với ID: {po.ProductId}", 404));
                    }

                    totalWeight += quantity * (existProduct.WeightPerUnit ?? 0);
                    totalCost += quantity * unitPrice;

                    var inventory = await _inventoryService.GetByProductIdRetriveOneObject(po.ProductId);

                    var tranDetail = new TransactionDetailCreateVM
                    {
                        ProductId = po.ProductId,
                        TransactionId = transaction.TransactionId,
                        Quantity = (int)quantity,
                        UnitPrice = unitPrice,
                    };
                    var tranDetailEntity = _mapper.Map<TransactionDetailCreateVM, TransactionDetail>(tranDetail);
                    await _transactionDetailService.CreateAsync(tranDetailEntity);
                }

                // Cập nhật TotalWeight và TotalCost
                transaction.TotalWeight = totalWeight;
                transaction.TotalCost = totalCost;


                if (!string.IsNullOrEmpty(model.Note))
                {
                    transaction.Note = model.Note;
                }
                await _transactionService.UpdateAsync(transaction);

                return Ok(ApiResponse<string>.Ok("Cập nhật đơn hàng thành công."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật đơn hàng");
                return BadRequest(ApiResponse<string>.Fail("Có lỗi xảy ra khi cập nhật đơn hàng."));
            }
        }

        [HttpDelete("DeleteImportTransaction/{Id}")]
        public async Task<IActionResult> DeleteImportTransaction(int Id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ", 400));
            }
            if (Id <= 0)
            {
                return BadRequest(ApiResponse<PagedList<SupplierDto>>.Fail("Id không hợp lệ", 400));
            }
            try
            {
                var transaction = await _transactionService.GetByTransactionId(Id);
                if (transaction == null)
                {
                    return NotFound(ApiResponse<PagedList<SupplierDto>>.Fail("Không tìm thấy giao dịch nhập kho", 404));
                }
                if (transaction.Type == "Export")
                {
                    return BadRequest(ApiResponse<PagedList<SupplierDto>>.Fail("Giao dịch không phải là nhập kho", 400));
                }
                if(transaction.Status == 0)
                {
                    return BadRequest(ApiResponse<PagedList<SupplierDto>>.Fail("Giao dịch đã bị hủy từ trước.", 400));
                }
                transaction.Status = 0; // Đặt trạng thái là hủy
                await _transactionService.UpdateAsync(transaction);
                return Ok(ApiResponse<object>.Ok("Đã hủy giao dịch nhập kho thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi hủy giao dịch nhập kho");
                return BadRequest(ApiResponse<object>.Fail("Có lỗi xảy ra khi hủy giao dịch nhập kho.", 400));

            }
        }

        [HttpPut("UpdateToCheckingStatus/{transactionId}")]
        public async Task<IActionResult> SetStatusChecking(int transactionId)
        {
            try
            {
                if (transactionId <= 0)
                {
                    return BadRequest(ApiResponse<object>.Fail("Transaction ID không hợp lệ", 400));
                }

                var transaction = await _transactionService.GetByTransactionId(transactionId);
                if (transaction == null)
                {
                    return NotFound(ApiResponse<object>.Fail("Không tìm thấy giao dịch", 404));
                }

                if (string.IsNullOrEmpty(transaction.Type) || !transaction.Type.Equals("Import", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(ApiResponse<object>.Fail("Giao dịch không phải là loại Import", 400));
                }

                transaction.Status = 1; // Đang kiểm hàng
                await _transactionService.UpdateAsync(transaction);

                return Ok(ApiResponse<object>.Ok("Đã cập nhật trạng thái: Đang kiểm hàng"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật trạng thái Đang kiểm hàng");
                return BadRequest(ApiResponse<object>.Fail("Có lỗi xảy ra khi cập nhật trạng thái", 400));
            }
        }

        [HttpPut("UpdateToCheckedStatus/{transactionId}")]
        public async Task<IActionResult> SetStatusChecked(int transactionId)
        {
            try
            {
                if (transactionId <= 0)
                {
                    return BadRequest(ApiResponse<object>.Fail("Transaction ID không hợp lệ", 400));
                }

                var transaction = await _transactionService.GetByTransactionId(transactionId);
                if (transaction == null)
                {
                    return NotFound(ApiResponse<object>.Fail("Không tìm thấy giao dịch", 404));
                }

                if (string.IsNullOrEmpty(transaction.Type) || !transaction.Type.Equals("Import", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(ApiResponse<object>.Fail("Giao dịch không phải là loại Import", 400));
                }

                transaction.Status = 2; // Đã kiểm hàng
                await _transactionService.UpdateAsync(transaction);

                return Ok(ApiResponse<object>.Ok("Đã cập nhật trạng thái: Đã kiểm hàng"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật trạng thái Đã kiểm hàng");
                return BadRequest(ApiResponse<object>.Fail("Có lỗi xảy ra khi cập nhật trạng thái", 400));
            }
        }

        [HttpPut("UpdateToRefundStatus/{transactionId}")]
        public async Task<IActionResult> SetStatusRefund(int transactionId)
        {
            try
            {
                if (transactionId <= 0)
                {
                    return BadRequest(ApiResponse<object>.Fail("Transaction ID không hợp lệ", 400));
                }

                var transaction = await _transactionService.GetByTransactionId(transactionId);
                if (transaction == null)
                {
                    return NotFound(ApiResponse<object>.Fail("Không tìm thấy giao dịch", 404));
                }

                if (string.IsNullOrEmpty(transaction.Type) || !transaction.Type.Equals("Import", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(ApiResponse<object>.Fail("Giao dịch không phải là loại Import", 400));
                }

                transaction.Status = 3; // Trả hàng
                await _transactionService.UpdateAsync(transaction);

                return Ok(ApiResponse<object>.Ok("Đã cập nhật trạng thái: Trả hàng"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật trạng thái Trả hàng");
                return BadRequest(ApiResponse<object>.Fail("Có lỗi xảy ra khi cập nhật trạng thái", 400));
            }
        }

        /// <summary>
        /// Chuyển trạng thái đơn hàng sang thanh toán tất
        /// </summary>
        /// <param name="transactionId"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut("UpdateToPaidInFullStatus/{transactionId}")]
        public async Task<IActionResult> UpdateToPaidInFullStatus(int transactionId, FinancialTransactionCreateVM model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<FinancialTransactionCreateVM>.Fail("Dữ liệu không hợp lệ"));
            }
            var transaction = await _transactionService.GetByTransactionId(transactionId);
            if (transaction == null)
            {
                return NotFound(ApiResponse<TransactionDto>.Fail("Không tìm thấy đơn hàng", 404));
            }
            //Kiểm tra xem đơn hàng có phải đơn nhập
            if (transaction.Type != transactionType)
            {
                return BadRequest(ApiResponse<TransactionDto>.Fail("Đơn hàng không phải đơn nhập", 400));
            }
            if (transaction.Status == (int)TransactionStatus.paidInFull || transaction.Status == (int)TransactionStatus.partiallyPaid)
            {
                return BadRequest(ApiResponse<Transaction>.Fail("Đơn hàng đã được thanh toán hoặc thanh toán một phần"));
            }
            var financialTransactions = await _financialTransactionService.GetByRelatedTransactionID(transactionId);
            // Kiểm tra nếu đã có thanh toán trước đó, không cho phép thanh toán đầy đủ
            // Phải sử dụng CreatePartialPayment nếu đã có thanh toán một phần
            if (financialTransactions != null && financialTransactions.Any())
            {
                return BadRequest(ApiResponse<Transaction>.Fail("Đơn hàng đã có thanh toán trước đó. Vui lòng sử dụng thanh toán một phần"));
            }
            try
            {
                var financialTransactionEntity = _mapper.Map<FinancialTransactionCreateVM, FinancialTransaction>(model);
                financialTransactionEntity.RelatedTransactionId = transaction.TransactionId;
                financialTransactionEntity.Amount = - (transaction.TotalCost ?? 0);
                financialTransactionEntity.TransactionDate = DateTime.Now;
                financialTransactionEntity.Type = FinancialTransactionType.ThanhToanNhanHang.ToString();

                await _financialTransactionService.CreateAsync(financialTransactionEntity);

                //cap nhat trang thai cho don hang
                transaction.Status = (int)TransactionStatus.paidInFull;
                await _transactionService.UpdateAsync(transaction);
                // 6️ Trả về kết quả sau khi hoàn tất toàn bộ sản phẩm
                return Ok(ApiResponse<string>.Ok("Cập nhật đơn hàng thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật trạng thái đơn hàng");
                return BadRequest(ApiResponse<string>.Fail("Có lỗi xảy ra khi cập nhật trạng thái đơn hàng"));
            }
        }

        /// <summary>
        /// Trả một phần tiền giá trị đơn hàng
        /// </summary>
        /// <param name="transactionId"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut("CreatePartialPayment/{transactionId}")]
        public async Task<IActionResult> CreatePartialPayment(int transactionId, FinancialTransactionCreateVM model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<FinancialTransactionCreateVM>.Fail("Dữ liệu không hợp lệ"));
            }
            if (!model.Amount.HasValue)
            {
                return BadRequest(ApiResponse<FinancialTransactionCreateVM>.Fail("Số tiền trả không được để trống"));
            }
            var transaction = await _transactionService.GetByTransactionId(transactionId);
            if (transaction == null)
            {
                return NotFound(ApiResponse<TransactionDto>.Fail("Không tìm thấy đơn hàng", 404));
            }
            //Kiểm tra xem xem đơn hàng có phải đơn nhập
            if (transaction.Type != transactionType)
            {
                return BadRequest(ApiResponse<TransactionDto>.Fail("Đơn hàng không phải đơn nhập", 404));
            }
            if (transaction.Status == (int)TransactionStatus.paidInFull)
            {
                return BadRequest(ApiResponse<Transaction>.Fail("Đơn hàng đã được thanh toán"));
            }
            var financialTransactions = await _financialTransactionService.GetByRelatedTransactionID(transactionId);
            decimal totalPaid = 0;
            if (financialTransactions != null && financialTransactions.Any())
            {
                totalPaid = financialTransactions.Sum(ft => Math.Abs(ft.Amount));
            }

            decimal newPaymentAmount = model.Amount.Value;
            decimal totalAfterPayment = totalPaid + newPaymentAmount;
            decimal transactionTotalCost = transaction.TotalCost ?? 0;

            //Kiểm tra tổng số tiền trả có vượt giá trị đơn hàng
            if (totalAfterPayment > transactionTotalCost)
            {
                return BadRequest(ApiResponse<Transaction>.Fail("Tổng số tiền thanh toán vượt quá giá trị đơn hàng"));
            }

            try
            {
                var financialTransactionEntity = _mapper.Map<FinancialTransactionCreateVM, FinancialTransaction>(model);
                financialTransactionEntity.RelatedTransactionId = transaction.TransactionId;
                financialTransactionEntity.Amount = - newPaymentAmount;
                financialTransactionEntity.TransactionDate = DateTime.Now;
                financialTransactionEntity.Type = FinancialTransactionType.ThanhToanNhanHang.ToString();

                await _financialTransactionService.CreateAsync(financialTransactionEntity);

                //cap nhat trang thai cho don hang
                // Sử dụng tolerance để so sánh số thập phân (tránh lỗi floating point precision)
                const decimal tolerance = 0.01m;
                //Tính độ lệch giữa số tiền đã thanh toán và tổng tiền cho phép sai số +-0.01 nếu nhỏ hơn 0.01 thì coi như đã thanh toán tất
                if (Math.Abs(totalAfterPayment - transactionTotalCost) < tolerance || totalAfterPayment >= transactionTotalCost)
                {
                    transaction.Status = (int)TransactionStatus.paidInFull;
                }
                else
                {
                    transaction.Status = (int)TransactionStatus.partiallyPaid;
                }
                await _transactionService.UpdateAsync(transaction);
                // 6️ Trả về kết quả sau khi hoàn tất toàn bộ sản phẩm
                return Ok(ApiResponse<string>.Ok("Cập nhật đơn hàng thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật trạng thái đơn hàng");
                return BadRequest(ApiResponse<string>.Fail("Có lỗi xảy ra khi cập nhật trạng thái đơn hàng"));
            }
        }

        /// <summary>
        /// API để trả hàng nhập - Trả lại một số sản phẩm từ đơn nhập hàng
        /// </summary>
        /// <param name="transactionId">ID của đơn nhập hàng cần trả hàng</param>
        /// <param name="or">Danh sách sản phẩm và số lượng cần trả</param>
        /// <returns>Kết quả trả hàng</returns>
        [HttpPost("ReturnOrder/{transactionId}")]
        public async Task<IActionResult> ReturnOrder(int transactionId, [FromBody] OrderRequest or)
        {
            var returnRequest = or.ListProductOrder;
            if (returnRequest == null || !returnRequest.Any())
            {
                return BadRequest(ApiResponse<string>.Fail("Danh sách sản phẩm trả hàng không được rỗng.", 400));
            }

            try
            {
                // Lấy thông tin đơn nhập hàng
                var transaction = await _transactionService.GetByIdAsync(transactionId);
                if (transaction == null)
                    return NotFound(ApiResponse<string>.Fail("Không tìm thấy đơn nhập hàng", 404));

                // Kiểm tra đơn nhập hàng phải là loại Import
                if (string.IsNullOrEmpty(transaction.Type) || !transaction.Type.Equals("Import", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(ApiResponse<string>.Fail("Đơn hàng không phải là loại nhập hàng", 400));
                }

                // Lấy chi tiết đơn nhập hàng hiện tại
                var currentDetails = await _transactionDetailService.GetByTransactionId(transactionId);
                if (currentDetails == null || !currentDetails.Any())
                {
                    return NotFound(ApiResponse<string>.Fail("Không tìm thấy chi tiết đơn nhập hàng", 404));
                }

                // Gom nhóm sản phẩm trả hàng theo ProductId
                var returnProductDict = returnRequest
                    .GroupBy(p => p.ProductId)
                    .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity ?? 0));

                // Kiểm tra tính hợp lệ của yêu cầu trả hàng
                var currentProductDict = currentDetails
                    .GroupBy(d => d.ProductId)
                    .ToDictionary(g => g.Key, g => g.Sum(d => d.Quantity));

                foreach (var returnItem in returnProductDict)
                {
                    var productId = returnItem.Key;
                    var returnQuantity = returnItem.Value;

                    // Kiểm tra sản phẩm có trong đơn nhập hàng không
                    if (!currentProductDict.ContainsKey(productId))
                    {
                        var product = await _productService.GetByIdAsync(productId);
                        return BadRequest(ApiResponse<string>.Fail(
                            $"Sản phẩm '{product?.ProductName ?? productId.ToString()}' không có trong đơn nhập hàng này.", 400));
                    }

                    // Kiểm tra số lượng trả có hợp lệ không
                    var currentQuantity = currentProductDict[productId];
                    if (returnQuantity > currentQuantity)
                    {
                        var product = await _productService.GetByIdAsync(productId);
                        return BadRequest(ApiResponse<string>.Fail(
                            $"Số lượng trả của sản phẩm '{product?.ProductName ?? productId.ToString()}' ({returnQuantity}) vượt quá số lượng trong đơn ({currentQuantity}).", 400));
                    }

                    if (returnQuantity <= 0)
                    {
                        var product = await _productService.GetByIdAsync(productId);
                        return BadRequest(ApiResponse<string>.Fail(
                            $"Số lượng trả của sản phẩm '{product?.ProductName ?? productId.ToString()}' phải lớn hơn 0.", 400));
                    }
                }

                // Dictionary để track các update (tránh update trùng lặp)
                var inventoryUpdates = new Dictionary<int, Inventory>();
                var stockBatchUpdates = new Dictionary<int, StockBatch>();

                // Lấy tất cả StockBatch theo transactionId của đơn nhập hàng
                var allStockBatches = await _stockBatchService.GetByTransactionId(transactionId);
                if (allStockBatches == null || !allStockBatches.Any())
                {
                    return BadRequest(ApiResponse<string>.Fail("Không tìm thấy lô hàng cho đơn nhập hàng này.", 404));
                }

                // Xử lý từng sản phẩm trả hàng
                foreach (var returnItem in returnProductDict)
                {
                    var productId = returnItem.Key;
                    var returnQuantity = returnItem.Value;

                    // Lấy các StockBatch của sản phẩm này trong đơn nhập hàng
                    var productBatches = allStockBatches
                        .Where(b => b.ProductId == productId && (b.QuantityIn ?? 0) > 0)
                        .OrderByDescending(b => b.ImportDate) // LIFO - Last In First Out
                        .ToList();

                    if (!productBatches.Any())
                    {
                        var product = await _productService.GetByIdAsync(productId);
                        return BadRequest(ApiResponse<string>.Fail(
                            $"Không tìm thấy lô hàng cho sản phẩm '{product?.ProductName ?? productId.ToString()}' trong đơn nhập hàng này.", 404));
                    }

                    // Trả lại hàng theo LIFO
                    decimal remaining = returnQuantity;
                    foreach (var batchDto in productBatches)
                    {
                        if (remaining <= 0) break;

                        var availableIn = batchDto.QuantityIn ?? 0;
                        if (availableIn <= 0) continue;

                        var takeBack = Math.Min(availableIn, remaining);

                        // Lấy entity để update
                        if (stockBatchUpdates.ContainsKey(batchDto.BatchId))
                        {
                            stockBatchUpdates[batchDto.BatchId].QuantityIn -= takeBack;
                            if (stockBatchUpdates[batchDto.BatchId].QuantityIn < 0)
                                stockBatchUpdates[batchDto.BatchId].QuantityIn = 0;
                        }
                        else
                        {
                            var batchEntity = await _stockBatchService.GetByIdAsync(batchDto.BatchId);
                            if (batchEntity != null)
                            {
                                batchEntity.QuantityIn -= takeBack;
                                if (batchEntity.QuantityIn < 0) batchEntity.QuantityIn = 0;
                                batchEntity.LastUpdated = DateTime.Now;
                                stockBatchUpdates[batchDto.BatchId] = batchEntity;
                            }
                        }

                        remaining -= takeBack;
                    }

                    if (remaining > 0)
                    {
                        var product = await _productService.GetByIdAsync(productId);
                        return BadRequest(ApiResponse<string>.Fail(
                            $"Không đủ hàng trong các lô để trả cho sản phẩm '{product?.ProductName ?? productId.ToString()}'.", 400));
                    }

                    // Giảm Inventory
                    var inventoryEntity = await _inventoryService.GetEntityByProductIdAsync(productId);
                    if (inventoryEntity != null)
                    {
                        if (inventoryUpdates.ContainsKey(productId))
                        {
                            inventoryUpdates[productId].Quantity -= returnQuantity;
                        }
                        else
                        {
                            inventoryEntity.Quantity -= returnQuantity;
                            inventoryEntity.LastUpdated = DateTime.Now;
                            inventoryUpdates[productId] = inventoryEntity;
                        }
                    }
                }

                // Cập nhật tất cả Inventory
                foreach (var inventory in inventoryUpdates.Values)
                {
                    await _inventoryService.UpdateNoTracking(inventory);
                }

                // Cập nhật tất cả StockBatch
                foreach (var stockBatch in stockBatchUpdates.Values)
                {
                    await _stockBatchService.UpdateNoTracking(stockBatch);
                }

                // Tạo đơn trả
                var returnTran = new ReturnTransactionCreateVM
                {
                    TransactionId = transaction.TransactionId,
                    Reason = or.Reason
                };
                var returnTranEntity = _mapper.Map<ReturnTransactionCreateVM, ReturnTransaction>(returnTran);
                returnTranEntity.CreatedAt = DateTime.Now;
                await _returnTransactionService.CreateAsync(returnTranEntity);

                // Cập nhật TransactionDetail - Trừ số lượng hoặc xóa nếu trả hết
                foreach (var detail in currentDetails)
                {
                    if (returnProductDict.ContainsKey(detail.ProductId))
                    {
                        var returnQuantity = returnProductDict[detail.ProductId];
                        var newQuantity = detail.Quantity - returnQuantity;

                        //Tạo chi tiết đơn trả
                        var returnTranDetail = new ReturnTransactionDetailCreateVM
                        {
                            ProductId = detail.ProductId,
                            ReturnTransactionId = returnTranEntity.ReturnTransactionId,
                            Quantity = (int)returnQuantity,
                            UnitPrice = detail.UnitPrice
                        };
                        var returnTranDetailEntity = _mapper.Map<ReturnTransactionDetailCreateVM, ReturnTransactionDetail>(returnTranDetail);
                        await _returnTransactionDetailService.CreateAsync(returnTranDetailEntity);

                        if (newQuantity > 0)
                        {
                            // Còn sản phẩm - Cập nhật số lượng
                            var detailEntity = await _transactionDetailService.GetByIdAsync(detail.Id);
                            if (detailEntity != null)
                            {
                                detailEntity.Quantity = (int)newQuantity;
                                await _transactionDetailService.UpdateAsync(detailEntity);
                            }
                        }
                        else
                        {
                            // Trả hết - Xóa detail
                            var detailEntity = await _transactionDetailService.GetByIdAsync(detail.Id);
                            if (detailEntity != null)
                            {
                                await _transactionDetailService.DeleteAsync(detailEntity);
                            }
                        }
                    }
                }

                // Cập nhật tổng tiền đơn hàng
                if (or.TotalCost.HasValue)
                {
                    transaction.TotalCost -= or.TotalCost;
                    if (transaction.TotalCost < 0) transaction.TotalCost = 0;
                }
                // Cập nhật note
                if (!string.IsNullOrEmpty(or.Note))
                {
                    transaction.Note = or.Note;
                }

                // Kiểm tra nếu trả hết tất cả sản phẩm thì chuyển trạng thái về draft hoặc cancelled
                var remainingDetails = await _transactionDetailService.GetByTransactionId(transactionId);
                if (remainingDetails == null || !remainingDetails.Any())
                {
                    transaction.Status = (int?)TransactionStatus.draft;
                }

                await _transactionService.UpdateAsync(transaction);

                return Ok(ApiResponse<string>.Ok($"Trả hàng nhập thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý trả hàng nhập");
                return BadRequest(ApiResponse<string>.Fail("Có lỗi xảy ra khi xử lý trả hàng nhập"));
            }
        }
    }

}

