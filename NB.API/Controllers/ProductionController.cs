using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NB.API.Utils;
using NB.Model.Entities;
using NB.Model.Enums;
using NB.Service.Core.Mapper;
using NB.Service.Dto;
using NB.Service.FinishproductService;
using NB.Service.FinishproductService.ViewModels;
using NB.Service.InventoryService;
using NB.Service.MaterialService;
using NB.Service.MaterialService.ViewModels;
using NB.Service.ProductionOrderService;
using NB.Service.ProductionOrderService.Dto;
using NB.Service.ProductionOrderService.ViewModels;
using NB.Service.Core.Enum;
using NB.Service.Common;
using NB.Service.ProductService;
using NB.Service.StockBatchService;
using NB.Service.StockBatchService.Dto;
using NB.Service.StockBatchService.ViewModels;
using NB.Service.WarehouseService;

namespace NB.API.Controllers
{
    [Route("api/production")]
    public class ProductionController : Controller
    {
        private readonly IMapper _mapper;
        private readonly ILogger<ProductionController> _logger;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IProductionOrderService _productionOrderService;
        private readonly IMaterialService _materialService;
        private readonly IFinishproductService _finishproductService;
        private readonly IProductService _productService;
        private readonly IInventoryService _inventoryService;
        private readonly IStockBatchService _stockBatchService;
        private readonly IWarehouseService _warehouseService;

        public ProductionController(
            IProductionOrderService productionOrderService,
            IMaterialService materialService,
            IFinishproductService finishproductService,
            IProductService productService,
            IInventoryService inventoryService,
            IStockBatchService stockBatchService,
            IWarehouseService warehouseService,
            IMapper mapper,
            ILogger<ProductionController> logger,
            ICloudinaryService cloudinaryService)
        {
            _productionOrderService = productionOrderService;
            _materialService = materialService;
            _finishproductService = finishproductService;
            _productService = productService;
            _inventoryService = inventoryService;
            _stockBatchService = stockBatchService;
            _warehouseService = warehouseService;
            _mapper = mapper;
            _logger = logger;
            _cloudinaryService = cloudinaryService;
        }

        [HttpPost("CreateProductionOrder")]
        public async Task<IActionResult> CreateProductionOrder([FromBody] ProductionRequest po)
        {
            if (po == null)
            {
                return BadRequest(ApiResponse<ProductionOrder>.Fail("Dữ liệu request không được để trống", 400));
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<ProductionOrder>.Fail("Dữ liệu không hợp lệ", 400));
            }

            // Validation: Kiểm tra MaterialProductId
            if (po.MaterialProductId <= 0)
            {
                return BadRequest(ApiResponse<ProductionOrder>.Fail("ID sản phẩm nguyên liệu không hợp lệ", 400));
            }

            // Validation: Kiểm tra MaterialQuantity
            if (po.MaterialQuantity <= 0)
            {
                return BadRequest(ApiResponse<ProductionOrder>.Fail("Số lượng nguyên liệu phải lớn hơn 0", 400));
            }

            // Validation: Kiểm tra ListFinishProduct
            if (po.ListFinishProduct == null || !po.ListFinishProduct.Any())
            {
                return BadRequest(ApiResponse<ProductionOrder>.Fail("Danh sách thành phẩm không được để trống", 400));
            }

            // Validation: Kiểm tra sản phẩm nguyên liệu tồn tại
            var productMaterioalCheck = await _productService.GetByIdAsync(po.MaterialProductId);
            if (productMaterioalCheck == null)
            {
                return BadRequest(ApiResponse<ProductionOrder>.Fail("Sản phẩm nguyên liệu không tồn tại", 404));
            }

