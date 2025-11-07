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
using OfficeOpenXml;
using System.Globalization;
using System.Security.Permissions;


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
                    TransactionStatus status = (TransactionStatus)item.Status;
                    var description = status.GetDescription();
                    list.Add(new TransactionOutputVM
                    {
                        TransactionId = item.TransactionId,
                        CustomerId = item.CustomerId,
                        TransactionDate = item.TransactionDate ?? DateTime.MinValue,
                        WarehouseName = (await _warehouseService.GetById(item.WarehouseId))?.WarehouseName ?? "N/A",
                        SupplierName = (await _supplierService.GetBySupplierId(item.SupplierId ?? 0))?.SupplierName ?? "N/A",
                        Type = item.Type,
                        Status = description,
                        Note = item.Note
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
                    ProductName = item.ProductName,
                    UnitPrice = item.UnitPrice,
                    WeightPerUnit = item.WeightPerUnit,
                    Quantity = item.Quantity,
                    SubTotal = item.Subtotal,
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
                    return BadRequest(ApiResponse<FullTransactionVM>.Fail("Có lỗi xảy ra khi lấy dữ liệu"));
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
                        Status = 6, // Mặc định
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
                if(model.Status != 5 && model.Status != 6)
                    return BadRequest(ApiResponse<string>.Fail("Trạng thái đơn hàng không hợp lệ.", 400));
                var oldDetails = await _transactionDetailService.GetByTransactionId(transactionId);
                if (oldDetails == null || !oldDetails.Any())
                {
                    return NotFound(ApiResponse<string>.Fail("Không tìm thấy chi tiết đơn hàng.", 404));
                }

                await _transactionDetailService.DeleteRange(oldDetails);

                var listProductId = listProductOrder.Select(p => p.ProductId).ToList();

                foreach (var po in listProductOrder)
                {
                    var inventory = await _inventoryService.GetByProductIdRetriveOneObject(po.ProductId);

                    var tranDetail = new TransactionDetailCreateVM
                    {
                        ProductId = po.ProductId,
                        TransactionId = transaction.TransactionId,
                        Quantity = (int)(po.Quantity ?? 0),
                        UnitPrice = (decimal)(po.UnitPrice ?? 0),
                        Subtotal = (po.UnitPrice ?? 0) * (po.Quantity ?? 0)
                    };
                    var tranDetailEntity = _mapper.Map<TransactionDetailCreateVM, TransactionDetail>(tranDetail);
                    await _transactionDetailService.CreateAsync(tranDetailEntity);
                }

                transaction.Status = model.Status;

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
    }

}

