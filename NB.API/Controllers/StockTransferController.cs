using Microsoft.AspNetCore.Mvc;
using NB.Model.Entities;
using NB.Model.Enums;
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
using NB.Service.TransactionDetailService.ViewModels;
using NB.Service.TransactionService;
using NB.Service.TransactionService.ViewModels;
using NB.Service.UserService;
using NB.Service.UserService.Dto;
using NB.Service.WarehouseService;

namespace NB.API.Controllers
{
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
                transactionEntity.TransactionDate = DateTime.Now;
                transactionEntity.Type = transactionType;
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
                        TransactionId = transactionEntity.TransactionId,
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
                }

                // 9️ Trả về kết quả sau khi hoàn tất toàn bộ sản phẩm
                return Ok(ApiResponse<string>.Ok("Chuyển kho thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi chuyển kho");
                return BadRequest(ApiResponse<string>.Fail("Có lỗi xảy ra khi chuyển kho"));
            }
        }
    }
}