            // Validation: Kiểm tra danh sách thành phẩm
            var listFinishProductId = po.ListFinishProduct.Select(fp => fp.ProductId).ToList();
            var listFinishProduct = await _productService.GetByIds(listFinishProductId);
            foreach (var finishProduct in po.ListFinishProduct)
            {
                if (finishProduct.ProductId <= 0)
                {
                    return BadRequest(ApiResponse<ProductionOrder>.Fail("ID sản phẩm thành phẩm không hợp lệ", 400));
                }
                if (finishProduct.Quantity <= 0)
                {
                    return BadRequest(ApiResponse<ProductionOrder>.Fail($"Số lượng thành phẩm với ID {finishProduct.ProductId} phải lớn hơn 0", 400));
                }

                var productFinishCheck = listFinishProduct.FirstOrDefault(p => p.ProductId == finishProduct.ProductId);
                if (productFinishCheck == null)
                {
                    return BadRequest(ApiResponse<ProductionOrder>.Fail($"Sản phẩm hoàn thiện với ID {finishProduct.ProductId} không tồn tại", 404));
                }
            }

            try
            {
                // Tạo ProductionOrder
                var entityProductionOrderCreate = new ProductionOrderCreateVM
                {
                    Note = po.Note
                };
                var entityProductionOrder = _mapper.Map<ProductionOrderCreateVM, ProductionOrder>(entityProductionOrderCreate);
                entityProductionOrder.Status = (int)ProductionOrderStatus.Pending;
                entityProductionOrder.CreatedAt = DateTime.Now;
                await _productionOrderService.CreateAsync(entityProductionOrder);

                // Sau khi CreateAsync, Id sẽ được set bởi EF Core
                if (entityProductionOrder.Id <= 0)
                {
                    _logger.LogError("ProductionOrder Id không được set sau khi tạo");
                    return StatusCode(500, ApiResponse<ProductionOrder>.Fail("Lỗi khi tạo đơn sản xuất: Không thể tạo đơn sản xuất", 500));
                }

                // Tạo Finishproducts
                foreach (var finishProduct in po.ListFinishProduct)
                {
                    var product = listFinishProduct.FirstOrDefault(p => p.ProductId == finishProduct.ProductId);
                    var entityFinishProductProductionCreate = new FinishproductCreateVM
                    {
                        ProductionId = entityProductionOrder.Id,
                        ProductId = finishProduct.ProductId,
                        Quantity = finishProduct.Quantity,
                        WarehouseId = 1, // Mặc định kho thành phẩm là 1
                        TotalWeight = (product.WeightPerUnit * finishProduct.Quantity) ?? 0
                    };
                    var entityFinishProductProduction = _mapper.Map<FinishproductCreateVM, Finishproduct>(entityFinishProductProductionCreate);
                    entityFinishProductProduction.CreatedAt = DateTime.Now;
                    await _finishproductService.CreateAsync(entityFinishProductProduction);
                }

                // Tạo Material
                var entityMaterialUsage = new MaterialCreateVM
                {
                    ProductionId = entityProductionOrder.Id,
                    ProductId = po.MaterialProductId,
                    Quantity = po.MaterialQuantity,
                    WarehouseId = 2, // Mặc định kho nguyên liệu là 2
                    TotalWeight = (productMaterioalCheck.WeightPerUnit * po.MaterialQuantity) ?? 0
                };
                var entityMaterial = _mapper.Map<MaterialCreateVM, Material>(entityMaterialUsage);
                entityMaterial.CreatedAt = DateTime.Now;
                entityMaterial.LastUpdated = DateTime.Now;
                await _materialService.CreateAsync(entityMaterial);

                return Ok(ApiResponse<string>.Ok("Tạo đơn sản xuất thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo đơn sản xuất. Chi tiết: {Message}", ex.Message);
                return StatusCode(500, ApiResponse<ProductionOrder>.Fail($"Có lỗi xảy ra khi tạo đơn sản xuất: {ex.Message}", 500));
            }
        }

