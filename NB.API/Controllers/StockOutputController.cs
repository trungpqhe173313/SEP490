using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NB.Model.Entities;
using NB.Model.Enums;
using NB.Service.Common;
using NB.Service.Core.Enum;
using NB.Service.Core.Mapper;
using NB.Service.Dto;
using NB.Service.FinancialTransactionService;
using NB.Service.FinancialTransactionService.ViewModels;
using NB.Service.InventoryService;
using NB.Service.InventoryService.Dto;
using NB.Service.ProductService;
using NB.Service.ProductService.Dto;
using NB.Service.ReturnTransactionDetailService;
using NB.Service.ReturnTransactionDetailService.ViewModels;
using NB.Service.ReturnTransactionService;
using NB.Service.ReturnTransactionService.ViewModels;
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
using static System.DateTime;

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
        private readonly IReturnTransactionService _returnTransactionService;
        private readonly IReturnTransactionDetailService _returnTransactionDetailService;
        private readonly IFinancialTransactionService _financialTransactionService;
        private readonly ILogger<EmployeeController> _logger;
        private readonly IMapper _mapper;
        private readonly string transactionType = "Export";
        private readonly int generalWarehouseId = 1;
        public StockOutputController(
            ITransactionService transactionService,
            ITransactionDetailService transactionDetailService,
            IProductService productService,
            IStockBatchService stockBatchService,
            IUserService userService,
            IWarehouseService warehouseService,
            IInventoryService inventoryService,
            IReturnTransactionService returnTransactionService,
            IReturnTransactionDetailService returnTransactionDetailService,
            IFinancialTransactionService financialTransactionService,
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
            _returnTransactionService = returnTransactionService;
            _returnTransactionDetailService = returnTransactionDetailService;
            _financialTransactionService = financialTransactionService;
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
                    return Ok(ApiResponse<PagedList<TransactionDto>>.Ok(result));
                }
                var listWarehouseId = result.Items.Select(t => t.WarehouseId).ToList();
                var listWareHouse = await _warehouseService.GetByListWarehouseId(listWarehouseId);
                //lấy tất cả các khách hàng
                var listUser = _userService.GetAll();
                if (listWareHouse == null || !listWareHouse.Any())
                {
                    return NotFound(ApiResponse<PagedList<WarehouseDto>>.Fail("Không tìm thấy kho", 404));
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
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ" ));
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
                        transaction.PriceListId = detail.PriceListId;
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
                        return NotFound(ApiResponse<FullTransactionVM>.Fail("Không tìm thấy đơn hàng.", 404 ));
                    }
                }
                else if (Id <= 0)
                {
                    return BadRequest(ApiResponse<FullTransactionVM>.Fail("Id không hợp lệ" ));
                }

                var productDetails = await _transactionDetailService.GetByTransactionId(Id);
                if (productDetails == null || !productDetails.Any())
                {
                    return NotFound(ApiResponse<FullTransactionVM>.Fail("Không có thông tin cho giao dịch này.", 404 ));
                }
                foreach (var item in productDetails)
                {
                    var product = await _productService.GetById(item.ProductId);
                    item.ProductName = product != null ? product.ProductName : "N/A";
                    item.Code = product != null ? product.ProductCode : "N/A";

                }
                //var batches = await _stockBatchService.GetByTransactionId(Id);
                //var batch = batches.FirstOrDefault();

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
                    return NotFound(ApiResponse<UserDto>.Fail("Không tìm thấy đơn hàng", 404 ));
                }

                var warehouse = await _warehouseService.GetByIdAsync(result.WarehouseId);
                if (warehouse == null)
                {
                    return NotFound(ApiResponse<PagedList<WarehouseDto>>.Fail("Không tìm thấy kho", 404));
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
                    return NotFound(ApiResponse<UserDto>.Fail("Không tìm thấy đơn hàng", 404 ));
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
                return BadRequest(ApiResponse<ProductDto>.Fail("Không có sản phẩm nào"));

            var listProductId = listProductOrder.Select(p => p.ProductId).ToList();
            var listProduct = await _productService.GetByIds(listProductId);
            if (!listProduct.Any())
                return BadRequest(ApiResponse<ProductDto>.Fail("Không tìm thấy sản phẩm nào"));

            // Xác định WarehouseId (ưu tiên từ request, nếu không có thì lấy từ stockBatch)
            //int? warehouseId = or.WarehouseId ?? 1;
            int? warehouseId = or.WarehouseId;
            if (!warehouseId.HasValue)
            {
                // Lấy tất cả các lô hàng còn hàng và còn hạn để xác định kho
                var listStockBatch = await _stockBatchService.GetByProductIdForOrder(listProductId);
                if (listStockBatch == null || !listStockBatch.Any())
                {
                    return BadRequest(ApiResponse<string>.Fail("Không tìm thấy lô hàng khả dụng cho các sản phẩm này"));
                }
                warehouseId = listStockBatch.First().WarehouseId;
                // Nếu không có giá trị nữa thì sẽ lấy mặc định Id của warehouse tổng
                if (!warehouseId.HasValue)
                {
                    warehouseId = generalWarehouseId;
                }
            }

            // Kiểm tra kho có tồn tại không
            var warehouse = await _warehouseService.GetByIdAsync(warehouseId.Value);
            if (warehouse == null)
            {
                return NotFound(ApiResponse<string>.Fail("Không tìm thấy kho", 404));
            }

            // Lấy tất cả inventory theo danh sách sản phẩm và kho để kiểm tra xem lượng hàng hóa trong kho còn đủ không
            var listInventory = await _inventoryService.GetByWarehouseAndProductIds(warehouseId.Value, listProductOrder.Select(po => po.ProductId).ToList());

            foreach (var po in listProductOrder)
            {
                var orderQty = po.Quantity ?? 0;
                var inven = listInventory.FirstOrDefault(p => p.ProductId == po.ProductId && p.WarehouseId == warehouseId.Value);

                if (inven == null)
                {
                    var productCheck = await _productService.GetByIdAsync(po.ProductId);
                    var productName = productCheck?.ProductName ?? $"Sản phẩm {po.ProductId}";
                    return BadRequest(ApiResponse<InventoryDto>.Fail(
                        $"Không tìm thấy sản phẩm '{productName}' trong kho '{warehouse.WarehouseName}'"));
                }

                var invenQty = inven.Quantity ?? 0;

                if (orderQty > invenQty)
                {
                    var productCheck = await _productService.GetByIdAsync(po.ProductId);
                    var productName = productCheck?.ProductName ?? $"Sản phẩm {po.ProductId}";
                    return BadRequest(ApiResponse<InventoryDto>.Fail(
                        $"Sản phẩm '{productName}' trong kho '{warehouse.WarehouseName}' chỉ còn {invenQty}, không đủ {orderQty} yêu cầu.",
                        400));
                }
            }

            try
            {
                // 1️ Lấy tất cả các lô hàng còn hàng và còn hạn từ kho đã xác định
                var listStockBatch = await _stockBatchService.GetByProductIdForOrder(listProductId);
                var listStockBatchInWarehouse = listStockBatch
                    .Where(sb => sb.WarehouseId == warehouseId.Value
                        && (sb.QuantityIn ?? 0) > (sb.QuantityOut ?? 0)
                        && (sb.ExpireDate == null || sb.ExpireDate > DateTime.Today))
                    .ToList();
                
                if (listStockBatchInWarehouse == null || !listStockBatchInWarehouse.Any())
                {
                    return BadRequest(ApiResponse<string>.Fail($"Không tìm thấy lô hàng khả dụng cho các sản phẩm này trong kho '{warehouse.WarehouseName}'"));
                }

                // 2️ Tạo transaction (đơn hàng)
                var tranCreate = new TransactionCreateVM
                {
                    CustomerId = userId,
                    WarehouseId = warehouseId.Value, // Lưu kho xuất hàng
                    Note = or.Note,
                    TotalCost = or.TotalCost,
                    PriceListId = or.PriceListId
                };
                var transactionEntity = _mapper.Map<TransactionCreateVM, Transaction>(tranCreate);

                transactionEntity.Status = (int?)TransactionStatus.draft; // đang xử lý
                transactionEntity.TransactionDate = Now;
                transactionEntity.Type = "Export";
                transactionEntity.TransactionCode = $"EXPORT-{Now:yyyyMMdd}";
                await _transactionService.CreateAsync(transactionEntity);

                // 3️ Duyệt từng sản phẩm để tạo transaction detail
                decimal totalWeight = 0;
                foreach (var po in listProductOrder)
                {
                    // 4 Tạo transaction detail
                    var tranDetail = new TransactionDetailCreateVM
                    {
                        ProductId = po.ProductId,
                        TransactionId = transactionEntity.TransactionId,
                        Quantity = (int)(po.Quantity ?? 0),
                        UnitPrice = (decimal)(po.UnitPrice ?? 0),
                    };
                    var tranDetailEntity = _mapper.Map<TransactionDetailCreateVM, TransactionDetail>(tranDetail);
                    await _transactionDetailService.CreateAsync(tranDetailEntity);

                    // Tính TotalWeight
                    var product = await _productService.GetByIdAsync(po.ProductId);
                    if (product != null && product.WeightPerUnit.HasValue)
                    {
                        totalWeight += product.WeightPerUnit.Value * (po.Quantity ?? 0);
                    }
                }

                // Cập nhật TotalWeight vào transaction
                transactionEntity.TotalWeight = totalWeight;
                await _transactionService.UpdateAsync(transactionEntity);

                // 5 Trả về kết quả sau khi hoàn tất toàn bộ sản phẩm
                return Ok(ApiResponse<string>.Ok("Tạo đơn hàng thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo đơn hàng");
                return BadRequest(ApiResponse<string>.Fail("Có lỗi xảy ra khi tạo đơn hàng"));
            }
        }

        [HttpPut("UpdateTransactionInDraftStatus/{transactionId}")]
        public async Task<IActionResult> UpdateTransactionInDraftStatus(int transactionId, [FromBody] OrderRequest or)
        {
            var listProductOrder = or.ListProductOrder;
            var transaction = await _transactionService.GetByTransactionId(transactionId);
            if (transaction == null)
            {
                return NotFound(ApiResponse<TransactionDto>.Fail("Không tìm thấy đơn hàng", 404));
            }
            if (transaction.Status != (int)TransactionStatus.draft)
            {
                return BadRequest(ApiResponse<string>.Fail("Đơn hàng không trong trạng thái nháp"));
            }
            if (listProductOrder == null || !listProductOrder.Any())
            {
                return BadRequest(ApiResponse<ProductDto>.Fail("Không có sản phẩm nào"));
            }
            var listProductId = listProductOrder.Select(p => p.ProductId).ToList();
            var listProduct = await _productService.GetByIds(listProductId);
            if (!listProduct.Any())
                return BadRequest(ApiResponse<ProductDto>.Fail("Không tìm thấy sản phẩm nào"));
            // Lấy WarehouseId từ transaction
            var transactionWarehouseId = transaction.WarehouseId;
            var transactionWarehouse = await _warehouseService.GetByIdAsync(transactionWarehouseId);
            if (transactionWarehouse == null)
            {
                return NotFound(ApiResponse<string>.Fail("Không tìm thấy kho của đơn hàng", 404));
            }

            // Lấy tất cả inventory theo danh sách sản phẩm và kho để kiểm tra xem lượng hàng hóa trong kho còn đủ không
            var listInventory = await _inventoryService.GetByWarehouseAndProductIds(transactionWarehouseId, listProductOrder.Select(po => po.ProductId).ToList());

            foreach (var po in listProductOrder)
            {
                var orderQty = po.Quantity ?? 0;
                var inven = listInventory.FirstOrDefault(p => p.ProductId == po.ProductId && p.WarehouseId == transactionWarehouseId);

                if (inven == null)
                {
                    var productCheck = await _productService.GetByIdAsync(po.ProductId);
                    var productName = productCheck?.ProductName ?? $"Sản phẩm {po.ProductId}";
                    return BadRequest(ApiResponse<InventoryDto>.Fail(
                        $"Không tìm thấy sản phẩm '{productName}' trong kho '{transactionWarehouse.WarehouseName}'"));
                }

                var invenQty = inven.Quantity ?? 0;

                if (orderQty > invenQty)
                {
                    var productCheck = await _productService.GetByIdAsync(po.ProductId);
                    var productName = productCheck?.ProductName ?? $"Sản phẩm {po.ProductId}";
                    return BadRequest(ApiResponse<InventoryDto>.Fail(
                        $"Sản phẩm '{productName}' trong kho '{transactionWarehouse.WarehouseName}' chỉ còn {invenQty}, không đủ {orderQty} yêu cầu.",
                        400));
                }
            }
            try
            {
                // Xóa chi tiết đơn hàng cũ
                var existingDetails = await _transactionDetailService.GetByTransactionId(transactionId);
                if (existingDetails == null || !existingDetails.Any())
                {
                    return BadRequest(ApiResponse<TransactionDetailDto>.Fail($"Không tìm thấy chi tiết đơn hàng"));
                    
                }
                await _transactionDetailService.DeleteRange(existingDetails);
                // Thêm chi tiết đơn hàng mới
                decimal totalWeight = 0;
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

                    // Tính TotalWeight
                    var product = await _productService.GetByIdAsync(po.ProductId);
                    if (product != null && product.WeightPerUnit.HasValue)
                    {
                        totalWeight += product.WeightPerUnit.Value * (po.Quantity ?? 0);
                    }
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
                // Cập nhật bảng giá sử dụng
                if (or.PriceListId.HasValue)
                {
                    transaction.PriceListId = or.PriceListId;
                }
                // Cập nhật TotalWeight
                transaction.TotalWeight = totalWeight;
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

        [HttpPut("UpdateToOrderStatus/{transactionId}")]
        public async Task<IActionResult> UpdateToOrderStatus(int transactionId)
        {
            var transaction = await _transactionService.GetByTransactionId(transactionId);
            if (transaction == null)
            {
                return NotFound(ApiResponse<TransactionDto>.Fail("Không tìm thấy đơn hàng", 404 ));
            }
            var listTransDetail = await _transactionDetailService.GetByTransactionId(transactionId);
            if (listTransDetail == null || !listTransDetail.Any())
            {
                return NotFound(ApiResponse<TransactionDto>.Fail("Không tìm thấy chi tiết đơn hàng", 404 ));
            }
            var listProductOrder = listTransDetail.Select(item => new
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity
            });

            var listProductId = listProductOrder.Select(p => p.ProductId).ToList();
            var listProduct = await _productService.GetByIds(listProductId);
            if (!listProduct.Any())
                return BadRequest(ApiResponse<ProductDto>.Fail("Không tìm thấy sản phẩm nào" ));

            // Lấy WarehouseId từ transaction
            var transactionWarehouseId = transaction.WarehouseId;
            var transactionWarehouse = await _warehouseService.GetByIdAsync(transactionWarehouseId);
            if (transactionWarehouse == null)
            {
                return NotFound(ApiResponse<string>.Fail("Không tìm thấy kho của đơn hàng", 404 ));
            }

            // Lấy tất cả inventory theo danh sách sản phẩm và kho để kiểm tra xem lượng hàng hóa trong kho còn đủ không
            var listInventory = await _inventoryService.GetByWarehouseAndProductIds(transactionWarehouseId, listProductOrder.Select(po => po.ProductId).ToList());

            foreach (var po in listProductOrder)
            {
                var orderQty = po.Quantity;
                var inven = listInventory.FirstOrDefault(p => p.ProductId == po.ProductId && p.WarehouseId == transactionWarehouseId);

                if (inven == null)
                {
                    var productCheck = await _productService.GetByIdAsync(po.ProductId);
                    var productName = productCheck?.ProductName ?? $"Sản phẩm {po.ProductId}";
                    return BadRequest(ApiResponse<InventoryDto>.Fail(
                        $"Không tìm thấy sản phẩm '{productName}' trong kho '{transactionWarehouse.WarehouseName}'" ));
                }

                var invenQty = inven.Quantity ?? 0;

                if (orderQty > invenQty)
                {
                    var productCheck = await _productService.GetByIdAsync(po.ProductId);
                    var productName = productCheck?.ProductName ?? $"Sản phẩm {po.ProductId}";
                    return BadRequest(ApiResponse<InventoryDto>.Fail(
                        $"Sản phẩm '{productName}' trong kho '{transactionWarehouse.WarehouseName}' chỉ còn {invenQty}, không đủ {orderQty} yêu cầu.",
                        400));
                }
            }

            try
            {
                // 1️ Lấy tất cả các lô hàng còn hàng và còn hạn từ kho của transaction
                var listStockBatch = await _stockBatchService.GetByProductIdForOrder(listProductId);
                var listStockBatchInWarehouse = listStockBatch
                    .Where(sb => sb.WarehouseId == transactionWarehouseId
                        && (sb.QuantityIn ?? 0) > (sb.QuantityOut ?? 0)
                        && (sb.ExpireDate == null || sb.ExpireDate > DateTime.Today))
                    .ToList();

                // 3️ Duyệt từng sản phẩm để lấy lô & cập nhật tồn
                foreach (var po in listProductOrder)
                {
                    var batches = listStockBatchInWarehouse
                        .Where(sb => sb.ProductId == po.ProductId)
                        .OrderBy(sb => sb.ImportDate) // FIFO - hàng cũ nhất trước
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

                    // 4️ Cập nhật inventory (tồn kho) ở kho của transaction
                    var inventoryEntity = await _inventoryService.GetEntityByWarehouseAndProductIdAsync(transactionWarehouseId, po.ProductId);
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
                return BadRequest(ApiResponse<string>.Fail("Đơn hàng mới không có sản phẩm nào để cập nhật." ));
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
                    return NotFound(ApiResponse<string>.Fail("Không tìm thấy đơn hàng", 404 ));
                if (transaction.Status != (int)TransactionStatus.order)
                {
                    return BadRequest(ApiResponse<string>.Fail("Đơn hàng không trong trạng thái lên đơn" ));
                }

                // Lấy WarehouseId từ transaction
                var transactionWarehouseId = transaction.WarehouseId;
                var transactionWarehouse = await _warehouseService.GetByIdAsync(transactionWarehouseId);
                if (transactionWarehouse == null)
                {
                    return NotFound(ApiResponse<string>.Fail("Không tìm thấy kho của đơn hàng", 404 ));
                }

                // --- 1️⃣ Lấy danh sách chi tiết cũ ---
                var oldDetails = (await _transactionDetailService.GetByTransactionId(transactionId))
                    ?? new List<TransactionDetailDto>();
                if (oldDetails == null || !oldDetails.Any())
                {
                    return NotFound(ApiResponse<string>.Fail("Không tìm thấy chi tiết đơn hàng" ));
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

                    // Trả lại Inventory ở kho của transaction
                    var inventoryEntity = await _inventoryService.GetEntityByWarehouseAndProductIdAsync(transactionWarehouseId, productId);
                    if (inventoryEntity != null)
                    {
                        inventoryEntity.Quantity += oldQuantity;
                        inventoryEntity.LastUpdated = DateTime.Now;
                        inventoryUpdates[productId] = inventoryEntity;
                    }

                    // Trả lại StockBatch theo LIFO từ kho của transaction
                    var batchesToRevert = await _stockBatchService.GetByProductIdForOrder(new List<int> { productId });
                    if (batchesToRevert != null && batchesToRevert.Any())
                    {
                        var revertList = batchesToRevert
                            .Where(b => b.WarehouseId == transactionWarehouseId
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
                        // Kiểm tra đủ hàng không ở kho của transaction
                        var inventoryDto = await _inventoryService.GetByWarehouseAndProductId(transactionWarehouseId, productId);
                        if (inventoryDto == null)
                        {
                            var product = await _productService.GetByIdAsync(productId);
                            return BadRequest(ApiResponse<string>.Fail(
                                $"Không tìm thấy sản phẩm '{product?.ProductName ?? productId.ToString()}' trong kho '{transactionWarehouse.WarehouseName}'." ));
                        }

                        if ((inventoryDto.Quantity ?? 0) < diff)
                        {
                            var product = await _productService.GetByIdAsync(productId);
                            return BadRequest(ApiResponse<string>.Fail(
                                $"Sản phẩm '{product?.ProductName ?? productId.ToString()}' trong kho '{transactionWarehouse.WarehouseName}' chỉ còn {inventoryDto.Quantity}, không đủ {diff} để tăng." ));
                        }

                        // Lấy Inventory entity để update ở kho của transaction
                        var inventoryEntity = await _inventoryService.GetEntityByWarehouseAndProductIdAsync(transactionWarehouseId, productId);
                        if (inventoryEntity != null)
                        {
                            inventoryEntity.Quantity -= diff;
                            inventoryEntity.LastUpdated = DateTime.Now;
                            inventoryUpdates[productId] = inventoryEntity;
                        }

                        // Lấy thêm StockBatch từ kho của transaction
                        var listStockBatch = await _stockBatchService.GetByProductIdForOrder(new List<int> { productId });
                        if (listStockBatch == null || !listStockBatch.Any())
                        {
                            return BadRequest(ApiResponse<string>.Fail($"Không tìm thấy lô hàng khả dụng cho sản phẩm {productId} trong kho '{transactionWarehouse.WarehouseName}'." ));
                        }
                        var batches = listStockBatch
                            .Where(sb => sb.ProductId == productId
                                && sb.WarehouseId == transactionWarehouseId
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
                            return BadRequest(ApiResponse<string>.Fail($"Không đủ hàng trong các lô cho sản phẩm {productId}" ));
                        }
                    }
                    else
                    {
                        // Đơn mới ít hơn - Trả lại hàng (diff < 0 nên cần trả lại |diff|)
                        var returnQuantity = Math.Abs(diff);

                        // Trả lại Inventory ở kho của transaction
                        var inventoryEntity = await _inventoryService.GetEntityByWarehouseAndProductIdAsync(transactionWarehouseId, productId);
                        if (inventoryEntity != null)
                        {
                            inventoryEntity.Quantity += returnQuantity;
                            inventoryEntity.LastUpdated = DateTime.Now;
                            inventoryUpdates[productId] = inventoryEntity;
                        }

                        // Trả lại StockBatch theo LIFO từ kho của transaction
                        var batchesToRevert = await _stockBatchService.GetByProductIdForOrder(new List<int> { productId });
                        if (batchesToRevert != null && batchesToRevert.Any())
                        {
                            var revertList = batchesToRevert
                                .Where(b => b.WarehouseId == transactionWarehouseId
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
                    // Kiểm tra đủ hàng cho tất cả sản phẩm mới trước khi trừ tồn ở kho của transaction
                    var listInventory = await _inventoryService.GetByWarehouseAndProductIds(transactionWarehouseId, newProducts) ?? new List<InventoryDto>();
                    foreach (var productId in newProducts)
                    {
                        var newQuantity = newProductDict[productId];
                        var inven = listInventory.FirstOrDefault(p => p.ProductId == productId && p.WarehouseId == transactionWarehouseId);
                        if (inven == null)
                        {
                            var product = await _productService.GetByIdAsync(productId);
                            return BadRequest(ApiResponse<string>.Fail(
                                $"Không tìm thấy sản phẩm '{product?.ProductName ?? productId.ToString()}' trong kho '{transactionWarehouse.WarehouseName}'" ));
                        }
                        if (inven.Quantity < newQuantity)
                        {
                            var product = await _productService.GetByIdAsync(productId);
                            return BadRequest(ApiResponse<string>.Fail(
                                $"Sản phẩm '{product?.ProductName}' trong kho '{transactionWarehouse.WarehouseName}' chỉ còn {inven.Quantity}, không đủ {newQuantity} yêu cầu." ));
                        }
                    }

                    // Lấy StockBatch cho các sản phẩm mới từ kho của transaction
                    var listStockBatch = await _stockBatchService.GetByProductIdForOrder(newProducts) ?? new List<StockBatchDto>();
                    foreach (var po in listProductOrder.Where(p => newProducts.Contains(p.ProductId)))
                    {
                        var batches = listStockBatch
                            .Where(sb => sb.ProductId == po.ProductId
                                && sb.WarehouseId == transactionWarehouseId
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
                                stockBatchUpdates[batch.BatchId] = batchEntity;
                            }
                            remaining -= take;
                        }

                        if (remaining > 0)
                        {
                            return BadRequest(ApiResponse<string>.Fail($"Không đủ hàng trong các lô cho sản phẩm {po.ProductId}" ));
                        }

                        // Cập nhật Inventory ở kho của transaction
                        var inventoryEntity = await _inventoryService.GetEntityByWarehouseAndProductIdAsync(transactionWarehouseId, po.ProductId);
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

                decimal totalWeight = 0;
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

                    // Tính TotalWeight
                    var product = await _productService.GetByIdAsync(po.ProductId);
                    if (product != null && product.WeightPerUnit.HasValue)
                    {
                        totalWeight += product.WeightPerUnit.Value * (po.Quantity ?? 0);
                    }
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
                if (or.PriceListId.HasValue)
                {
                    transaction.PriceListId = or.PriceListId;
                }
                // Cập nhật TotalWeight
                transaction.TotalWeight = totalWeight;
                await _transactionService.UpdateAsync(transaction);

                return Ok(ApiResponse<string>.Ok("Cập nhật đơn hàng thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật đơn hàng");
                return BadRequest(ApiResponse<string>.Fail("Có lỗi xảy ra khi cập nhật đơn hàng"));
            }
        }

        [HttpPut("UpdateToDoneStatus/{transactionId}")]
        public async Task<IActionResult> UpdateToDoneStatus(int transactionId)
        {
            var transaction = await _transactionService.GetByTransactionId(transactionId);
            if (transaction == null)
            {
                return NotFound(ApiResponse<TransactionDto>.Fail("Không tìm thấy đơn hàng", 404 ));
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

        [HttpPut("UpdateToDeliveringStatus/{transactionId}")]
        public async Task<IActionResult> UpdateToDeliveringStatus(int transactionId)
        {
            var transaction = await _transactionService.GetByTransactionId(transactionId);
            if (transaction == null)
            {
                return NotFound(ApiResponse<TransactionDto>.Fail("Không tìm thấy đơn hàng", 404 ));
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

        [HttpPut("UpdateToCancelStatus/{transactionId}")]
        public async Task<IActionResult> UpdateToCancelStatus(int transactionId)
        {
            var transaction = await _transactionService.GetByTransactionId(transactionId);
            if (transaction == null)
            {
                return NotFound(ApiResponse<TransactionDto>.Fail("Không tìm thấy đơn hàng", 404));
            }
            if (transaction.Status != (int)TransactionStatus.draft)
            {
                return BadRequest(ApiResponse<TransactionDto>.Fail("Đơn hàng không trong trạng thái nháp", 404));
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
                return NotFound(ApiResponse<TransactionDto>.Fail("Không tìm thấy đơn hàng", 404 ));
            }
            //Kiểm tra xem xem đơn hàng có phải đơn nhập
            if (transaction.Type != transactionType)
            {
                return BadRequest(ApiResponse<TransactionDto>.Fail("Đơn hàng không phải đơn xuất" ));
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
                financialTransactionEntity.Amount = transaction.TotalCost ?? 0;
                financialTransactionEntity.TransactionDate = DateTime.Now;
                financialTransactionEntity.Type = FinancialTransactionType.ThuTienKhach.ToString();

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
                return NotFound(ApiResponse<TransactionDto>.Fail("Không tìm thấy đơn hàng", 404 ));
            }
            //Kiểm tra xem xem đơn hàng có phải đơn nhập
            if (transaction.Type != transactionType)
            {
                return BadRequest(ApiResponse<TransactionDto>.Fail("Đơn hàng không phải đơn xuất" ));
            }
            if (transaction.Status == (int)TransactionStatus.paidInFull)
            {
                return BadRequest(ApiResponse<Transaction>.Fail("Đơn hàng đã được thanh toán"));
            }
            var financialTransactions = await _financialTransactionService.GetByRelatedTransactionID(transactionId);
            decimal totalPaid = 0;
            if (financialTransactions != null && financialTransactions.Any())
            {
                totalPaid = financialTransactions.Sum(ft => ft.Amount);
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
                financialTransactionEntity.Amount = newPaymentAmount;
                financialTransactionEntity.TransactionDate = DateTime.Now;
                financialTransactionEntity.Type = FinancialTransactionType.ThuTienKhach.ToString();

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


        [HttpGet("GetTransactionStatus")]
        public IActionResult GetTransactionStatus()
        {
            try
            {
                var listStatus = new List<TransactionStatus>
                {
                    TransactionStatus.draft,
                    TransactionStatus.order,
                    TransactionStatus.delivering,
                    TransactionStatus.failure,
                    TransactionStatus.cancel,
                    TransactionStatus.paidInFull,
                    TransactionStatus.partiallyPaid
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
                return BadRequest(ApiResponse<string>.Fail("Danh sách sản phẩm trả hàng không được rỗng." ));
            }

            try
            {
                // Lấy thông tin đơn hàng
                var transaction = await _transactionService.GetByIdAsync(transactionId);
                if (transaction == null)
                    return NotFound(ApiResponse<string>.Fail("Không tìm thấy đơn hàng", 404 ));

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
                    // Lấy ra mã sp và số lượng trả
                    var productId = returnItem.Key;
                    var returnQuantity = returnItem.Value;

                    // Kiểm tra sản phẩm có trong đơn hàng không
                    if (!currentProductDict.ContainsKey(productId))
                    {
                        var product = await _productService.GetByIdAsync(productId);
                        return BadRequest(ApiResponse<string>.Fail(
                            $"Sản phẩm '{product?.ProductName ?? productId.ToString()}' không có trong đơn hàng này." ));
                    }

                    // Kiểm tra số lượng trả có hợp lệ không
                    var currentQuantity = currentProductDict[productId];
                    if (returnQuantity > currentQuantity)
                    {
                        var product = await _productService.GetByIdAsync(productId);
                        return BadRequest(ApiResponse<string>.Fail(
                            $"Số lượng trả của sản phẩm '{product?.ProductName ?? productId.ToString()}' ({returnQuantity}) vượt quá số lượng trong đơn ({currentQuantity})." ));
                    }

                    if (returnQuantity <= 0)
                    {
                        var product = await _productService.GetByIdAsync(productId);
                        return BadRequest(ApiResponse<string>.Fail(
                            $"Số lượng trả của sản phẩm '{product?.ProductName ?? productId.ToString()}' phải lớn hơn 0." ));
                    }
                }

                // Dictionary để track các update (tránh update trùng lặp)
                var inventoryUpdates = new Dictionary<int, Inventory>();
                var stockBatchUpdates = new Dictionary<int, StockBatch>();

                // Tính lại TotalWeight cho các sản phẩm bị trả
                decimal returnWeight = 0;

                // Xử lý từng sản phẩm trả hàng
                foreach (var returnItem in returnProductDict)
                {
                    var productId = returnItem.Key;
                    var returnQuantity = returnItem.Value;

                    // Tính weight của sản phẩm trả
                    var productForWeight = await _productService.GetByIdAsync(productId);
                    if (productForWeight != null && productForWeight.WeightPerUnit.HasValue)
                    {
                        returnWeight += productForWeight.WeightPerUnit.Value * returnQuantity;
                    }

                    // Trả lại Inventory ở kho của transaction
                    var inventoryEntity = await _inventoryService.GetEntityByWarehouseAndProductIdAsync(transaction.WarehouseId, productId);
                    if (inventoryEntity != null)
                    {
                        inventoryEntity.Quantity += returnQuantity;
                        inventoryEntity.LastUpdated = DateTime.Now;
                        inventoryUpdates[productId] = inventoryEntity;
                    }

                    // Trả lại StockBatch theo LIFO (Last In First Out) từ kho của transaction
                    var batchesToRevert = await _stockBatchService.GetByProductIdForOrder(new List<int> { productId });
                    if (batchesToRevert != null && batchesToRevert.Any())
                    {
                        // Lọc ra những lô nằm trong kho đã xuất
                        var revertList = batchesToRevert
                            .Where(b => b.WarehouseId == transaction.WarehouseId
                                && (b.QuantityOut ?? 0) > 0)
                            .OrderByDescending(b => b.ImportDate)
                            .ToList();

                        decimal toRevert = returnQuantity;
                        foreach (var b in revertList)
                        {
                            // Nếu số lượng trả đã về 0 thì thoát vòng lặp
                            if (toRevert <= 0) break;
                            // lấy số lượng đã xuất của lô
                            var availableOut = b.QuantityOut ?? 0;
                            // Nếu lô chưa xuất bao nào thì sẽ chuyển sang lô tiếp theo
                            if (availableOut <= 0) continue;

                            var takeBack = Math.Min(availableOut, toRevert);
                            // Trường hợp lô đấy đã trừ ở trước và đã được thêm vào Dictionary rồi
                            if (stockBatchUpdates.ContainsKey(b.BatchId))
                            {
                                stockBatchUpdates[b.BatchId].QuantityOut -= takeBack;
                                if (stockBatchUpdates[b.BatchId].QuantityOut < 0)
                                    stockBatchUpdates[b.BatchId].QuantityOut = 0;
                            }
                            // Nếu lô lần đầu tiên được lấy ra để update lại số lượng 
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
                var updatedDetails = new List<TransactionDetail>();
                //decimal totalCostReduction = 0;

                foreach (var detail in currentDetails)
                {
                    if (returnProductDict.ContainsKey(detail.ProductId))
                    {
                        // Lấy ra số lượng phải trả của sản phẩm
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
                if (transaction.TotalCost.HasValue)
                {
                    //gia goc tru di gia tong so hang bi tra
                    transaction.TotalCost -= or.TotalCost;
                    if (transaction.TotalCost < 0) transaction.TotalCost = 0;
                }
                // Cập nhật TotalWeight - trừ đi weight của hàng bị trả
                if (transaction.TotalWeight.HasValue)
                {
                    transaction.TotalWeight -= returnWeight;
                    if (transaction.TotalWeight < 0) transaction.TotalWeight = 0;
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
                    transaction.Status = (int?)TransactionStatus.cancel;
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
