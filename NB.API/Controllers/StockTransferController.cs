using Microsoft.AspNetCore.Mvc;
using NB.Model.Entities;
using NB.Model.Enums;
using NB.Service.Core.Enum;
using NB.Service.Core.Mapper;
using NB.Service.Dto;
using NB.Service.InventoryService;
using NB.Service.InventoryService.Dto;
using NB.Service.ProductService;
using NB.Service.ProductService.Dto;
using NB.Service.ReturnTransactionDetailService;
using NB.Service.ReturnTransactionService;
using NB.Service.StockBatchService;
using NB.Service.StockBatchService.Dto;
using NB.Service.TransactionDetailService;
using NB.Service.TransactionDetailService.Dto;
using NB.Service.TransactionDetailService.ViewModels;
using NB.Service.TransactionService;
using NB.Service.TransactionService.ViewModels;
using NB.Service.UserService;
using NB.Service.UserService.Dto;
using NB.Service.WarehouseService;
using NB.Service.WarehouseService.Dto;
using NB.Service.TransactionService.Dto;
using NB.Service.Common;
using static System.DateTime;

namespace NB.API.Controllers
{
    [Route("api/stocktransfer")]
    public class StockTransferController : Controller
    {
        private readonly IInventoryService _inventoryService;
        private readonly IMapper _mapper;
        private readonly ILogger<EmployeeController> _logger;
        private readonly ITransactionService _transactionService;
        private readonly ITransactionDetailService _transactionDetailService;
        private readonly IProductService _productService;
        private readonly IUserService _userService;
        private readonly IStockBatchService _stockBatchService;
        private readonly IWarehouseService _warehouseService;
        private readonly string transactionType = "Transfer";