        /// <summary>
        /// Chuyển đơn sản xuất sang trạng thái Processing
        /// Trừ số lượng nguyên liệu từ stockBatch và inventory ở kho nguyên liệu (warehouseId = 2)
        /// </summary>
        [HttpPut("ChangeToProcessing/{productionOrderId}")]
        public async Task<IActionResult> ChangeToProcessing(int productionOrderId)
        {
            if (productionOrderId <= 0)
            {
                return BadRequest(ApiResponse<string>.Fail("Id đơn sản xuất không hợp lệ", 400));
            }

            try
            {
                // Lấy đơn sản xuất
                var productionOrder = await _productionOrderService.GetByIdAsync(productionOrderId);
                if (productionOrder == null)
                {
                    return NotFound(ApiResponse<string>.Fail("Không tìm thấy đơn sản xuất", 404));
                }

                // Kiểm tra trạng thái hiện tại phải là Pending
                if (productionOrder.Status != (int)ProductionOrderStatus.Pending)
                {
                    return BadRequest(ApiResponse<string>.Fail("Chỉ có thể chuyển đơn từ trạng thái Pending sang Processing", 400));
                }

                // Lấy danh sách nguyên liệu của đơn sản xuất
                var materials = await _materialService.GetQueryable()
                    .Where(m => m.ProductionId == productionOrderId)
                    .ToListAsync();

                if (!materials.Any())
                {
                    return BadRequest(ApiResponse<string>.Fail("Đơn sản xuất không có nguyên liệu", 400));
                }

                const int rawMaterialWarehouseId = 2; // Kho nguyên liệu mặc định
                var inventoryUpdates = new Dictionary<int, Inventory>();
                var stockBatchUpdates = new Dictionary<int, StockBatch>();

                // Xử lý từng nguyên liệu
                foreach (var material in materials)
                {
                    var productId = material.ProductId;
                    var quantity = material.Quantity;

                    // Kiểm tra tồn kho
                    var inventoryDto = await _inventoryService.GetByWarehouseAndProductId(rawMaterialWarehouseId, productId);
                    if (inventoryDto == null)
                    {
                        var product = await _productService.GetByIdAsync(productId);
                        return BadRequest(ApiResponse<string>.Fail(
                            $"Không tìm thấy sản phẩm '{product?.ProductName ?? productId.ToString()}' trong kho nguyên liệu.", 404));
                    }

                    if ((inventoryDto.Quantity ?? 0) < quantity)
                    {
                        var product = await _productService.GetByIdAsync(productId);
                        return BadRequest(ApiResponse<string>.Fail(
                            $"Sản phẩm '{product?.ProductName ?? productId.ToString()}' trong kho nguyên liệu chỉ còn {inventoryDto.Quantity}, không đủ {quantity} yêu cầu.", 400));
                    }

                    // Lấy StockBatch theo FIFO từ kho nguyên liệu
                    var listStockBatch = await _stockBatchService.GetByProductIdForOrder(new List<int> { productId });
                    if (listStockBatch == null || !listStockBatch.Any())
                    {
                        return BadRequest(ApiResponse<string>.Fail($"Không tìm thấy lô hàng khả dụng cho sản phẩm {productId} trong kho nguyên liệu.", 404));
                    }

                    var batches = listStockBatch
                        .Where(sb => sb.ProductId == productId
                            && sb.WarehouseId == rawMaterialWarehouseId
                            && ((sb.QuantityIn ?? 0) > (sb.QuantityOut ?? 0))
                            && (sb.ExpireDate == null || sb.ExpireDate > DateTime.Today))
                        .OrderBy(sb => sb.ImportDate) // FIFO
                        .ToList();

                    decimal remaining = quantity;
                    foreach (var batch in batches)
                    {
                        if (remaining <= 0) break;
                        decimal available = ((batch.QuantityIn ?? 0) - (batch.QuantityOut ?? 0));
                        if (available <= 0) continue;

                        decimal take = Math.Min(available, remaining);

                        // Cập nhật StockBatch
                        if (stockBatchUpdates.ContainsKey(batch.BatchId))
                        {
                            stockBatchUpdates[batch.BatchId].QuantityOut += take;
                        }
                        else
                        {
                            var batchEntity = await _stockBatchService.GetByIdAsync(batch.BatchId);
                            if (batchEntity != null)
                            {
                                batchEntity.QuantityOut += take;
                                batchEntity.LastUpdated = DateTime.Now;
                                stockBatchUpdates[batch.BatchId] = batchEntity;
                            }
                        }
                        remaining -= take;
                    }

                    if (remaining > 0)
                    {
                        var product = await _productService.GetByIdAsync(productId);
                        return BadRequest(ApiResponse<string>.Fail(
                            $"Không đủ hàng trong các lô cho sản phẩm '{product?.ProductName ?? productId.ToString()}'", 400));
                    }

                    // Cập nhật Inventory
                    var inventoryEntity = await _inventoryService.GetEntityByWarehouseAndProductIdAsync(rawMaterialWarehouseId, productId);
                    if (inventoryEntity != null)
                    {
                        inventoryEntity.Quantity -= quantity;
                        inventoryEntity.LastUpdated = DateTime.Now;
                        inventoryUpdates[productId] = inventoryEntity;
                    }
                }

                // Thực hiện update tất cả Inventory
                foreach (var inventory in inventoryUpdates.Values)
                {
                    await _inventoryService.UpdateNoTracking(inventory);
                }

                // Thực hiện update tất cả StockBatch
                foreach (var stockBatch in stockBatchUpdates.Values)
                {
                    await _stockBatchService.UpdateNoTracking(stockBatch);
                }

                // Cập nhật trạng thái đơn sản xuất
                productionOrder.Status = (int)ProductionOrderStatus.Processing;
                productionOrder.StartDate = DateTime.Now;
                await _productionOrderService.UpdateAsync(productionOrder);

                return Ok(ApiResponse<string>.Ok("Đơn sản xuất đang được xử lý"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi chuyển đơn sản xuất sang Processing");
                return BadRequest(ApiResponse<string>.Fail("Có lỗi xảy ra khi chuyển đơn sản xuất sang Processing"));
            }
        }

        /// <summary>
        /// Chuyển đơn sản xuất sang trạng thái Finished
        /// Cộng số lượng thành phẩm vào inventory và tạo stockBatch ở kho tổng (warehouseId = 1)
        /// </summary>
        [HttpPut("ChangeToFinished/{productionOrderId}")]
        public async Task<IActionResult> ChangeToFinished(int productionOrderId, [FromBody] FinishProductionRequest? request = null)
        {
            if (productionOrderId <= 0)
            {
                return BadRequest(ApiResponse<string>.Fail("Id đơn sản xuất không hợp lệ", 400));
            }

            try
            {
                // Lấy đơn sản xuất
                var productionOrder = await _productionOrderService.GetByIdAsync(productionOrderId);
                if (productionOrder == null)
                {
                    return NotFound(ApiResponse<string>.Fail("Không tìm thấy đơn sản xuất", 404));
                }

                // Kiểm tra trạng thái hiện tại phải là Processing
                if (productionOrder.Status != (int)ProductionOrderStatus.Processing)
                {
                    return BadRequest(ApiResponse<string>.Fail("Chỉ có thể chuyển đơn từ trạng thái Processing sang Finished", 400));
                }

                // Lấy danh sách thành phẩm của đơn sản xuất
                var finishProducts = await _finishproductService.GetQueryable()
                    .Where(f => f.ProductionId == productionOrderId)
                    .ToListAsync();

                if (!finishProducts.Any())
                {
                    return BadRequest(ApiResponse<string>.Fail("Đơn sản xuất không có thành phẩm", 400));
                }

                const int finishedProductWarehouseId = 1; // Kho tổng mặc định
                var inventoryUpdates = new Dictionary<int, Inventory>();
                string batchCodePrefix = "BATCH-PROD";
                int batchCounter = 1;

                // Xử lý từng thành phẩm
                foreach (var finishProduct in finishProducts)
                {
                    var productId = finishProduct.ProductId;
                    // Lấy số lượng từ request nếu có, nếu không thì dùng số lượng mặc định
                    int quantity = finishProduct.Quantity;
                    bool quantityUpdated = false;
                    if (request?.FinishProductQuantities != null)
                    {
                        var customQuantity = request.FinishProductQuantities
                            .FirstOrDefault(fpq => fpq.ProductId == finishProduct.ProductId);
                        if (customQuantity != null && customQuantity.Quantity.HasValue)
                        {
                            quantity = customQuantity.Quantity.Value;
                            quantityUpdated = true;
                        }
                    }

                    if (quantity <= 0)
                    {
                        continue; // Bỏ qua nếu số lượng <= 0
                    }

                    // Cập nhật số lượng thành phẩm trong bảng Finishproduct nếu có thay đổi
                    if (quantityUpdated)
                    {
                        var product = await _productService.GetByIdAsync(finishProduct.ProductId);
                        finishProduct.Quantity = quantity;
                        finishProduct.TotalWeight = (product.WeightPerUnit * quantity) ?? 0;
                        await _finishproductService.UpdateAsync(finishProduct);
                    }

                    // Tạo BatchCode
                    string uniqueBatchCode = $"{batchCodePrefix}{batchCounter:D4}";
                    while (await _stockBatchService.GetByName(uniqueBatchCode) != null)
                    {
                        batchCounter++;
                        uniqueBatchCode = $"{batchCodePrefix}{batchCounter:D4}";
                    }
                    batchCounter++;

                    // Tạo StockBatch sử dụng ViewModel và Mapper
                    var stockBatchCreateVM = new StockBatchProductionCreateVM
                    {
                        WarehouseId = finishedProductWarehouseId,
                        ProductId = productId,
                        ProductionFinishId = productionOrder.Id,
                        BatchCode = uniqueBatchCode,
                        ImportDate = DateTime.UtcNow,
                        QuantityIn = quantity,
                        Status = 1, // Đã nhập kho
                        IsActive = true,
                        LastUpdated = DateTime.UtcNow,
                        Note = $"Sản xuất từ đơn sản xuất #{productionOrderId}"
                    };
                    var stockBatchEntity = _mapper.Map<StockBatchProductionCreateVM, StockBatch>(stockBatchCreateVM);
                    await _stockBatchService.CreateAsync(stockBatchEntity);

                    // Cập nhật hoặc tạo Inventory
                    var inventoryEntity = await _inventoryService.GetEntityByWarehouseAndProductIdAsync(finishedProductWarehouseId, productId);
                    if (inventoryEntity != null)
                    {
                        inventoryEntity.Quantity += quantity;
                        inventoryEntity.LastUpdated = DateTime.Now;
                        inventoryUpdates[productId] = inventoryEntity;
                    }
                    else
                    {
                        // Tạo mới Inventory nếu chưa có
                        var newInventory = new Inventory
                        {
                            WarehouseId = finishedProductWarehouseId,
                            ProductId = productId,
                            Quantity = quantity,
                            LastUpdated = DateTime.Now
                        };
                        await _inventoryService.CreateAsync(newInventory);
                    }
                }

                // Thực hiện update tất cả Inventory
                foreach (var inventory in inventoryUpdates.Values)
                {
                    await _inventoryService.UpdateNoTracking(inventory);
                }

                // Cập nhật trạng thái đơn sản xuất
                productionOrder.Status = (int)ProductionOrderStatus.Finished;
                productionOrder.EndDate = DateTime.Now;
                await _productionOrderService.UpdateAsync(productionOrder);

                return Ok(ApiResponse<string>.Ok("Đơn sản xuất đã hoàn thành"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi chuyển đơn sản xuất sang Finished");
                return BadRequest(ApiResponse<string>.Fail("Có lỗi xảy ra khi chuyển đơn sản xuất sang Finished"));
            }
        }

        /// <summary>
        /// Chuyển đơn sản xuất sang trạng thái Cancel
        /// Chỉ có thể hủy đơn khi đơn đang ở trạng thái Pending (Đang chờ xử lý)
        /// </summary>
        [HttpPut("ChangeToCancel/{productionOrderId}")]
        public async Task<IActionResult> ChangeToCancel(int productionOrderId)
        {
            if (productionOrderId <= 0)
            {
                return BadRequest(ApiResponse<string>.Fail("Id đơn sản xuất không hợp lệ", 400));
            }

            try
            {
                // Lấy đơn sản xuất
                var productionOrder = await _productionOrderService.GetByIdAsync(productionOrderId);
                if (productionOrder == null)
                {
                    return NotFound(ApiResponse<string>.Fail("Không tìm thấy đơn sản xuất", 404));
                }

                // Kiểm tra trạng thái hiện tại phải là Pending
                if (productionOrder.Status != (int)ProductionOrderStatus.Pending)
                {
                    return BadRequest(ApiResponse<string>.Fail("Chỉ có thể hủy đơn sản xuất khi đơn đang ở trạng thái Đang chờ xử lý (Pending)", 400));
                }

                // Cập nhật trạng thái đơn sản xuất sang Cancel
                productionOrder.Status = (int)ProductionOrderStatus.Cancel;
                await _productionOrderService.UpdateAsync(productionOrder);

                return Ok(ApiResponse<string>.Ok("Đơn sản xuất đã được hủy"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi hủy đơn sản xuất");
                return BadRequest(ApiResponse<string>.Fail("Có lỗi xảy ra khi hủy đơn sản xuất"));
            }
        }

        /// <summary>
        /// Lấy danh sách các đơn sản xuất với phân trang và tìm kiếm
        /// </summary>
        /// <param name="search">Điều kiện tìm kiếm và phân trang</param>
        /// <returns>Danh sách đơn sản xuất thỏa mãn điều kiện</returns>
        [HttpPost("GetData")]
        public async Task<IActionResult> GetData([FromBody] ProductionOrderSearch search)
        {
            try
            {
                var result = await _productionOrderService.GetData(search);
                if (result.Items == null || !result.Items.Any())
                {
                    return Ok(ApiResponse<PagedList<ProductionOrderDto>>.Ok(result));
                }

                // Enrich thông tin: thêm StatusName
                foreach (var po in result.Items)
                {
                    if (po.Status.HasValue)
                    {
                        ProductionOrderStatus status = (ProductionOrderStatus)po.Status.Value;
                        po.StatusName = status.GetDescription();
                    }
                }

                return Ok(ApiResponse<PagedList<ProductionOrderDto>>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách đơn sản xuất");
                return BadRequest(ApiResponse<PagedList<ProductionOrderDto>>.Fail("Có lỗi xảy ra khi lấy dữ liệu"));
            }
        }

        /// <summary>
        /// Lấy chi tiết đơn sản xuất theo ID
        /// </summary>
        /// <param name="Id">ProductionOrderId</param>
        /// <returns>Chi tiết đơn sản xuất bao gồm thành phẩm và nguyên liệu</returns>
        [HttpGet("GetDetail/{Id}")]
        public async Task<IActionResult> GetDetail(int Id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ", 400));
            }

            try
            {
                var productionOrder = new FullProductionOrderVM();
                if (Id > 0)
                {
                    var detail = await _productionOrderService.GetByIdAsync(Id);
                    if (detail != null)
                    {
                        productionOrder.Id = detail.Id;
                        productionOrder.StartDate = detail.StartDate;
                        productionOrder.EndDate = detail.EndDate;
                        productionOrder.Status = detail.Status;
                        productionOrder.Note = detail.Note;
                        productionOrder.CreatedAt = detail.CreatedAt;

                        // Gắn StatusName
                        if (detail.Status.HasValue)
                        {
                            ProductionOrderStatus status = (ProductionOrderStatus)detail.Status.Value;
                            productionOrder.StatusName = status.GetDescription();
                        }
                    }
                    else
                    {
                        return NotFound(ApiResponse<FullProductionOrderVM>.Fail("Không tìm thấy đơn sản xuất.", 404));
                    }
                }
                else if (Id <= 0)
                {
                    return BadRequest(ApiResponse<FullProductionOrderVM>.Fail("Id không hợp lệ", 400));
                }

                // Lấy danh sách thành phẩm
                var finishProducts = await _finishproductService.GetQueryable()
                    .Where(f => f.ProductionId == Id)
                    .ToListAsync();

                // Lấy danh sách nguyên liệu
                var materials = await _materialService.GetQueryable()
                    .Where(m => m.ProductionId == Id)
                    .ToListAsync();

                // Lấy danh sách ProductId và WarehouseId để query một lần
                var productIds = finishProducts.Select(f => f.ProductId)
                    .Union(materials.Select(m => m.ProductId))
                    .Distinct()
                    .ToList();

                var warehouseIds = finishProducts.Select(f => f.WarehouseId)
                    .Union(materials.Select(m => m.WarehouseId))
                    .Distinct()
                    .ToList();

                // Lấy thông tin sản phẩm
                var products = await _productService.GetByIds(productIds);
                var productsDict = products.Where(p => p != null).ToDictionary(p => p!.ProductId, p => p!);

                // Lấy thông tin kho
                var warehouses = await _warehouseService.GetByListWarehouseId(warehouseIds);
                var warehousesDict = warehouses.Where(w => w != null).ToDictionary(w => w!.WarehouseId, w => w!);

                // Map thành phẩm
                var finishProductDetails = finishProducts.Select(fp =>
                {
                    var product = productsDict.ContainsKey(fp.ProductId) ? productsDict[fp.ProductId] : null;
                    var warehouse = warehousesDict.ContainsKey(fp.WarehouseId) ? warehousesDict[fp.WarehouseId] : null;
                    return new FinishProductDetailDto
                    {
                        Id = fp.Id,
                        ProductId = fp.ProductId,
                        ProductName = product?.ProductName ?? "N/A",
                        ProductCode = product?.ProductCode ?? "N/A",
                        WarehouseId = fp.WarehouseId,
                        WarehouseName = warehouse?.WarehouseName ?? "N/A",
                        Quantity = fp.Quantity,
                        WeightPerUnit = product?.WeightPerUnit ?? 0,
                        CreatedAt = fp.CreatedAt
                    };
                }).ToList();

                // Map nguyên liệu
                var materialDetails = materials.Select(m =>
                {
                    var product = productsDict.ContainsKey(m.ProductId) ? productsDict[m.ProductId] : null;
                    var warehouse = warehousesDict.ContainsKey(m.WarehouseId) ? warehousesDict[m.WarehouseId] : null;
                    return new MaterialDetailDto
                    {
                        Id = m.Id,
                        ProductId = m.ProductId,
                        ProductName = product?.ProductName ?? "N/A",
                        ProductCode = product?.ProductCode ?? "N/A",
                        WarehouseId = m.WarehouseId,
                        WarehouseName = warehouse?.WarehouseName ?? "N/A",
                        Quantity = m.Quantity,
                        WeightPerUnit = product?.WeightPerUnit ?? 0,
                        CreatedAt = m.CreatedAt,
                        LastUpdated = m.LastUpdated
                    };
                }).ToList();

                productionOrder.FinishProducts = finishProductDetails;
                productionOrder.Materials = materialDetails;

                return Ok(ApiResponse<FullProductionOrderVM>.Ok(productionOrder));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy dữ liệu đơn sản xuất");
                return BadRequest(ApiResponse<FullProductionOrderVM>.Fail("Có lỗi xảy ra khi lấy dữ liệu"));
            }
        }

    }
}
