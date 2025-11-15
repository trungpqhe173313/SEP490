using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NB.Model.Entities;
using NB.Model.Enums;
using NB.Service.Common;
using NB.Service.Core.Enum;
using NB.Service.Core.Mapper;
using NB.Service.Dto;
using NB.Service.InventoryService;
using NB.Service.InventoryService.Dto;
using NB.Service.ProductService;
using NB.Service.ProductService.Dto;
using NB.Service.StockBatchService;
using NB.Service.StockBatchService.Dto;
using NB.Service.SupplierService.Dto;
using NB.Service.SupplierService.ViewModels;
using NB.Service.TransactionDetailService;
using NB.Service.TransactionDetailService.Dto;
using NB.Service.TransactionDetailService.ViewModels;
using NB.Service.TransactionService;
using NB.Service.TransactionService.Dto;
using NB.Service.TransactionService.ViewModels;
using NB.Service.UserService;
using NB.Service.UserService.Dto;
using NB.Service.UserService.ViewModels;
using NB.Service.WarehouseService;
using NB.Service.WarehouseService.Dto;

namespace NB.API.Controllers
{
    [Route("api/stockoutput")]
    public class StockOutputController : Controller
    {
        private readonly ITransactionService _transactionService;
        private readonly ITransactionDetailService _transactionDetailService;
        private readonly IProductService _productService;
        private readonly IUserService _userService;
        private readonly IStockBatchService _stockBatchService;
        private readonly IWarehouseService _warehouseService;
        private readonly IInventoryService _inventoryService;
        private readonly ILogger<EmployeeController> _logger;
        private readonly IMapper _mapper;
        private readonly string transactionType = "Export";
        public StockOutputController(
            ITransactionService transactionService,
            ITransactionDetailService transactionDetailService,
            IProductService productService,
            IStockBatchService stockBatchService,
            IUserService userService,
            IWarehouseService warehouseService,
            IInventoryService inventoryService,
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
        /// Duc Anh
        /// Lấy ra tất cả các các đơn hàng
        /// </summary>
        /// <param name="search"> tìm các đơn hàng theo một số đơn hàng</param>
        /// <returns>các đơn hàng thỏa mãn các điều kiện của search nếu có</returns>
        [HttpPost("GetData")]
        public async Task<IActionResult> GetData([FromBody] TransactionSearch search)
        {
            try
            {
                search.Type = transactionType;
                var result = await _transactionService.GetDataForExport(search);
                if (result.Items == null || !result.Items.Any())
                {
                    return NotFound(ApiResponse<PagedList<TransactionDto>>.Fail("Không tìm đơn hàng"));
                }
                var listWarehouseId = result.Items.Select(t => t.WarehouseId).ToList();
                var listWareHouse = await _warehouseService.GetByListWarehouseId(listWarehouseId);
                //lấy tất cả các khách hàng
                var listUser = _userService.GetAll();
                if (listWareHouse == null || !listWareHouse.Any())
                {
                    return NotFound(ApiResponse<PagedList<WarehouseDto>>.Fail("Không tìm thấy kho"));
                }
                foreach (var t in result.Items)
                {
                    //gắn tên khách hàng
                    var user = listUser.FirstOrDefault(u => u.UserId == t.CustomerId);
                    if (user != null)
                    {
                        t.FullName = user.FullName;
                    }

                    //lay tên kho
                    var warehouse = listWareHouse?.FirstOrDefault(w => w != null && w.WarehouseId == t.WarehouseId);
                    if (warehouse != null)
                    {
                        t.WarehouseName = warehouse.WarehouseName;
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
                _logger.LogError(ex, "Lỗi khi lấy dữ liệu đơn hàng");
                return BadRequest(ApiResponse<PagedList<TransactionDto>>.Fail("Có lỗi xảy ra khi lấy dữ liệu"));
            }
        }

        /// <summary>
        /// Duc Anh
        /// Hàm để lấy ra chi tiết của đơn hàng
        /// </summary>
        /// <param name="Id">TransactionId</param>
        /// <returns>Trả về chi tiết đơn hàng bao gồm các sản phẩm có trong đơn hàng</returns>
        [HttpGet("GetDetail/{Id}")]
        public async Task<IActionResult> GetDetail(int Id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ", 400));
            }

            try
            {
                var transaction = new FullTransactionExportVM();
                if (Id > 0)
                {
                    var detail = await _transactionService.GetByTransactionId(Id);
                    if (detail != null)
                    {
                        transaction.Status = detail.Status;
                        transaction.TransactionId = detail.TransactionId;
                        transaction.TransactionDate = detail.TransactionDate ?? DateTime.MinValue;
                        transaction.WarehouseName = (await _warehouseService.GetById(detail.WarehouseId))?.WarehouseName ?? "N/A";
                        transaction.TotalCost = detail.TotalCost ?? 0;
                        int id = detail.SupplierId ?? 0;
                        var customer = await _userService.GetByIdAsync(detail.CustomerId);
                        if (customer != null)
                        {

                            var customerResult = new CustomerOutputVM
                            {
                                UserId = customer.UserId,
                                FullName = customer.FullName,
                                Phone = customer.Phone,
                                Email = customer.Email,
                                Image = customer.Image
                            };
                            transaction.Customer = customerResult;
                        }
                        else
                        {
                            var customerResult = new CustomerOutputVM
                            {
                                UserId = null,
                                FullName = "N/A",
                                Phone = "N/A",
                                Email = "N/A",
                                Image = "N/A"
                            };
                            transaction.Customer = customerResult;
                        }
                    }
                    else
                    {
                        return NotFound(ApiResponse<FullTransactionVM>.Fail("Không tìm thấy đơn hàng.", 404));
                    }
                }
                else if (Id <= 0)
                {
                    return BadRequest(ApiResponse<FullTransactionVM>.Fail("Id không hợp lệ", 400));
                }

                var productDetails = await _transactionDetailService.GetByTransactionId(Id);
                if (productDetails == null || !productDetails.Any())
                {
                    return NotFound(ApiResponse<FullTransactionVM>.Fail("Không có thông tin cho giao dịch này.", 400));
                }
                foreach (var item in productDetails)
                {
                    var product = await _productService.GetById(item.ProductId);
                    item.ProductName = product != null ? product.ProductName : "N/A";

                }
                //var batches = await _stockBatchService.GetByTransactionId(Id);
                //var batch = batches.FirstOrDefault();

                var listResult = productDetails.Select(item => new TransactionDetailOutputVM
                {
                    TransactionDetailId = item.Id,
                    ProductId = item.ProductId,
                    ProductName = item.ProductName ?? "N/A",
                    UnitPrice = item.UnitPrice,
                    WeightPerUnit = item.WeightPerUnit,
                    Quantity = item.Quantity
                }).ToList<TransactionDetailOutputVM?>();

                transaction.list = listResult ?? new List<TransactionDetailOutputVM?>();
                return Ok(ApiResponse<FullTransactionExportVM>.Ok(transaction));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy dữ liệu đơn hàng");
                return BadRequest(ApiResponse<PagedList<SupplierDto>>.Fail("Có lỗi xảy ra khi lấy dữ liệu"));
            }
        }

        [HttpGet("GetByTransactionId")]
        public async Task<IActionResult> GetByTransactionId(int id)
        {
            try
            {
                var result = await _transactionService.GetByTransactionId(id);
                if (result == null)
                {
                    return NotFound(ApiResponse<UserDto>.Fail("Không tìm thấy đơn hàng", 404));
                }

                var warehouse = await _warehouseService.GetByIdAsync(result.WarehouseId);
                if (warehouse == null)
                {
                    return NotFound(ApiResponse<PagedList<WarehouseDto>>.Fail("Không tìm thấy kho"));
                }
                //gắn tên warehouse
                result.WarehouseName = warehouse?.WarehouseName ?? "N/A";

                //gắn status cho transaction
                if (result.Status.HasValue)
                {
                    TransactionStatus status = (TransactionStatus)result.Status.Value;
                    result.StatusName = status.ToString();
                }

                return Ok(ApiResponse<TransactionDto>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy đơn hàng với Id: {Id}", id);
                return BadRequest(ApiResponse<TransactionDto>.Fail("Có lỗi xảy ra"));
            }
        }

        [HttpGet("GetTransactionDetailByTransactionId")]
        public async Task<IActionResult> GetTransactionDetailByTransactionId(int id)
        {
            try
            {
                //lay don hàng
                var result = await _transactionDetailService.GetByTransactionId(id);
                if (result == null)
                {
                    return NotFound(ApiResponse<UserDto>.Fail("Không tìm thấy đơn hàng", 404));
                }
                //lay danh sach product id
                List<int> listProductId = result.Select(td => td.ProductId).ToList();
                //lay ra các sản phẩm trong listProductId
                var listProduct = await _productService.GetByIds(listProductId);
                foreach (var t in result)
                {
                    var product = listProduct.FirstOrDefault(p => p.ProductId == t.ProductId);
                    if (product is not null)
                    {
                        t.ProductName = product.ProductName;
                        t.ImageUrl = product.ImageUrl;
                    }
                }

                return Ok(ApiResponse<List<TransactionDetailDto>>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy đơn hàng với Id: {Id}", id);
                return BadRequest(ApiResponse<TransactionDto>.Fail("Có lỗi xảy ra"));
            }
        }


        [HttpPost("CreateOrder/{userId}")]
        public async Task<IActionResult> CreateOrder(int userId, [FromBody] OrderRequest or)
        {
            var listProductOrder = or.ListProductOrder;
            var existingUser = await _userService.GetByUserId(userId);
            if (existingUser == null)
                return NotFound(ApiResponse<UserDto>.Fail("Không tìm thấy khách hàng", 404));

            if (listProductOrder == null || !listProductOrder.Any())
                return BadRequest(ApiResponse<ProductDto>.Fail("Không có sản phẩm nào", 404));

            var listProductId = listProductOrder.Select(p => p.ProductId).ToList();
            var listProduct = await _productService.GetByIds(listProductId);
            if (!listProduct.Any())
                return BadRequest(ApiResponse<ProductDto>.Fail("Không tìm thấy sản phẩm nào", 404));

            // Lấy tất cả inventory theo danh sách sản phẩm để kiểm tra xem lượng hàng hóa trong kho còn đủ không
            var listInventory = await _inventoryService.GetByProductIds(listProductOrder.Select(po => po.ProductId).ToList());

            foreach (var po in listProductOrder)
            {
                var orderQty = po.Quantity ?? 0;
                var inven = listInventory.FirstOrDefault(p => p.ProductId == po.ProductId);

                if (inven == null)
                {
                    return BadRequest(ApiResponse<InventoryDto>.Fail($"Không tìm thấy tồn kho cho sản phẩm {po.ProductId}", 404));
                }

                var invenQty = inven.Quantity ?? 0;

                if (orderQty > invenQty)
                {
                    var productCheck = await _productService.GetByIdAsync(po.ProductId);
                    var productName = productCheck?.ProductName ?? $"Sản phẩm {po.ProductId}";
                    return BadRequest(ApiResponse<InventoryDto>.Fail(
                        $"Sản phẩm '{productName}' chỉ còn {invenQty}, không đủ {orderQty} yêu cầu.",
                        400));
                }
            }

            try
            {
                // 1️ Lấy tất cả các lô hàng còn hàng và còn hạn
                var listStockBatch = await _stockBatchService.GetByProductIdForOrder(listProductId);
                
                if (listStockBatch == null || !listStockBatch.Any())
                {
                    return BadRequest(ApiResponse<string>.Fail("Không tìm thấy lô hàng khả dụng cho các sản phẩm này", 404));
                }

                // 2️ Tạo transaction (đơn hàng)
                var tranCreate = new TransactionCreateVM
                {
                    CustomerId = userId,
                    WarehouseId = listStockBatch.First().WarehouseId, // hoặc lấy theo input từ FE
                    Note = or.Note,
                    TotalCost = or.TotalCost
                };
                var transactionEntity = _mapper.Map<TransactionCreateVM, Transaction>(tranCreate);

                transactionEntity.Status = (int?)TransactionStatus.draft; // đang xử lý
                transactionEntity.TransactionDate = DateTime.Now;
                transactionEntity.Type = "Export";
                await _transactionService.CreateAsync(transactionEntity);

                // 3️ Duyệt từng sản phẩm để lấy lô & cập nhật tồn
                foreach (var po in listProductOrder)
                {
                    // 5️ Tạo transaction detail
                    var tranDetail = new TransactionDetailCreateVM
                    {
                        ProductId = po.ProductId,
                        TransactionId = transactionEntity.TransactionId,
                        Quantity = (int)(po.Quantity ?? 0),
                        UnitPrice = (decimal)(po.UnitPrice ?? 0),
                    };
                    var tranDetailEntity = _mapper.Map<TransactionDetailCreateVM, TransactionDetail>(tranDetail);
                    await _transactionDetailService.CreateAsync(tranDetailEntity);
                }

                // 6️ Trả về kết quả sau khi hoàn tất toàn bộ sản phẩm
                return Ok(ApiResponse<string>.Ok("Tạo đơn hàng thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo đơn hàng");
                return BadRequest(ApiResponse<string>.Fail("Có lỗi xảy ra khi tạo đơn hàng"));
            }
        }

        [HttpPost("UpdateOrderInDraftStatus/{transactionId}")]
        public async Task<IActionResult> UpdateOrderInDraftStatus(int transactionId, [FromBody] OrderRequest or)
        {
            var listProductOrder = or.ListProductOrder;
            var transaction = await _transactionService.GetByTransactionId(transactionId);
            if (transaction == null)
            {
                return NotFound(ApiResponse<TransactionDto>.Fail("Không tìm thấy đơn hàng", 404));
            }
            if (listProductOrder == null || !listProductOrder.Any())
            {
                return BadRequest(ApiResponse<ProductDto>.Fail("Không có sản phẩm nào", 404));
            }
            var listProductId = listProductOrder.Select(p => p.ProductId).ToList();
            var listProduct = await _productService.GetByIds(listProductId);
            if (!listProduct.Any())
                return BadRequest(ApiResponse<ProductDto>.Fail("Không tìm thấy sản phẩm nào", 404));
            // Lấy tất cả inventory theo danh sách sản phẩm để kiểm tra xem lượng hàng hóa trong kho còn đủ không
            var listInventory = await _inventoryService.GetByProductIds(listProductOrder.Select(po => po.ProductId).ToList());

            foreach (var po in listProductOrder)
            {
                var orderQty = po.Quantity ?? 0;
                var inven = listInventory.FirstOrDefault(p => p.ProductId == po.ProductId);

                if (inven == null)
                {
                    return BadRequest(ApiResponse<InventoryDto>.Fail($"Không tìm thấy tồn kho cho sản phẩm {po.ProductId}", 404));
                }

                var invenQty = inven.Quantity ?? 0;

                if (orderQty > invenQty)
                {
                    var productCheck = await _productService.GetByIdAsync(po.ProductId);
                    var productName = productCheck?.ProductName ?? $"Sản phẩm {po.ProductId}";
                    return BadRequest(ApiResponse<InventoryDto>.Fail(
                        $"Sản phẩm '{productName}' chỉ còn {invenQty}, không đủ {orderQty} yêu cầu.",
                        400));
                }
            }
            try
            {
                // Xóa chi tiết đơn hàng cũ
                var existingDetails = await _transactionDetailService.GetByTransactionId(transactionId);
                if (existingDetails == null || !existingDetails.Any())
                {
                    return BadRequest(ApiResponse<TransactionDetailDto>.Fail($"Không tìm thấy chi tiết đơn hàng", 404));
                    
                }
                await _transactionDetailService.DeleteRange(existingDetails);
                // Thêm chi tiết đơn hàng mới
                foreach (var po in listProductOrder)
                {
                    var tranDetail = new TransactionDetailCreateVM
                    {
                        ProductId = po.ProductId,
                        TransactionId = transaction.TransactionId,
                        Quantity = (int)(po.Quantity ?? 0),
                        UnitPrice = (decimal)(po.UnitPrice ?? 0),
                    };
                    var tranDetailEntity = _mapper.Map<TransactionDetailCreateVM, TransactionDetail>(tranDetail);
                    await _transactionDetailService.CreateAsync(tranDetailEntity);
                }
                // Cập nhật thông tin đơn hàng
                if (!string.IsNullOrEmpty(or.Note))
                {
                    transaction.Note = or.Note;
                }
                if (or.TotalCost.HasValue)
                {
                    transaction.TotalCost = or.TotalCost;
                }
                transaction.Status = (int)TransactionStatus.draft;
                await _transactionService.UpdateAsync(transaction);
                return Ok(ApiResponse<string>.Ok("Cập nhật đơn hàng thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật đơn hàng");
                return BadRequest(ApiResponse<string>.Fail("Có lỗi xảy ra khi cập nhật đơn hàng"));
            }
        }

        [HttpPost("UpdateToOrderStatus/{transactionId}")]
        public async Task<IActionResult> UpdateToOrderStatus(int transactionId)
        {
            var transaction = await _transactionService.GetByTransactionId(transactionId);
            if (transaction == null)
            {
                return NotFound(ApiResponse<TransactionDto>.Fail("Không tìm thấy đơn hàng", 404));
            }
            var listTransDetail = await _transactionDetailService.GetByTransactionId(transactionId);
            if (listTransDetail == null || !listTransDetail.Any())
            {
                return NotFound(ApiResponse<TransactionDto>.Fail("Không tìm thấy chi tiết đơn hàng", 404));
            }
            var listProductOrder = listTransDetail.Select(item => new
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity
            });

            var listProductId = listProductOrder.Select(p => p.ProductId).ToList();
            var listProduct = await _productService.GetByIds(listProductId);
            if (!listProduct.Any())
                return BadRequest(ApiResponse<ProductDto>.Fail("Không tìm thấy sản phẩm nào", 404));

            // Lấy tất cả inventory theo danh sách sản phẩm để kiểm tra xem lượng hàng hóa trong kho còn đủ không
            var listInventory = await _inventoryService.GetByProductIds(listProductOrder.Select(po => po.ProductId).ToList());

            foreach (var po in listProductOrder)
            {
                var orderQty = po.Quantity;
                var inven = listInventory.FirstOrDefault(p => p.ProductId == po.ProductId);

                if (inven == null)
                {
                    return BadRequest(ApiResponse<InventoryDto>.Fail($"Không tìm thấy tồn kho cho sản phẩm {po.ProductId}", 404));
                }

                var invenQty = inven.Quantity ?? 0;

                if (orderQty > invenQty)
                {
                    var productCheck = await _productService.GetByIdAsync(po.ProductId);
                    var productName = productCheck?.ProductName ?? $"Sản phẩm {po.ProductId}";
                    return BadRequest(ApiResponse<InventoryDto>.Fail(
                        $"Sản phẩm '{productName}' chỉ còn {invenQty}, không đủ {orderQty} yêu cầu.",
                        400));
                }
            }

            try
            {
                // 1️ Lấy tất cả các lô hàng còn hàng và còn hạn
                var listStockBatch = await _stockBatchService.GetByProductIdForOrder(listProductId);

                // 3️ Duyệt từng sản phẩm để lấy lô & cập nhật tồn
                foreach (var po in listProductOrder)
                {
                    var batches = listStockBatch
                        .Where(sb => sb.ProductId == po.ProductId
                                     && sb.QuantityIn > sb.QuantityOut
                                     && sb.ExpireDate > DateTime.Today)
                        .OrderBy(sb => sb.ImportDate)
                        .ToList();

                    //lay ra so luong can lay
                    decimal remaining = po.Quantity;
                    var taken = new List<(StockBatchDto, decimal)>();

                    foreach (var batch in batches)
                    {
                        decimal available = (batch.QuantityIn - batch.QuantityOut) ?? 0;
                        if (available <= 0) continue;

                        decimal take = Math.Min(available, remaining);
                        taken.Add((batch, take));

                        // cập nhật lại lô hàng
                        var entity = await _stockBatchService.GetByIdAsync(batch.BatchId);
                        if (entity != null)
                        {
                            entity.QuantityOut += take;
                            entity.LastUpdated = DateTime.Now;
                            await _stockBatchService.UpdateAsync(entity);
                        }

                        remaining -= take;
                        if (remaining <= 0) break;
                    }

                    // 4️ Cập nhật inventory (tồn kho)
                    var inventoryEntity = await _inventoryService.GetEntityByProductIdAsync(po.ProductId);
                    if (inventoryEntity != null)
                    {
                        inventoryEntity.Quantity -= po.Quantity;
                        inventoryEntity.LastUpdated = DateTime.Now;
                        await _inventoryService.UpdateNoTracking(inventoryEntity);
                    }
                }

                //cap nhat trang thai cho don hang
                transaction.Status = (int)TransactionStatus.order;
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


        [HttpPut("UpdateTransactionInOrderStatus/{transactionId}")]
        public async Task<IActionResult> UpdateTransactionInOrderStatus(int transactionId, [FromBody] OrderRequest or)
        {
            // Bảo vệ trường hợp request không có sản phẩm
            if (or?.ListProductOrder == null || !or.ListProductOrder.Any())
            {
                return BadRequest(ApiResponse<string>.Fail("Đơn hàng mới không có sản phẩm nào để cập nhật.", 400));
            }

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
                    return NotFound(ApiResponse<string>.Fail("Không tìm thấy đơn hàng", 404));

                // --- 1️⃣ Lấy danh sách chi tiết cũ ---
                var oldDetails = (await _transactionDetailService.GetByTransactionId(transactionId))
                    ?? new List<TransactionDetailDto>();
                if (oldDetails == null || !oldDetails.Any())
                {
                    return NotFound(ApiResponse<string>.Fail("Không tìm thấy chi tiết đơn hàng", 404));
                }

                // Tạo dictionary để track các Inventory và StockBatch đã được update (tránh update lặp)
                var inventoryUpdates = new Dictionary<int, Inventory>(); // Key: ProductId
                var stockBatchUpdates = new Dictionary<int, StockBatch>(); // Key: BatchId

                // --- 2️⃣ Phân loại sản phẩm: giống nhau, mới, cũ ---
                var oldProductDict = oldDetails
                    .GroupBy(d => d.ProductId)
                    .ToDictionary(g => g.Key, g => g.Sum(d => d.Quantity));
                var newProductDict = listProductOrder
                    .ToDictionary(p => p.ProductId, p => p.Quantity ?? 0);

                var commonProducts = oldProductDict.Keys.Intersect(newProductDict.Keys).ToList();
                var newProducts = newProductDict.Keys.Except(oldProductDict.Keys).ToList();
                var removedProducts = oldProductDict.Keys.Except(newProductDict.Keys).ToList();

                // --- 3️⃣ Xử lý sản phẩm bị xóa (chỉ có trong đơn cũ) - Trả lại hàng ---
                foreach (var productId in removedProducts)
                {
                    var oldQuantity = oldProductDict[productId];

                    // Trả lại Inventory
                    var inventoryEntity = await _inventoryService.GetEntityByProductIdAsync(productId);
                    if (inventoryEntity != null)
                    {
                        inventoryEntity.Quantity += oldQuantity;
                        inventoryEntity.LastUpdated = DateTime.Now;
                        inventoryUpdates[productId] = inventoryEntity;
                    }

                    // Trả lại StockBatch theo LIFO
                    var batchesToRevert = await _stockBatchService.GetByProductIdForOrder(new List<int> { productId });
                    if (batchesToRevert != null && batchesToRevert.Any())
                    {
                        var revertList = batchesToRevert
                            .Where(b => (b.QuantityOut ?? 0) > 0)
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
                                stockBatchUpdates[b.BatchId] = batchEntity;
                            }
                            toRevert -= takeBack;
                        }
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
                        // Kiểm tra đủ hàng không
                        var inventoryDto = await _inventoryService.GetByProductIdRetriveOneObject(productId);
                        if (inventoryDto == null)
                        {
                            var product = await _productService.GetByIdAsync(productId);
                            return BadRequest(ApiResponse<string>.Fail(
                                $"Không tìm thấy tồn kho cho sản phẩm '{product?.ProductName ?? productId.ToString()}'.", 404));
                        }

                        if ((inventoryDto.Quantity ?? 0) < diff)
                        {
                            var product = await _productService.GetByIdAsync(productId);
                            return BadRequest(ApiResponse<string>.Fail(
                                $"Sản phẩm '{product?.ProductName ?? productId.ToString()}' chỉ còn {inventoryDto.Quantity}, không đủ {diff} để tăng.", 400));
                        }

                        // Lấy Inventory entity để update
                        var inventoryEntity = await _inventoryService.GetEntityByProductIdAsync(productId);
                        if (inventoryEntity != null)
                        {
                            inventoryEntity.Quantity -= diff;
                            inventoryEntity.LastUpdated = DateTime.Now;
                            inventoryUpdates[productId] = inventoryEntity;
                        }

                        // Lấy thêm StockBatch
                        var listStockBatch = await _stockBatchService.GetByProductIdForOrder(new List<int> { productId });
                        if (listStockBatch == null || !listStockBatch.Any())
                        {
                            return BadRequest(ApiResponse<string>.Fail($"Không tìm thấy lô hàng khả dụng cho sản phẩm {productId}.", 404));
                        }
                        var batches = listStockBatch
                            .Where(sb => sb.ProductId == productId
                                && ((sb.QuantityIn ?? 0) > (sb.QuantityOut ?? 0))
                                && sb.ExpireDate > DateTime.Today)
                            .OrderBy(sb => sb.ImportDate)
                            .ToList();

                        decimal remaining = diff;
                        foreach (var batch in batches)
                        {
                            if (remaining <= 0) break;
                            decimal available = ((batch.QuantityIn ?? 0) - (batch.QuantityOut ?? 0));
                            if (available <= 0) continue;

                            decimal take = Math.Min(available, remaining);

                            // Nếu đã có trong dictionary thì cộng thêm, nếu chưa thì lấy từ DB và thêm vào
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
                            return BadRequest(ApiResponse<string>.Fail($"Không đủ hàng trong các lô cho sản phẩm {productId}", 400));
                        }
                    }
                    else
                    {
                        // Đơn mới ít hơn - Trả lại hàng (diff < 0 nên cần trả lại |diff|)
                        var returnQuantity = Math.Abs(diff);

                        // Trả lại Inventory
                        var inventoryEntity = await _inventoryService.GetEntityByProductIdAsync(productId);
                        if (inventoryEntity != null)
                        {
                            inventoryEntity.Quantity += returnQuantity;
                            inventoryEntity.LastUpdated = DateTime.Now;
                            inventoryUpdates[productId] = inventoryEntity;
                        }

                        // Trả lại StockBatch theo LIFO
                        var batchesToRevert = await _stockBatchService.GetByProductIdForOrder(new List<int> { productId });
                        if (batchesToRevert != null && batchesToRevert.Any())
                        {
                            var revertList = batchesToRevert
                                .Where(b => (b.QuantityOut ?? 0) > 0)
                                .OrderByDescending(b => b.ImportDate)
                                .ToList();

                            decimal toRevert = returnQuantity;
                            foreach (var b in revertList)
                            {
                                if (toRevert <= 0) break;
                                var availableOut = b.QuantityOut ?? 0;
                                if (availableOut <= 0) continue;

                                var takeBack = Math.Min(availableOut, toRevert);

                                // Nếu đã có trong dictionary thì trừ thêm, nếu chưa thì lấy từ DB và thêm vào
                                if (stockBatchUpdates.ContainsKey(b.BatchId))
                                {
                                    stockBatchUpdates[b.BatchId].QuantityOut -= takeBack;
                                    if (stockBatchUpdates[b.BatchId].QuantityOut < 0)
                                        stockBatchUpdates[b.BatchId].QuantityOut = 0;
                                }
                                else
                                {
                                    var batchEntity = await _stockBatchService.GetByIdAsync(b.BatchId);
                                    if (batchEntity != null)
                                    {
                                        batchEntity.QuantityOut -= takeBack;
                                        if (batchEntity.QuantityOut < 0) batchEntity.QuantityOut = 0;
                                        batchEntity.LastUpdated = DateTime.Now;
                                        stockBatchUpdates[b.BatchId] = batchEntity;
                                    }
                                }
                                toRevert -= takeBack;
                            }
                        }
                    }
                }

                // --- 5️⃣ Xử lý sản phẩm mới (chỉ có trong đơn mới) - Thêm mới như bình thường ---
                if (newProducts.Any())
                {
                    // Kiểm tra đủ hàng cho tất cả sản phẩm mới trước khi trừ tồn
                    var listInventory = await _inventoryService.GetByProductIds(newProducts) ?? new List<InventoryDto>();
                    foreach (var productId in newProducts)
                    {
                        var newQuantity = newProductDict[productId];
                        var inven = listInventory.FirstOrDefault(p => p.ProductId == productId);
                        if (inven == null)
                        {
                            return BadRequest(ApiResponse<string>.Fail($"Không tìm thấy tồn kho cho sản phẩm {productId}", 404));
                        }
                        if (inven.Quantity < newQuantity)
                        {
                            var product = await _productService.GetByIdAsync(productId);
                            return BadRequest(ApiResponse<string>.Fail(
                                $"Sản phẩm '{product?.ProductName}' chỉ còn {inven.Quantity}, không đủ {newQuantity} yêu cầu.", 400));
                        }
                    }

                    // Lấy StockBatch cho các sản phẩm mới
                    var listStockBatch = await _stockBatchService.GetByProductIdForOrder(newProducts) ?? new List<StockBatchDto>();
                    foreach (var po in listProductOrder.Where(p => newProducts.Contains(p.ProductId)))
                    {
                        var batches = listStockBatch
                            .Where(sb => sb.ProductId == po.ProductId
                                && ((sb.QuantityIn ?? 0) > (sb.QuantityOut ?? 0))
                                && sb.ExpireDate > DateTime.Today)
                            .OrderBy(sb => sb.ImportDate)
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
                                stockBatchUpdates[batch.BatchId] = batchEntity;
                            }
                            remaining -= take;
                        }

                        if (remaining > 0)
                        {
                            return BadRequest(ApiResponse<string>.Fail($"Không đủ hàng trong các lô cho sản phẩm {po.ProductId}", 400));
                        }

                        // Cập nhật Inventory
                        var inventoryEntity = await _inventoryService.GetEntityByProductIdAsync(po.ProductId);
                        if (inventoryEntity != null)
                        {
                            inventoryEntity.Quantity -= po.Quantity ?? 0;
                            inventoryEntity.LastUpdated = DateTime.Now;
                            inventoryUpdates[po.ProductId] = inventoryEntity;
                        }
                    }
                }

                // --- 6️⃣ Thực hiện update tất cả Inventory (mỗi cái chỉ 1 lần) ---
                foreach (var inventory in inventoryUpdates.Values)
                {
                    await _inventoryService.UpdateNoTracking(inventory);
                }

                // --- 7️⃣ Thực hiện update tất cả StockBatch (mỗi cái chỉ 1 lần) ---
                foreach (var stockBatch in stockBatchUpdates.Values)
                {
                    await _stockBatchService.UpdateNoTracking(stockBatch);
                }

                // --- 8️⃣ Xóa và tạo lại TransactionDetail ---
                await _transactionDetailService.DeleteRange(oldDetails); // Xóa dữ liệu cũ để tạo lại chính xác

                // Lấy lại inventory sau khi update để tính lại subtotal chính xác
                //var allInventoryDict = new Dictionary<int, Inventory>();
                //foreach (var productId in listProductOrder.Select(p => p.ProductId).Distinct())
                //{
                //    var inv = await _inventoryService.GetEntityByProductIdAsync(productId);
                //    if (inv != null)
                //    {
                //        allInventoryDict[productId] = inv;
                //    }
                //}

                foreach (var po in listProductOrder)
                {
                    // Ánh xạ lại transaction detail dựa trên số lượng mới
                    var tranDetail = new TransactionDetailCreateVM
                    {
                        ProductId = po.ProductId,
                        TransactionId = transactionId,
                        Quantity = (int)(po.Quantity ?? 0),
                        UnitPrice = (decimal)(po.UnitPrice ?? 0),
                    };
                    var tranDetailEntity = _mapper.Map<TransactionDetailCreateVM, TransactionDetail>(tranDetail);
                    await _transactionDetailService.CreateAsync(tranDetailEntity);
                }

                // --- 9️⃣ Cập nhật thông tin đơn hàng ---
                if (or.Status.HasValue)
                {
                    transaction.Status = or.Status;
                }
                else
                {
                    transaction.Status = (int?)TransactionStatus.order;
                }
                if (!string.IsNullOrEmpty(or.Note))
                {
                    transaction.Note = or.Note;
                }
                if (or.TotalCost.HasValue)
                {
                    transaction.TotalCost = or.TotalCost;
                }
                await _transactionService.UpdateAsync(transaction);

                return Ok(ApiResponse<string>.Ok("Cập nhật đơn hàng thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật đơn hàng");
                return BadRequest(ApiResponse<string>.Fail("Có lỗi xảy ra khi cập nhật đơn hàng"));
            }
        }

        [HttpPost("UpdateToDoneStatus/{transactionId}")]
        public async Task<IActionResult> UpdateToDoneStatus(int transactionId)
        {
            var transaction = await _transactionService.GetByTransactionId(transactionId);
            if (transaction == null)
            {
                return NotFound(ApiResponse<TransactionDto>.Fail("Không tìm thấy đơn hàng", 404));
            }
            try
            {
                //cap nhat trang thai cho don hang
                transaction.Status = (int)TransactionStatus.done;
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

        [HttpPost("UpdateToDeliveringStatus/{transactionId}")]
        public async Task<IActionResult> UpdateToDeliveringStatus(int transactionId)
        {
            var transaction = await _transactionService.GetByTransactionId(transactionId);
            if (transaction == null)
            {
                return NotFound(ApiResponse<TransactionDto>.Fail("Không tìm thấy đơn hàng", 404));
            }
            try
            {
                //cap nhat trang thai cho don hang
                transaction.Status = (int)TransactionStatus.delivering;
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




        [HttpGet("GetTransactionStatus")]
        public async Task<IActionResult> GetTransactionStatus()
        {
            try
            {
                var listStatus = new List<TransactionStatus>
        {
            TransactionStatus.draft,
            TransactionStatus.order,
            TransactionStatus.delivering
        };

                // Tạo danh sách trả về gồm int + string
                var result = listStatus
                    .Select(s => new KeyValuePair<int, string>((int)s, s.GetDescription()))
                    .ToList();

                return Ok(ApiResponse<List<KeyValuePair<int, string>>>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách trạng thái đơn hàng");
                return BadRequest(ApiResponse<string>.Fail("Có lỗi xảy ra"));
            }
        }

        /// <summary>
        /// API để trả hàng - Khách hàng có thể trả lại một số sản phẩm từ đơn hàng
        /// </summary>
        /// <param name="transactionId">ID của đơn hàng cần trả hàng</param>
        /// <param name="returnRequest">Danh sách sản phẩm và số lượng cần trả</param>
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
                // Lấy thông tin đơn hàng
                var transaction = await _transactionService.GetByIdAsync(transactionId);
                if (transaction == null)
                    return NotFound(ApiResponse<string>.Fail("Không tìm thấy đơn hàng", 404));

                // Lấy chi tiết đơn hàng hiện tại
                var currentDetails = await _transactionDetailService.GetByTransactionId(transactionId);
                if (currentDetails == null || !currentDetails.Any())
                {
                    return NotFound(ApiResponse<string>.Fail("Không tìm thấy chi tiết đơn hàng", 404));
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

                    // Kiểm tra sản phẩm có trong đơn hàng không
                    if (!currentProductDict.ContainsKey(productId))
                    {
                        var product = await _productService.GetByIdAsync(productId);
                        return BadRequest(ApiResponse<string>.Fail(
                            $"Sản phẩm '{product?.ProductName ?? productId.ToString()}' không có trong đơn hàng này.", 400));
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

                // Xử lý từng sản phẩm trả hàng
                foreach (var returnItem in returnProductDict)
                {
                    var productId = returnItem.Key;
                    var returnQuantity = returnItem.Value;

                    // Trả lại Inventory
                    var inventoryEntity = await _inventoryService.GetEntityByProductIdAsync(productId);
                    if (inventoryEntity != null)
                    {
                        inventoryEntity.Quantity += returnQuantity;
                        inventoryEntity.LastUpdated = DateTime.Now;
                        inventoryUpdates[productId] = inventoryEntity;
                    }

                    // Trả lại StockBatch theo LIFO (Last In First Out)
                    var batchesToRevert = await _stockBatchService.GetByProductIdForOrder(new List<int> { productId });
                    if (batchesToRevert != null && batchesToRevert.Any())
                    {
                        var revertList = batchesToRevert
                            .Where(b => (b.QuantityOut ?? 0) > 0)
                            .OrderByDescending(b => b.ImportDate)
                            .ToList();

                        decimal toRevert = returnQuantity;
                        foreach (var b in revertList)
                        {
                            if (toRevert <= 0) break;
                            var availableOut = b.QuantityOut ?? 0;
                            if (availableOut <= 0) continue;

                            var takeBack = Math.Min(availableOut, toRevert);

                            if (stockBatchUpdates.ContainsKey(b.BatchId))
                            {
                                stockBatchUpdates[b.BatchId].QuantityOut -= takeBack;
                                if (stockBatchUpdates[b.BatchId].QuantityOut < 0)
                                    stockBatchUpdates[b.BatchId].QuantityOut = 0;
                            }
                            else
                            {
                                var batchEntity = await _stockBatchService.GetByIdAsync(b.BatchId);
                                if (batchEntity != null)
                                {
                                    batchEntity.QuantityOut -= takeBack;
                                    if (batchEntity.QuantityOut < 0) batchEntity.QuantityOut = 0;
                                    batchEntity.LastUpdated = DateTime.Now;
                                    stockBatchUpdates[b.BatchId] = batchEntity;
                                }
                            }
                            toRevert -= takeBack;
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

                // Cập nhật TransactionDetail - Trừ số lượng hoặc xóa nếu trả hết
                var updatedDetails = new List<TransactionDetail>();
                //decimal totalCostReduction = 0;

                foreach (var detail in currentDetails)
                {
                    if (returnProductDict.ContainsKey(detail.ProductId))
                    {
                        var returnQuantity = returnProductDict[detail.ProductId];
                        var newQuantity = detail.Quantity - returnQuantity;

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

                        // Tính tổng tiền giảm
                        //totalCostReduction += returnQuantity * detail.UnitPrice;
                    }
                }

                // Cập nhật tổng tiền đơn hàng
                if (transaction.TotalCost.HasValue)
                {
                    transaction.TotalCost = or.TotalCost;
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

                return Ok(ApiResponse<string>.Ok($"Trả hàng thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý trả hàng");
                return BadRequest(ApiResponse<string>.Fail("Có lỗi xảy ra khi xử lý trả hàng"));
            }
        }

    }
}