        public StockTransferController(
            ITransactionService transactionService,
            ITransactionDetailService transactionDetailService,
            IProductService productService,
            IStockBatchService stockBatchService,
            IUserService userService,
            IWarehouseService warehouseService,
            IInventoryService inventoryService,
            IReturnTransactionService returnTransactionService,
            IReturnTransactionDetailService returnTransactionDetailService,
            IMapper mapper,
            ILogger<EmployeeController> logger)
        {
            _transactionService = transactionService;
            _transactionDetailService = transactionDetailService;
            _productService = productService;
            _userService = userService;
            _stockBatchService = stockBatchService;
            _warehouseService = warehouseService;
            _inventoryService = inventoryService;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Lấy ra tất cả các đơn chuyển kho
        /// </summary>
        /// <param name="search">tìm các đơn chuyển kho theo các điều kiện</param>
        /// <returns>các đơn chuyển kho thỏa mãn các điều kiện của search nếu có</returns>
        [HttpPost("GetData")]
        public async Task<IActionResult> GetData([FromBody] TransactionSearch search)
        {
            try
            {
                search.Type = transactionType;
                var result = await _transactionService.GetDataForExport(search);
                if (result.Items == null || !result.Items.Any())
                {
                    return Ok(ApiResponse<PagedList<TransactionDto>>.Ok(result));
                }
                var listWarehouseId = result.Items.Select(t => t.WarehouseId).ToList();
                var listWarehouseInId = result.Items.Where(t => t.WarehouseInId.HasValue).Select(t => t.WarehouseInId.Value).ToList();
                var allWarehouseIds = listWarehouseId.Concat(listWarehouseInId).Distinct().ToList();
                
                var listWareHouse = await _warehouseService.GetByListWarehouseId(allWarehouseIds);
                
                if (listWareHouse == null || !listWareHouse.Any())
                {
                    return NotFound(ApiResponse<PagedList<WarehouseDto>>.Fail("Không tìm thấy kho"));
                }
                
                foreach (var t in result.Items)
                {
                    //lấy tên kho nguồn
                    var sourceWarehouse = listWareHouse?.FirstOrDefault(w => w != null && w.WarehouseId == t.WarehouseId);
                    if (sourceWarehouse != null)
                    {
                        t.WarehouseName = sourceWarehouse.WarehouseName;
                    }
                    
                    //lấy tên kho đích
                    if (t.WarehouseInId.HasValue)
                    {
                        var destWarehouse = listWareHouse?.FirstOrDefault(w => w != null && w.WarehouseId == t.WarehouseInId.Value);
                        if (destWarehouse != null)
                        {
                            t.WarehouseInName = destWarehouse.WarehouseName;
                        }
                    }
                    
                    //gắn statusName cho transaction
                    if (t.Status.HasValue)
                    {
                        TransactionStatus status = (TransactionStatus)t.Status.Value;
                        t.StatusName = status.GetDescription();
                    }
                }
                return Ok(ApiResponse<PagedList<TransactionDto>>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy dữ liệu đơn chuyển kho");
                return BadRequest(ApiResponse<PagedList<TransactionDto>>.Fail("Có lỗi xảy ra khi lấy dữ liệu"));
            }
        }

        /// <summary>
        /// Hàm để lấy ra chi tiết của đơn chuyển kho
        /// </summary>
        /// <param name="Id">TransactionId</param>
        /// <returns>Trả về chi tiết đơn chuyển kho bao gồm các sản phẩm có trong đơn</returns>
        [HttpGet("GetDetail/{Id}")]
        public async Task<IActionResult> GetDetail(int Id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ", 400));
            }

            try
            {
                var transaction = new FullTransactionTransferVM();
                if (Id > 0)
                {
                    var detail = await _transactionService.GetByTransactionId(Id);
                    if (detail != null)
                    {
                        transaction.Status = detail.Status;
                        transaction.TransactionId = detail.TransactionId;
                        transaction.TransactionDate = detail.TransactionDate ?? DateTime.MinValue;
                        transaction.TotalWeight = detail.TotalWeight;
                        transaction.Note = detail.Note;
                        
                        // Lấy thông tin kho nguồn
                        var sourceWarehouse = await _warehouseService.GetById(detail.WarehouseId);
                        transaction.SourceWarehouseName = sourceWarehouse?.WarehouseName ?? "N/A";
                        
                        // Lấy thông tin kho đích
                        if (detail.WarehouseInId.HasValue)
                        {
                            var destWarehouse = await _warehouseService.GetById(detail.WarehouseInId.Value);
                            transaction.DestinationWarehouseName = destWarehouse?.WarehouseName ?? "N/A";
                        }
                        else
                        {
                            transaction.DestinationWarehouseName = "N/A";
                        }
                    }
                    else
                    {
                        return NotFound(ApiResponse<FullTransactionTransferVM>.Fail("Không tìm thấy đơn chuyển kho.", 404));
                    }
                }
                else if (Id <= 0)
                {
                    return BadRequest(ApiResponse<FullTransactionTransferVM>.Fail("Id không hợp lệ", 400));
                }

                var productDetails = await _transactionDetailService.GetByTransactionId(Id);
                if (productDetails == null || !productDetails.Any())
                {
                    return NotFound(ApiResponse<FullTransactionTransferVM>.Fail("Không có thông tin cho giao dịch này.", 400));
                }
                
                foreach (var item in productDetails)
                {
                    var product = await _productService.GetById(item.ProductId);
                    item.ProductName = product != null ? product.ProductName : "N/A";
                    item.Code = product != null ? product.ProductCode : "N/A";
                }

                var listResult = productDetails.Select(item => new TransactionDetailOutputVM
                {
                    TransactionDetailId = item.Id,
                    ProductId = item.ProductId,
                    Code = item.Code ?? "N/A",
                    ProductName = item.ProductName ?? "N/A",
                    UnitPrice = item.UnitPrice,
                    WeightPerUnit = item.WeightPerUnit,
                    Quantity = item.Quantity
                }).ToList<TransactionDetailOutputVM?>();

                transaction.list = listResult ?? new List<TransactionDetailOutputVM?>();
                return Ok(ApiResponse<FullTransactionTransferVM>.Ok(transaction));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy dữ liệu đơn chuyển kho");
                return BadRequest(ApiResponse<FullTransactionTransferVM>.Fail("Có lỗi xảy ra khi lấy dữ liệu"));
            }
        }

        [HttpPost("CreateTransferOrder")]
        public async Task<IActionResult> CreateTransferOrder([FromBody] TransferRequest or)
        {
            var listProductOrder = or.ListProductOrder;

            if (listProductOrder == null || !listProductOrder.Any())
                return BadRequest(ApiResponse<ProductDto>.Fail("Không có sản phẩm nào", 404));

            // Kiểm tra kho nguồn và kho đích
            if (or.WarehouseId == or.WarehouseInId)
                return BadRequest(ApiResponse<string>.Fail("Kho nguồn và kho đích không thể giống nhau", 400));

            var sourceWarehouse = await _warehouseService.GetByIdAsync(or.WarehouseId);
            var destWarehouse = await _warehouseService.GetByIdAsync(or.WarehouseInId);
            if (sourceWarehouse == null)
                return NotFound(ApiResponse<string>.Fail("Không tìm thấy kho nguồn", 404));
            if (destWarehouse == null)
                return NotFound(ApiResponse<string>.Fail("Không tìm thấy kho đích", 404));

            var listProductId = listProductOrder.Select(p => p.ProductId).ToList();
            var listProduct = await _productService.GetByIds(listProductId);
            if (!listProduct.Any())
                return BadRequest(ApiResponse<ProductDto>.Fail("Không tìm thấy sản phẩm nào", 404));

            // Kiểm tra tồn kho ở kho nguồn
            var listInventorySource = await _inventoryService.GetByWarehouseAndProductIds(or.WarehouseId, listProductId);
            foreach (var po in listProductOrder)
            {
                var orderQty = po.Quantity ?? 0;
                var inven = listInventorySource.FirstOrDefault(p => p.ProductId == po.ProductId && p.WarehouseId == or.WarehouseId);

                if (inven == null)
                {
                    var productCheck = await _productService.GetByIdAsync(po.ProductId);
                    var productName = productCheck?.ProductName ?? $"Sản phẩm {po.ProductId}";
                    return BadRequest(ApiResponse<InventoryDto>.Fail(
                        $"Không tìm thấy sản phẩm '{productName}' trong kho nguồn '{sourceWarehouse.WarehouseName}'", 404));
                }

                var invenQty = inven.Quantity ?? 0;

                if (orderQty > invenQty)
                {
                    var productCheck = await _productService.GetByIdAsync(po.ProductId);
                    var productName = productCheck?.ProductName ?? $"Sản phẩm {po.ProductId}";
                    return BadRequest(ApiResponse<InventoryDto>.Fail(
                        $"Sản phẩm '{productName}' trong kho nguồn '{sourceWarehouse.WarehouseName}' chỉ còn {invenQty}, không đủ {orderQty} yêu cầu.",
                        400));
                }
            }

            try
            {
                // 1️ Tạo transaction (đơn chuyển kho)
                var tranCreate = new TransactionCreateVM
                {
                    WarehouseId = or.WarehouseId, // Kho nguồn
                    WarehouseInId = or.WarehouseInId, // Kho đích
                    Note = or.Note,
                };
                var transactionEntity = _mapper.Map<TransactionCreateVM, Transaction>(tranCreate);
                transactionEntity.TransactionDate = Now;
                transactionEntity.Type = transactionType;
                transactionEntity.TransactionCode = $"TRANSFER-{Now:yyyyMMdd}";
                transactionEntity.Status = (int)TransactionStatus.inTransit;
                await _transactionService.CreateAsync(transactionEntity);

                // 2️ Lấy tất cả stockBatch từ kho nguồn, sắp xếp theo ImportDate (hàng cũ nhất trước - FIFO)
                var listStockBatchSource = await _stockBatchService.GetByProductIdForOrder(listProductId);
                var stockBatchSourceByProduct = listStockBatchSource
                    .Where(sb => sb.WarehouseId == or.WarehouseId 
                        && (sb.QuantityIn ?? 0) > (sb.QuantityOut ?? 0) // Còn hàng
                        && (sb.ExpireDate == null || sb.ExpireDate > DateTime.Today)) // Chưa hết hạn
                    .GroupBy(sb => sb.ProductId)
                    .ToDictionary(g => g.Key, g => g.OrderBy(sb => sb.ImportDate).ToList());

                // BatchCode prefix cho kho đích
                string batchCodePrefix = "BATCH-NUMBER";
                int batchCounter = 1;

                // Biến để tính TotalWeight
                decimal totalWeight = 0;

                // 3️ Xử lý từng sản phẩm để chuyển hàng
                foreach (var po in listProductOrder)
                {
                    var transferQty = po.Quantity ?? 0;
                    var productId = po.ProductId;

                    // Lấy các lô hàng từ kho nguồn (đã sắp xếp theo ImportDate - hàng cũ nhất trước)
                    if (!stockBatchSourceByProduct.ContainsKey(productId))
                    {
                        var productCheck = await _productService.GetByIdAsync(productId);
                        var productName = productCheck?.ProductName ?? $"Sản phẩm {productId}";
                        return BadRequest(ApiResponse<string>.Fail(
                            $"Không tìm thấy lô hàng khả dụng cho sản phẩm '{productName}' trong kho nguồn", 404));
                    }

                    var availableBatches = stockBatchSourceByProduct[productId];
                    decimal remaining = transferQty;
                    var batchesToTransfer = new List<(StockBatchDto batch, decimal quantity)>();

                    // Lấy hàng từ các lô cũ nhất trước (FIFO)
                    foreach (var batch in availableBatches)
                    {
                        if (remaining <= 0) break;
                        decimal available = (batch.QuantityIn ?? 0) - (batch.QuantityOut ?? 0);
                        if (available <= 0) continue;

                        decimal take = Math.Min(available, remaining);
                        batchesToTransfer.Add((batch, take));
                        remaining -= take;
                    }

                    if (remaining > 0)
                    {
                        var productCheck = await _productService.GetByIdAsync(productId);
                        var productName = productCheck?.ProductName ?? $"Sản phẩm {productId}";
                        return BadRequest(ApiResponse<string>.Fail(
                            $"Không đủ hàng trong các lô cho sản phẩm '{productName}' trong kho nguồn", 400));
                    }

                    // 4️ Cập nhật StockBatch ở kho nguồn (tăng QuantityOut)
                    foreach (var (batch, qty) in batchesToTransfer)
                    {
                        var batchEntity = await _stockBatchService.GetByIdAsync(batch.BatchId);
                        if (batchEntity != null)
                        {
                            batchEntity.QuantityOut = (batchEntity.QuantityOut ?? 0) + qty;
                            batchEntity.LastUpdated = DateTime.Now;
                            await _stockBatchService.UpdateAsync(batchEntity);
                        }
                    }

                    // 5️ Tạo StockBatch mới ở kho đích
                    // Tạo BatchCode
                    string uniqueBatchCode = $"{batchCodePrefix}{batchCounter:D4}";
                    while (await _stockBatchService.GetByName(uniqueBatchCode) != null)
                    {
                        batchCounter++;
                        uniqueBatchCode = $"{batchCodePrefix}{batchCounter:D4}";
                    }
                    batchCounter++;

                    // Lấy thông tin từ lô cũ nhất để giữ nguyên ExpireDate
                    var oldestBatch = batchesToTransfer.First().batch;
                    var newStockBatch = new StockBatchDto
                    {
                        WarehouseId = or.WarehouseInId, // Kho đích
                        ProductId = productId,
                        TransactionId = transactionEntity.TransactionId, // TransactionId của đơn chuyển kho
                        BatchCode = uniqueBatchCode,
                        ImportDate = DateTime.Now, // Ngày chuyển kho
                        ExpireDate = oldestBatch.ExpireDate, // Giữ nguyên hạn sử dụng
                        QuantityIn = transferQty,
                        QuantityOut = 0,
                        Status = 1,
                        IsActive = true,
                        LastUpdated = DateTime.Now,
                        Note = $"Chuyển từ kho {sourceWarehouse.WarehouseName}"
                    };
                    await _stockBatchService.CreateAsync(newStockBatch);

                    // 6️ Trừ Inventory ở kho nguồn
                    var sourceInventoryEntity = await _inventoryService.GetEntityByWarehouseAndProductIdAsync(or.WarehouseId, productId);
                    if (sourceInventoryEntity != null)
                    {
                        sourceInventoryEntity.Quantity = (sourceInventoryEntity.Quantity ?? 0) - transferQty;
                        sourceInventoryEntity.LastUpdated = DateTime.Now;
                        await _inventoryService.UpdateNoTracking(sourceInventoryEntity);
                    }

                    // 7️ Cộng Inventory ở kho đích (tạo mới nếu chưa có)
                    var destInventoryEntity = await _inventoryService.GetEntityByWarehouseAndProductIdAsync(or.WarehouseInId, productId);
                    if (destInventoryEntity != null)
                    {
                        destInventoryEntity.Quantity = (destInventoryEntity.Quantity ?? 0) + transferQty;
                        destInventoryEntity.LastUpdated = DateTime.Now;
                        await _inventoryService.UpdateNoTracking(destInventoryEntity);
                    }
                    else
                    {
                        // Tạo mới Inventory ở kho đích
                        var newInventory = new InventoryDto
                        {
                            WarehouseId = or.WarehouseInId,
                            ProductId = productId,
                            Quantity = transferQty,
                            LastUpdated = DateTime.Now
                        };
                        await _inventoryService.CreateAsync(newInventory);
                    }

                    // 8️ Tạo transaction detail
                    var tranDetail = new TransactionDetailCreateVM
                    {
                        ProductId = po.ProductId,
                        TransactionId = transactionEntity.TransactionId,
                        Quantity = (int)transferQty,
                        UnitPrice = (decimal)(po.UnitPrice ?? 0),
                    };
                    var tranDetailEntity = _mapper.Map<TransactionDetailCreateVM, TransactionDetail>(tranDetail);
                    await _transactionDetailService.CreateAsync(tranDetailEntity);

                    // Tính TotalWeight
                    var productForWeight = await _productService.GetByIdAsync(po.ProductId);
                    if (productForWeight != null && productForWeight.WeightPerUnit.HasValue)
                    {
                        totalWeight += productForWeight.WeightPerUnit.Value * transferQty;
                    }
                }

                // Cập nhật TotalWeight vào transaction
                transactionEntity.TotalWeight = totalWeight;
                await _transactionService.UpdateAsync(transactionEntity);

                // 9️ Trả về kết quả sau khi hoàn tất toàn bộ sản phẩm
                return Ok(ApiResponse<string>.Ok("Chuyển kho thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi chuyển kho");
                return BadRequest(ApiResponse<string>.Fail("Có lỗi xảy ra khi chuyển kho"));
            }
        }

        /// <summary>
        /// Cập nhật đơn chuyển kho
        /// </summary>
        /// <param name="transactionId">ID của đơn chuyển kho cần cập nhật</param>
        /// <param name="or">Thông tin cập nhật đơn chuyển kho</param>
        /// <returns>Kết quả cập nhật</returns>
        [HttpPut("UpdateTransferOrder/{transactionId}")]
        public async Task<IActionResult> UpdateTransferOrder(int transactionId, [FromBody] TransferRequest or)
        {
            // Bảo vệ trường hợp request không có sản phẩm
            if (or?.ListProductOrder == null || !or.ListProductOrder.Any())
            {
                return BadRequest(ApiResponse<string>.Fail("Đơn chuyển kho mới không có sản phẩm nào để cập nhật.", 400));
            }

            // Kiểm tra kho nguồn và kho đích
            if (or.WarehouseId == or.WarehouseInId)
                return BadRequest(ApiResponse<string>.Fail("Kho nguồn và kho đích không thể giống nhau", 400));

            // Gom các sản phẩm có cùng ProductId về một dòng, cộng dồn số lượng để tránh lỗi ToDictionary
            var listProductOrder = or.ListProductOrder
                .GroupBy(p => p.ProductId)
                .Select(g => new ProductOrder
                {
                    ProductId = g.Key,
                    Quantity = g.Sum(x => x.Quantity ?? 0),
                    UnitPrice = g.First().UnitPrice
                })
                .ToList();

            try
            {
                // Lấy entity transaction hiện tại
                var transaction = await _transactionService.GetByIdAsync(transactionId);
                if (transaction == null)
                    return NotFound(ApiResponse<string>.Fail("Không tìm thấy đơn chuyển kho", 404));

                // Kiểm tra loại transaction phải là Transfer
                if (transaction.Type != transactionType)
                {
                    return BadRequest(ApiResponse<string>.Fail("Đơn này không phải là đơn chuyển kho", 400));
                }

                // Kiểm tra trạng thái - chỉ cho phép cập nhật khi đang ở trạng thái inTransit
                if (transaction.Status == (int)TransactionStatus.transferred)
                {
                    return BadRequest(ApiResponse<string>.Fail("Không thể cập nhật đơn chuyển kho đã hoàn thành", 400));
                }
                if (transaction.Status == (int)TransactionStatus.cancel)
                {
                    return BadRequest(ApiResponse<string>.Fail("Không thể cập nhật đơn chuyển kho đã hủy", 400));
                }

                // Lấy thông tin kho
                var sourceWarehouse = await _warehouseService.GetByIdAsync(or.WarehouseId);
                var destWarehouse = await _warehouseService.GetByIdAsync(or.WarehouseInId);
                if (sourceWarehouse == null)
                    return NotFound(ApiResponse<string>.Fail("Không tìm thấy kho nguồn", 404));
                if (destWarehouse == null)
                    return NotFound(ApiResponse<string>.Fail("Không tìm thấy kho đích", 404));

                // --- 1️⃣ Lấy danh sách chi tiết cũ ---
                var oldDetails = await _transactionDetailService.GetByTransactionId(transactionId);
                if (oldDetails == null || !oldDetails.Any())
                {
                    return NotFound(ApiResponse<string>.Fail("Không tìm thấy chi tiết đơn chuyển kho", 404));
                }

                // Lấy tất cả StockBatch được tạo từ transaction này (ở kho đích)
                var destStockBatches = await _stockBatchService.GetByTransactionId(transactionId);
                var destStockBatchByProduct = destStockBatches
                    .Where(sb => sb.WarehouseId == transaction.WarehouseInId)
                    .GroupBy(sb => sb.ProductId)
                    .ToDictionary(g => g.Key, g => g.ToList());

                // Tạo dictionary để track các Inventory và StockBatch đã được update (tránh update lặp)
                var sourceInventoryUpdates = new Dictionary<int, Inventory>(); // Key: ProductId
                var destInventoryUpdates = new Dictionary<int, Inventory>(); // Key: ProductId
                var sourceStockBatchUpdates = new Dictionary<int, StockBatch>(); // Key: BatchId
                var destStockBatchToDelete = new List<int>(); // BatchId cần xóa

                // --- 2️⃣ Phân loại sản phẩm: giống nhau, mới, cũ ---
                var oldProductDict = oldDetails
                    .GroupBy(d => d.ProductId)
                    .ToDictionary(g => g.Key, g => g.Sum(d => d.Quantity));
                var newProductDict = listProductOrder
                    .ToDictionary(p => p.ProductId, p => p.Quantity ?? 0);

                var commonProducts = oldProductDict.Keys.Intersect(newProductDict.Keys).ToList();
                var newProducts = newProductDict.Keys.Except(oldProductDict.Keys).ToList();
                var removedProducts = oldProductDict.Keys.Except(newProductDict.Keys).ToList();

                // --- 3️⃣ Xử lý sản phẩm bị xóa (chỉ có trong đơn cũ) - Trả lại hàng về kho nguồn, xóa ở kho đích ---
                foreach (var productId in removedProducts)
                {
                    var oldQuantity = oldProductDict[productId];

                    // Trả lại Inventory ở kho nguồn
                    var sourceInventoryEntity = await _inventoryService.GetEntityByWarehouseAndProductIdAsync(transaction.WarehouseId, productId);
                    if (sourceInventoryEntity != null)
                    {
                        sourceInventoryEntity.Quantity += oldQuantity;
                        sourceInventoryEntity.LastUpdated = DateTime.Now;
                        sourceInventoryUpdates[productId] = sourceInventoryEntity;
                    }

                    // Trả lại StockBatch ở kho nguồn theo LIFO
                    var batchesToRevert = await _stockBatchService.GetByProductIdForOrder(new List<int> { productId });
                    if (batchesToRevert != null && batchesToRevert.Any())
                    {
                        var revertList = batchesToRevert
                            .Where(b => b.WarehouseId == transaction.WarehouseId
                                && (b.QuantityOut ?? 0) > 0)
                            .OrderByDescending(b => b.ImportDate)
                            .ToList();

                        decimal toRevert = oldQuantity;
                        foreach (var b in revertList)
                        {
                            if (toRevert <= 0) break;
                            var availableOut = b.QuantityOut ?? 0;
                            if (availableOut <= 0) continue;

                            var takeBack = Math.Min(availableOut, toRevert);
                            var batchEntity = await _stockBatchService.GetByIdAsync(b.BatchId);
                            if (batchEntity != null)
                            {
                                batchEntity.QuantityOut -= takeBack;
                                if (batchEntity.QuantityOut < 0) batchEntity.QuantityOut = 0;
                                batchEntity.LastUpdated = DateTime.Now;
                                sourceStockBatchUpdates[b.BatchId] = batchEntity;
                            }
                            toRevert -= takeBack;
                        }
                    }

                    // Xóa StockBatch ở kho đích
                    if (destStockBatchByProduct.ContainsKey(productId))
                    {
                        foreach (var batch in destStockBatchByProduct[productId])
                        {
                            destStockBatchToDelete.Add(batch.BatchId);
                        }
                    }

                    // Trừ Inventory ở kho đích
                    var destInventoryEntity = await _inventoryService.GetEntityByWarehouseAndProductIdAsync(transaction.WarehouseInId ?? 0, productId);
                    if (destInventoryEntity != null)
                    {
                        destInventoryEntity.Quantity -= oldQuantity;
                        if (destInventoryEntity.Quantity < 0) destInventoryEntity.Quantity = 0;
                        destInventoryEntity.LastUpdated = DateTime.Now;
                        destInventoryUpdates[productId] = destInventoryEntity;
                    }
                }

                // --- 4️⃣ Xử lý sản phẩm giống nhau (có trong cả 2 đơn) - Chỉ update chênh lệch ---
                foreach (var productId in commonProducts)
                {
                    var oldQuantity = oldProductDict[productId];
                    var newQuantity = newProductDict[productId];
                    var diff = newQuantity - oldQuantity;

                    if (diff == 0)
                    {
                        // Không thay đổi số lượng, không cần update gì
                        continue;
                    }
                    else if (diff > 0)
                    {
                        // Đơn mới nhiều hơn - Cần thêm hàng
                        // Kiểm tra đủ hàng không ở kho nguồn
                        var sourceInventoryDto = await _inventoryService.GetByWarehouseAndProductId(transaction.WarehouseId, productId);
                        if (sourceInventoryDto == null)
                        {
                            var product = await _productService.GetByIdAsync(productId);
                            return BadRequest(ApiResponse<string>.Fail(
                                $"Không tìm thấy sản phẩm '{product?.ProductName ?? productId.ToString()}' trong kho nguồn '{sourceWarehouse.WarehouseName}'.", 404));
                        }

                        if ((sourceInventoryDto.Quantity ?? 0) < diff)
                        {
                            var product = await _productService.GetByIdAsync(productId);
                            return BadRequest(ApiResponse<string>.Fail(
                                $"Sản phẩm '{product?.ProductName ?? productId.ToString()}' trong kho nguồn '{sourceWarehouse.WarehouseName}' chỉ còn {sourceInventoryDto.Quantity}, không đủ {diff} để tăng.", 400));
                        }

                        // Trừ Inventory ở kho nguồn
                        var sourceInventoryEntity = await _inventoryService.GetEntityByWarehouseAndProductIdAsync(transaction.WarehouseId, productId);
                        if (sourceInventoryEntity != null)
                        {
                            sourceInventoryEntity.Quantity -= diff;
                            sourceInventoryEntity.LastUpdated = DateTime.Now;
                            sourceInventoryUpdates[productId] = sourceInventoryEntity;
                        }

                        // Lấy thêm StockBatch từ kho nguồn
                        var listStockBatch = await _stockBatchService.GetByProductIdForOrder(new List<int> { productId });
                        if (listStockBatch == null || !listStockBatch.Any())
                        {
                            return BadRequest(ApiResponse<string>.Fail($"Không tìm thấy lô hàng khả dụng cho sản phẩm {productId} trong kho nguồn '{sourceWarehouse.WarehouseName}'.", 404));
                        }
                        var batches = listStockBatch
                            .Where(sb => sb.ProductId == productId
                                && sb.WarehouseId == transaction.WarehouseId
                                && ((sb.QuantityIn ?? 0) > (sb.QuantityOut ?? 0))
                                && (sb.ExpireDate == null || sb.ExpireDate > DateTime.Today))
                            .OrderBy(sb => sb.ImportDate) // FIFO
                            .ToList();

                        decimal remaining = diff;
                        foreach (var batch in batches)
                        {
                            if (remaining <= 0) break;
                            decimal available = ((batch.QuantityIn ?? 0) - (batch.QuantityOut ?? 0));
                            if (available <= 0) continue;

                            decimal take = Math.Min(available, remaining);

                            if (sourceStockBatchUpdates.ContainsKey(batch.BatchId))
                            {
                                sourceStockBatchUpdates[batch.BatchId].QuantityOut += take;
                            }
                            else
                            {
                                var batchEntity = await _stockBatchService.GetByIdAsync(batch.BatchId);
                                if (batchEntity != null)
                                {
                                    batchEntity.QuantityOut += take;
                                    batchEntity.LastUpdated = DateTime.Now;
                                    sourceStockBatchUpdates[batch.BatchId] = batchEntity;
                                }
                            }
                            remaining -= take;
                        }

                        if (remaining > 0)
                        {
                            return BadRequest(ApiResponse<string>.Fail($"Không đủ hàng trong các lô cho sản phẩm {productId}", 400));
                        }

                        // Cộng Inventory ở kho đích
                        var destInventoryEntity = await _inventoryService.GetEntityByWarehouseAndProductIdAsync(transaction.WarehouseInId ?? 0, productId);
                        if (destInventoryEntity != null)
                        {
                            destInventoryEntity.Quantity += diff;
                            destInventoryEntity.LastUpdated = DateTime.Now;
                            destInventoryUpdates[productId] = destInventoryEntity;
                        }
                        else
                        {
                            // Tạo mới Inventory ở kho đích
                            var newInventory = new InventoryDto
                            {
                                WarehouseId = transaction.WarehouseInId ?? 0,
                                ProductId = productId,
                                Quantity = diff,
                                LastUpdated = DateTime.Now
                            };
                            await _inventoryService.CreateAsync(newInventory);
                        }

                        // Tạo StockBatch mới ở kho đích cho phần tăng thêm
                        string batchCodePrefix = "BATCH-NUMBER";
                        int batchCounter = 1;
                        string uniqueBatchCode = $"{batchCodePrefix}{batchCounter:D4}";
                        while (await _stockBatchService.GetByName(uniqueBatchCode) != null)
                        {
                            batchCounter++;
                            uniqueBatchCode = $"{batchCodePrefix}{batchCounter:D4}";
                        }

                        var oldestBatch = batches.First();
                        var newStockBatch = new StockBatchDto
                        {
                            WarehouseId = transaction.WarehouseInId ?? 0,
                            ProductId = productId,
                            TransactionId = transactionId,
                            BatchCode = uniqueBatchCode,
                            ImportDate = DateTime.Now,
                            ExpireDate = oldestBatch.ExpireDate,
                            QuantityIn = diff,
                            QuantityOut = 0,
                            Status = 1,
                            IsActive = true,
                            LastUpdated = DateTime.Now,
                            Note = $"Chuyển từ kho {sourceWarehouse.WarehouseName}"
                        };
                        await _stockBatchService.CreateAsync(newStockBatch);
                    }
                    else
                    {
                        // Đơn mới ít hơn - Trả lại hàng về kho nguồn, trừ ở kho đích (diff < 0 nên cần trả lại |diff|)
                        var returnQuantity = Math.Abs(diff);

                        // Trả lại Inventory ở kho nguồn
                        var sourceInventoryEntity = await _inventoryService.GetEntityByWarehouseAndProductIdAsync(transaction.WarehouseId, productId);
                        if (sourceInventoryEntity != null)
                        {
                            sourceInventoryEntity.Quantity += returnQuantity;
                            sourceInventoryEntity.LastUpdated = DateTime.Now;
                            sourceInventoryUpdates[productId] = sourceInventoryEntity;
                        }

                        // Trả lại StockBatch ở kho nguồn theo LIFO
                        var batchesToRevert = await _stockBatchService.GetByProductIdForOrder(new List<int> { productId });
                        if (batchesToRevert != null && batchesToRevert.Any())
                        {
                            var revertList = batchesToRevert
                                .Where(b => b.WarehouseId == transaction.WarehouseId
                                    && (b.QuantityOut ?? 0) > 0)
                                .OrderByDescending(b => b.ImportDate)
                                .ToList();

                            decimal toRevert = returnQuantity;
                            foreach (var b in revertList)
                            {
                                if (toRevert <= 0) break;
                                var availableOut = b.QuantityOut ?? 0;
                                if (availableOut <= 0) continue;

                                var takeBack = Math.Min(availableOut, toRevert);

                                if (sourceStockBatchUpdates.ContainsKey(b.BatchId))
                                {
                                    sourceStockBatchUpdates[b.BatchId].QuantityOut -= takeBack;
                                    if (sourceStockBatchUpdates[b.BatchId].QuantityOut < 0)
                                        sourceStockBatchUpdates[b.BatchId].QuantityOut = 0;
                                }
                                else
                                {
                                    var batchEntity = await _stockBatchService.GetByIdAsync(b.BatchId);
                                    if (batchEntity != null)
                                    {
                                        batchEntity.QuantityOut -= takeBack;
                                        if (batchEntity.QuantityOut < 0) batchEntity.QuantityOut = 0;
                                        batchEntity.LastUpdated = DateTime.Now;
                                        sourceStockBatchUpdates[b.BatchId] = batchEntity;
                                    }
                                }
                                toRevert -= takeBack;
                            }
                        }

                        // Trừ Inventory ở kho đích
                        var destInventoryEntity = await _inventoryService.GetEntityByWarehouseAndProductIdAsync(transaction.WarehouseInId ?? 0, productId);
                        if (destInventoryEntity != null)
                        {
                            destInventoryEntity.Quantity -= returnQuantity;
                            if (destInventoryEntity.Quantity < 0) destInventoryEntity.Quantity = 0;
                            destInventoryEntity.LastUpdated = DateTime.Now;
                            destInventoryUpdates[productId] = destInventoryEntity;
                        }

                        // Xóa hoặc giảm StockBatch ở kho đích (LIFO - xóa lô mới nhất trước)
                        if (destStockBatchByProduct.ContainsKey(productId))
                        {
                            var batchesToReduce = destStockBatchByProduct[productId]
                                .OrderByDescending(b => b.ImportDate)
                                .ToList();

                            decimal toReduce = returnQuantity;
                            foreach (var batch in batchesToReduce)
                            {
                                if (toReduce <= 0) break;
                                if (batch.QuantityIn <= toReduce)
                                {
                                    // Xóa toàn bộ lô
                                    destStockBatchToDelete.Add(batch.BatchId);
                                    toReduce -= batch.QuantityIn ?? 0;
                                }
                                else
                                {
                                    // Giảm số lượng lô
                                    var batchEntity = await _stockBatchService.GetByIdAsync(batch.BatchId);
                                    if (batchEntity != null)
                                    {
                                        batchEntity.QuantityIn -= toReduce;
                                        if (batchEntity.QuantityIn < 0) batchEntity.QuantityIn = 0;
                                        batchEntity.LastUpdated = DateTime.Now;
                                        await _stockBatchService.UpdateAsync(batchEntity);
                                    }
                                    toReduce = 0;
                                }
                            }
                        }
                    }
                }

                // --- 5️⃣ Xử lý sản phẩm mới (chỉ có trong đơn mới) - Thêm mới như bình thường ---
                if (newProducts.Any())
                {
                    // Kiểm tra đủ hàng cho tất cả sản phẩm mới trước khi trừ tồn ở kho nguồn
                    var listInventory = await _inventoryService.GetByWarehouseAndProductIds(transaction.WarehouseId, newProducts) ?? new List<InventoryDto>();
                    foreach (var productId in newProducts)
                    {
                        var newQuantity = newProductDict[productId];
                        var inven = listInventory.FirstOrDefault(p => p.ProductId == productId && p.WarehouseId == transaction.WarehouseId);
                        if (inven == null)
                        {
                            var product = await _productService.GetByIdAsync(productId);
                            return BadRequest(ApiResponse<string>.Fail(
                                $"Không tìm thấy sản phẩm '{product?.ProductName ?? productId.ToString()}' trong kho nguồn '{sourceWarehouse.WarehouseName}'", 404));
                        }
                        if (inven.Quantity < newQuantity)
                        {
                            var product = await _productService.GetByIdAsync(productId);
                            return BadRequest(ApiResponse<string>.Fail(
                                $"Sản phẩm '{product?.ProductName}' trong kho nguồn '{sourceWarehouse.WarehouseName}' chỉ còn {inven.Quantity}, không đủ {newQuantity} yêu cầu.", 400));
                        }
                    }

                    // Lấy StockBatch cho các sản phẩm mới từ kho nguồn
                    var listStockBatch = await _stockBatchService.GetByProductIdForOrder(newProducts) ?? new List<StockBatchDto>();
                    string batchCodePrefix = "BATCH-NUMBER";
                    int batchCounter = 1;

                    foreach (var po in listProductOrder.Where(p => newProducts.Contains(p.ProductId)))
                    {
                        var batches = listStockBatch
                            .Where(sb => sb.ProductId == po.ProductId
                                && sb.WarehouseId == transaction.WarehouseId
                                && ((sb.QuantityIn ?? 0) > (sb.QuantityOut ?? 0))
                                && (sb.ExpireDate == null || sb.ExpireDate > DateTime.Today))
                            .OrderBy(sb => sb.ImportDate) // FIFO
                            .ToList();

                        decimal remaining = po.Quantity ?? 0;
                        foreach (var batch in batches)
                        {
                            if (remaining <= 0) break;
                            decimal available = ((batch.QuantityIn ?? 0) - (batch.QuantityOut ?? 0));
                            if (available <= 0) continue;

                            decimal take = Math.Min(available, remaining);
                            var batchEntity = await _stockBatchService.GetByIdAsync(batch.BatchId);
                            if (batchEntity != null)
                            {
                                batchEntity.QuantityOut += take;
                                batchEntity.LastUpdated = DateTime.Now;
                                sourceStockBatchUpdates[batch.BatchId] = batchEntity;
                            }
                            remaining -= take;
                        }

                        if (remaining > 0)
                        {
                            return BadRequest(ApiResponse<string>.Fail($"Không đủ hàng trong các lô cho sản phẩm {po.ProductId}", 400));
                        }

                        // Trừ Inventory ở kho nguồn
                        var sourceInventoryEntity = await _inventoryService.GetEntityByWarehouseAndProductIdAsync(transaction.WarehouseId, po.ProductId);
                        if (sourceInventoryEntity != null)
                        {
                            sourceInventoryEntity.Quantity -= po.Quantity ?? 0;
                            sourceInventoryEntity.LastUpdated = DateTime.Now;
                            sourceInventoryUpdates[po.ProductId] = sourceInventoryEntity;
                        }

                        // Cộng Inventory ở kho đích
                        var destInventoryEntity = await _inventoryService.GetEntityByWarehouseAndProductIdAsync(transaction.WarehouseInId ?? 0, po.ProductId);
                        if (destInventoryEntity != null)
                        {
                            destInventoryEntity.Quantity += po.Quantity ?? 0;
                            destInventoryEntity.LastUpdated = DateTime.Now;
                            destInventoryUpdates[po.ProductId] = destInventoryEntity;
                        }
                        else
                        {
                            // Tạo mới Inventory ở kho đích
                            var newInventory = new InventoryDto
                            {
                                WarehouseId = transaction.WarehouseInId ?? 0,
                                ProductId = po.ProductId,
                                Quantity = po.Quantity ?? 0,
                                LastUpdated = DateTime.Now
                            };
                            await _inventoryService.CreateAsync(newInventory);
                        }

                        // Tạo StockBatch mới ở kho đích
                        string uniqueBatchCode = $"{batchCodePrefix}{batchCounter:D4}";
                        while (await _stockBatchService.GetByName(uniqueBatchCode) != null)
                        {
                            batchCounter++;
                            uniqueBatchCode = $"{batchCodePrefix}{batchCounter:D4}";
                        }
                        batchCounter++;

                        var oldestBatch = batches.First();
                        var newStockBatch = new StockBatchDto
                        {
                            WarehouseId = transaction.WarehouseInId ?? 0,
                            ProductId = po.ProductId,
                            TransactionId = transactionId,
                            BatchCode = uniqueBatchCode,
                            ImportDate = DateTime.Now,
                            ExpireDate = oldestBatch.ExpireDate,
                            QuantityIn = po.Quantity ?? 0,
                            QuantityOut = 0,
                            Status = 1,
                            IsActive = true,
                            LastUpdated = DateTime.Now,
                            Note = $"Chuyển từ kho {sourceWarehouse.WarehouseName}"
                        };
                        await _stockBatchService.CreateAsync(newStockBatch);
                    }
                }

                // --- 6️⃣ Thực hiện update tất cả Inventory (mỗi cái chỉ 1 lần) ---
                foreach (var inventory in sourceInventoryUpdates.Values)
                {
                    await _inventoryService.UpdateNoTracking(inventory);
                }
                foreach (var inventory in destInventoryUpdates.Values)
                {
                    await _inventoryService.UpdateNoTracking(inventory);
                }

                // --- 7️⃣ Thực hiện update tất cả StockBatch ở kho nguồn (mỗi cái chỉ 1 lần) ---
                foreach (var stockBatch in sourceStockBatchUpdates.Values)
                {
                    await _stockBatchService.UpdateNoTracking(stockBatch);
                }

                // --- 8️⃣ Xóa StockBatch ở kho đích ---
                foreach (var batchId in destStockBatchToDelete)
                {
                    var batchEntity = await _stockBatchService.GetByIdAsync(batchId);
                    if (batchEntity != null)
                    {
                        await _stockBatchService.DeleteAsync(batchEntity);
                    }
                }

                // --- 9️⃣ Xóa và tạo lại TransactionDetail ---
                await _transactionDetailService.DeleteRange(oldDetails);

                decimal totalWeight = 0;
                foreach (var po in listProductOrder)
                {
                    var tranDetail = new TransactionDetailCreateVM
                    {
                        ProductId = po.ProductId,
                        TransactionId = transactionId,
                        Quantity = (int)(po.Quantity ?? 0),
                        UnitPrice = (decimal)(po.UnitPrice ?? 0),
                    };
                    var tranDetailEntity = _mapper.Map<TransactionDetailCreateVM, TransactionDetail>(tranDetail);
                    await _transactionDetailService.CreateAsync(tranDetailEntity);

                    // Tính TotalWeight
                    var productForWeight = await _productService.GetByIdAsync(po.ProductId);
                    if (productForWeight != null && productForWeight.WeightPerUnit.HasValue)
                    {
                        totalWeight += productForWeight.WeightPerUnit.Value * (po.Quantity ?? 0);
                    }
                }

                // --- 🔟 Cập nhật thông tin đơn chuyển kho ---
                if (!string.IsNullOrEmpty(or.Note))
                {
                    transaction.Note = or.Note;
                }
                // Cập nhật TotalWeight
                transaction.TotalWeight = totalWeight;
                await _transactionService.UpdateAsync(transaction);

                return Ok(ApiResponse<string>.Ok("Cập nhật đơn chuyển kho thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật đơn chuyển kho");
                return BadRequest(ApiResponse<string>.Fail("Có lỗi xảy ra khi cập nhật đơn chuyển kho"));
            }
        }

        /// <summary>
        /// Cập nhật trạng thái đơn chuyển kho sang "Đã Chuyển"
        /// </summary>
        /// <param name="transactionId">ID của đơn chuyển kho</param>
        /// <returns>Kết quả cập nhật</returns>
        [HttpPut("UpdateToTransferredStatus/{transactionId}")]
        public async Task<IActionResult> UpdateToTransferredStatus(int transactionId)
        {
            try
            {
                // Lấy thông tin đơn chuyển kho
                var transaction = await _transactionService.GetByIdAsync(transactionId);
                if (transaction == null)
                {
                    return NotFound(ApiResponse<string>.Fail("Không tìm thấy đơn chuyển kho", 404));
                }

                // Kiểm tra loại transaction phải là Transfer
                if (transaction.Type != transactionType)
                {
                    return BadRequest(ApiResponse<string>.Fail("Đơn này không phải là đơn chuyển kho", 400));
                }

                // Kiểm tra trạng thái hiện tại - chỉ cho phép chuyển từ inTransit sang transferred
                if (transaction.Status != (int)TransactionStatus.inTransit)
                {
                    if (transaction.Status == (int)TransactionStatus.transferred)
                    {
                        return BadRequest(ApiResponse<string>.Fail("Đơn chuyển kho đã ở trạng thái hoàn thành", 400));
                    }
                    else if (transaction.Status == (int)TransactionStatus.cancel)
                    {
                        return BadRequest(ApiResponse<string>.Fail("Không thể cập nhật đơn chuyển kho đã hủy", 400));
                    }
                    else
                    {
                        return BadRequest(ApiResponse<string>.Fail("Chỉ có thể cập nhật trạng thái khi đơn đang ở trạng thái 'Đang Chuyển'", 400));
                    }
                }

                // Cập nhật trạng thái
                transaction.Status = (int)TransactionStatus.transferred;
                await _transactionService.UpdateAsync(transaction);
                
                return Ok(ApiResponse<string>.Ok("Cập nhật đơn chuyển kho thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật trạng thái đơn chuyển kho");
                return BadRequest(ApiResponse<string>.Fail("Có lỗi xảy ra khi cập nhật trạng thái đơn chuyển kho"));
            }
        }

        /// <summary>
        /// Hủy đơn chuyển kho - Trả lại hàng về kho nguồn, xóa hàng ở kho đích
        /// </summary>
        /// <param name="transactionId">ID của đơn chuyển kho cần hủy</param>
        /// <returns>Kết quả hủy đơn</returns>
        [HttpPut("CancelTransferOrder/{transactionId}")]
        public async Task<IActionResult> CancelTransferOrder(int transactionId)
        {
            try
            {
                // Lấy thông tin đơn chuyển kho
                var transaction = await _transactionService.GetByIdAsync(transactionId);
                if (transaction == null)
                    return NotFound(ApiResponse<string>.Fail("Không tìm thấy đơn chuyển kho", 404));

                // Kiểm tra loại transaction phải là Transfer
                if (transaction.Type != transactionType)
                {
                    return BadRequest(ApiResponse<string>.Fail("Đơn này không phải là đơn chuyển kho", 400));
                }

                // Kiểm tra trạng thái - chỉ cho phép hủy khi đang ở trạng thái inTransit
                if (transaction.Status == (int)TransactionStatus.transferred)
                {
                    return BadRequest(ApiResponse<string>.Fail("Không thể hủy đơn chuyển kho đã hoàn thành", 400));
                }
                if (transaction.Status == (int)TransactionStatus.cancel)
                {
                    return BadRequest(ApiResponse<string>.Fail("Đơn chuyển kho đã được hủy trước đó", 400));
                }

                // Lấy chi tiết đơn chuyển kho
                var transactionDetails = await _transactionDetailService.GetByTransactionId(transactionId);
                if (transactionDetails == null || !transactionDetails.Any())
                {
                    return NotFound(ApiResponse<string>.Fail("Không tìm thấy chi tiết đơn chuyển kho", 404));
                }

                // Lấy tất cả StockBatch được tạo từ transaction này (ở kho đích)
                var destStockBatches = await _stockBatchService.GetByTransactionId(transactionId);
                var destStockBatchByProduct = destStockBatches
                    .Where(sb => sb.WarehouseId == transaction.WarehouseInId)
                    .GroupBy(sb => sb.ProductId)
                    .ToDictionary(g => g.Key, g => g.Sum(sb => sb.QuantityIn ?? 0));

                // Dictionary để track các update (tránh update trùng lặp)
                var sourceInventoryUpdates = new Dictionary<int, Inventory>();
                var destInventoryUpdates = new Dictionary<int, Inventory>();
                var sourceStockBatchUpdates = new Dictionary<int, StockBatch>();

                // Xử lý từng sản phẩm để trả lại hàng
                foreach (var detail in transactionDetails)
                {
                    var productId = detail.ProductId;
                    var quantity = detail.Quantity;

                    // Trả lại Inventory ở kho nguồn
                    var sourceInventoryEntity = await _inventoryService.GetEntityByWarehouseAndProductIdAsync(transaction.WarehouseId, productId);
                    if (sourceInventoryEntity != null)
                    {
                        sourceInventoryEntity.Quantity += quantity;
                        sourceInventoryEntity.LastUpdated = DateTime.Now;
                        sourceInventoryUpdates[productId] = sourceInventoryEntity;
                    }

                    // Trả lại StockBatch ở kho nguồn theo LIFO (Last In First Out)
                    var batchesToRevert = await _stockBatchService.GetByProductIdForOrder(new List<int> { productId });
                    if (batchesToRevert != null && batchesToRevert.Any())
                    {
                        var revertList = batchesToRevert
                            .Where(b => b.WarehouseId == transaction.WarehouseId
                                && (b.QuantityOut ?? 0) > 0)
                            .OrderByDescending(b => b.ImportDate)
                            .ToList();

                        decimal toRevert = quantity;
                        foreach (var b in revertList)
                        {
                            if (toRevert <= 0) break;
                            var availableOut = b.QuantityOut ?? 0;
                            if (availableOut <= 0) continue;

                            var takeBack = Math.Min(availableOut, toRevert);

                            if (sourceStockBatchUpdates.ContainsKey(b.BatchId))
                            {
                                sourceStockBatchUpdates[b.BatchId].QuantityOut -= takeBack;
                                if (sourceStockBatchUpdates[b.BatchId].QuantityOut < 0)
                                    sourceStockBatchUpdates[b.BatchId].QuantityOut = 0;
                            }
                            else
                            {
                                var batchEntity = await _stockBatchService.GetByIdAsync(b.BatchId);
                                if (batchEntity != null)
                                {
                                    batchEntity.QuantityOut -= takeBack;
                                    if (batchEntity.QuantityOut < 0) batchEntity.QuantityOut = 0;
                                    batchEntity.LastUpdated = DateTime.Now;
                                    sourceStockBatchUpdates[b.BatchId] = batchEntity;
                                }
                            }
                            toRevert -= takeBack;
                        }
                    }

                    // Trừ Inventory ở kho đích
                    if (destStockBatchByProduct.ContainsKey(productId))
                    {
                        var destInventoryEntity = await _inventoryService.GetEntityByWarehouseAndProductIdAsync(transaction.WarehouseInId ?? 0, productId);
                        if (destInventoryEntity != null)
                        {
                            var quantityToRemove = destStockBatchByProduct[productId];
                            destInventoryEntity.Quantity -= quantityToRemove;
                            if (destInventoryEntity.Quantity < 0) destInventoryEntity.Quantity = 0;
                            destInventoryEntity.LastUpdated = DateTime.Now;
                            destInventoryUpdates[productId] = destInventoryEntity;
                        }
                    }
                }

                // Cập nhật tất cả Inventory
                foreach (var inventory in sourceInventoryUpdates.Values)
                {
                    await _inventoryService.UpdateNoTracking(inventory);
                }
                foreach (var inventory in destInventoryUpdates.Values)
                {
                    await _inventoryService.UpdateNoTracking(inventory);
                }

                // Cập nhật tất cả StockBatch ở kho nguồn
                foreach (var stockBatch in sourceStockBatchUpdates.Values)
                {
                    await _stockBatchService.UpdateNoTracking(stockBatch);
                }

                // Xóa tất cả StockBatch ở kho đích được tạo từ transaction này
                foreach (var batch in destStockBatches.Where(sb => sb.WarehouseId == transaction.WarehouseInId))
                {
                    var batchEntity = await _stockBatchService.GetByIdAsync(batch.BatchId);
                    if (batchEntity != null)
                    {
                        await _stockBatchService.DeleteAsync(batchEntity);
                    }
                }

                // Cập nhật trạng thái đơn chuyển kho thành cancel
                transaction.Status = (int)TransactionStatus.cancel;
                await _transactionService.UpdateAsync(transaction);

                return Ok(ApiResponse<string>.Ok("Hủy đơn chuyển kho thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi hủy đơn chuyển kho");
                return BadRequest(ApiResponse<string>.Fail("Có lỗi xảy ra khi hủy đơn chuyển kho"));
            }
        }
    }
}
