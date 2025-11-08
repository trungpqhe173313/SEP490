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
                    var warehouse = listWareHouse.FirstOrDefault(w => w.WarehouseId == t.WarehouseId);
                    if (warehouse != null)
                    {
                        t.WarehouseName = warehouse.WarehouseName;
                    }
                    //gắn statusName cho transaction
                    TransactionStatus status = (TransactionStatus)t.Status;
                    t.StatusName = status.GetDescription();
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
                    ProductName = item.ProductName,
                    UnitPrice = item.UnitPrice,
                    WeightPerUnit = item.WeightPerUnit,
                    Quantity = item.Quantity,
                    SubTotal = item.Subtotal
                    //,ExpireDate = batch.ExpireDate,
                    //Note = batch.Note
                }).ToList();

                transaction.list = listResult;
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
                result.WarehouseName = warehouse.WarehouseName;

                //gắn status cho transaction
                TransactionStatus status = (TransactionStatus)result.Status;
                result.StatusName = status.ToString();

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
                    return BadRequest(ApiResponse<InventoryDto>.Fail(
                        $"Sản phẩm '{productCheck.ProductName}' chỉ còn {invenQty}, không đủ {orderQty} yêu cầu.",
                        400));
                }
            }

            try
            {
                // 1️ Lấy tất cả các lô hàng còn hàng và còn hạn
                var listStockBatch = await _stockBatchService.GetByProductIdForOrder(listProductId);

                // 2️ Tạo transaction (đơn hàng)
                var tranCreate = new TransactionCreateVM
                {
                    CustomerId = userId,
                    WarehouseId = listStockBatch.First().WarehouseId, // hoặc lấy theo input từ FE
                    Note = or.Note
                };
                var transactionEntity = _mapper.Map<TransactionCreateVM, Transaction>(tranCreate);

                transactionEntity.Status = (int?)TransactionStatus.draft; // đang xử lý
                transactionEntity.TransactionDate = DateTime.Now;
                transactionEntity.Type = "Export";
                await _transactionService.CreateAsync(transactionEntity);

                // 3️ Duyệt từng sản phẩm để lấy lô & cập nhật tồn
                foreach (var po in listProductOrder)
                {
                    //var batches = listStockBatch
                    //    .Where(sb => sb.ProductId == po.ProductId
                    //                 && sb.QuantityIn > sb.QuantityOut
                    //                 && sb.ExpireDate > DateTime.Today)
                    //    .OrderBy(sb => sb.ImportDate)
                    //    .ToList();

                    //decimal remaining = po.Quantity ?? 0;
                    //var taken = new List<(StockBatchDto, decimal)>();

                    //foreach (var batch in batches)
                    //{
                    //    decimal available = (batch.QuantityIn - batch.QuantityOut) ?? 0;
                    //    if (available <= 0) continue;

                    //    decimal take = Math.Min(available, remaining);
                    //    taken.Add((batch, take));

                    //    // cập nhật lại lô hàng
                    //    var entity = await _stockBatchService.GetByIdAsync(batch.BatchId);
                    //    if (entity != null)
                    //    {
                    //        entity.QuantityOut += take;
                    //        entity.LastUpdated = DateTime.Now;
                    //        await _stockBatchService.UpdateAsync(entity);
                    //    }

                    //    remaining -= take;
                    //    if (remaining <= 0) break;
                    //}

                    // 4️ Cập nhật inventory (tồn kho)
                    var inventory = await _inventoryService.GetByProductIdRetriveOneObject(po.ProductId);
                    //if (inventory != null)
                    //{
                    //    inventory.Quantity -= po.Quantity ?? 0;
                    //    await _inventoryService.UpdateAsync(inventory);
                    //}

                    // 5️ Tạo transaction detail
                    var tranDetail = new TransactionDetailCreateVM
                    {
                        ProductId = po.ProductId,
                        TransactionId = transactionEntity.TransactionId,
                        Quantity = (int)(po.Quantity ?? 0),
                        UnitPrice = (decimal)(po.UnitPrice ?? 0),
                        Subtotal = (po.UnitPrice ?? 0) * (po.Quantity ?? 0)
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

        [HttpPut("UpdateOrder/{transactionId}")]
        public async Task<IActionResult> UpdateOrder(int transactionId, [FromBody] OrderRequest or)
        {
            //await _unitOfWork.BeginTransactionAsync();
            var listProductOrder = or.ListProductOrder;
            try
            {
                var transaction = await _transactionService.GetByIdAsync(transactionId);
                if (transaction == null)
                    return NotFound(ApiResponse<string>.Fail("Không tìm thấy đơn hàng", 404));

                // --- 1️⃣ Lấy danh sách chi tiết cũ ---
                var oldDetails = await _transactionDetailService.GetByTransactionId(transactionId);
                if (oldDetails == null || !oldDetails.Any())
                {
                    return NotFound(ApiResponse<string>.Fail("Không tìm thấy chi tiết đơn hàng", 404));
                }
                // --- 2️⃣ Khôi phục lại tồn kho & lô hàng ---
                //foreach (var detail in oldDetails)
                //{
                //    // Hoàn lại inventory
                //    var inventory = await _inventoryService.GetByProductIdRetriveOneObject(detail.ProductId);
                //    if (inventory != null)
                //    {
                //        inventory.Quantity += detail.Quantity;
                //        await _inventoryService.UpdateAsync(inventory);
                //    }

                //    // Hoàn lại stock batch (trả lại số lượng)
                //    //var stockBatches = await _stockBatchService.GetByProductIdForTransaction(detail.ProductId, transactionId);
                //    //foreach (var batch in stockBatches)
                //    //{
                //    //    batch.QuantityOut -= detail.Quantity; // hoặc số lượng thực tế theo batch
                //    //    if (batch.QuantityOut < 0) batch.QuantityOut = 0;
                //    //    await _stockBatchService.UpdateAsync(batch);
                //    //}
                //}

                // --- 3️ Xóa chi tiết cũ ---
                await _transactionDetailService.DeleteRange(oldDetails);

                // --- 4️ Áp dụng lại logic tạo đơn mới ---
                var listProductId = listProductOrder.Select(p => p.ProductId).ToList();
                //var listStockBatch = await _stockBatchService.GetByProductIdForOrder(listProductId);

                foreach (var po in listProductOrder)
                {
                    //var batches = listStockBatch
                    //    .Where(sb => sb.ProductId == po.ProductId && sb.QuantityIn > sb.QuantityOut && sb.ExpireDate > DateTime.Today)
                    //    .OrderBy(sb => sb.ImportDate)
                    //    .ToList();

                    //decimal remaining = po.Quantity ?? 0;

                    //foreach (var batch in batches)
                    //{
                    //    decimal available = (batch.QuantityIn - batch.QuantityOut) ?? 0;
                    //    if (available <= 0) continue;

                    //    decimal take = Math.Min(available, remaining);
                    //    batch.QuantityOut += take;
                    //    batch.LastUpdated = DateTime.Now;
                    //    await _stockBatchService.UpdateAsync(batch);

                    //    remaining -= take;
                    //    if (remaining <= 0) break;
                    //}

                    // Cập nhật inventory
                    var inventory = await _inventoryService.GetByProductIdRetriveOneObject(po.ProductId);
                    //if (inventory != null)
                    //{
                    //    inventory.Quantity -= po.Quantity ?? 0;
                    //    await _inventoryService.UpdateAsync(inventory);
                    //}

                    // Thêm transaction detail mới
                    var tranDetail = new TransactionDetailCreateVM
                    {
                        ProductId = po.ProductId,
                        TransactionId = transactionId,
                        Quantity = (int)(po.Quantity ?? 0),
                        UnitPrice = (decimal)(po.UnitPrice ?? 0),
                        Subtotal = (inventory?.AverageCost ?? 0) * (po.Quantity ?? 0)
                    };
                    var tranDetailEntity = _mapper.Map<TransactionDetailCreateVM, TransactionDetail>(tranDetail);
                    await _transactionDetailService.CreateAsync(tranDetailEntity);
                }
                if (or.Status.HasValue)
                {
                    transaction.Status = or.Status;
                }
                else
                {
                    // --- 5️ Cập nhật thông tin đơn hàng ---
                    transaction.Status = (int?)TransactionStatus.draft; // đang xử lý lại
                }
                // --- 6 cập nhật lại note nếu có ---
                if (!string.IsNullOrEmpty(or.Note))
                {
                    transaction.Note = or.Note;
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

    }
}
