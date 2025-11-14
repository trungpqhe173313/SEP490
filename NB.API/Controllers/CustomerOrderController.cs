using Microsoft.AspNetCore.Mvc;
using NB.Model.Enums;
using NB.Service.Common;
using NB.Service.Core.Enum;
using NB.Service.Core.Mapper;
using NB.Service.Dto;
using NB.Service.InventoryService;
using NB.Service.ProductService;
using NB.Service.StockBatchService;
using NB.Service.SupplierService.Dto;
using NB.Service.TransactionDetailService;
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
    [Route("api/customerorder")]
    public class CustomerOrderController : Controller
    {
        private readonly ITransactionService _transactionService;
        private readonly ITransactionDetailService _transactionDetailService;
        private readonly IProductService _productService;
        private readonly IUserService _userService;
        private readonly IStockBatchService _stockBatchService;
        private readonly IWarehouseService _warehouseService;
        private readonly IInventoryService _inventoryService;
        private readonly ILogger<CustomerOrderController> _logger;
        private readonly IMapper _mapper;
        private readonly string transactionType = "Export";
        public CustomerOrderController(
            ITransactionService transactionService,
            ITransactionDetailService transactionDetailService,
            IProductService productService,
            IStockBatchService stockBatchService,
            IUserService userService,
            IWarehouseService warehouseService,
            IInventoryService inventoryService,
            IMapper mapper,
            ILogger<CustomerOrderController> logger)
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
        /// Lấy ra tất cả các các đơn hàng theo Id khách hàng đang ở trạng thái đơn nháp, lên đơn, đang giao
        /// </summary>
        /// <param name="search"> tìm các đơn hàng theo một số điều kiện</param>
        /// <returns>các đơn hàng thỏa mãn các điều kiện của search</returns>
        [HttpPost("GetOrderList")]
        public async Task<IActionResult> GetOrderList([FromBody] TransactionSearch search)
        {
            try
            {
                if (search.CustomerId == null ||!search.CustomerId.HasValue)
                {
                    return BadRequest(ApiResponse<PagedList<UserDto>>.Fail("Yêu cầu Id khách hàng"));
                }
                var user = await _userService.GetByUserId((int)search.CustomerId);
                if (user == null)
                {
                    return NotFound(ApiResponse<PagedList<UserDto>>.Fail("Không tìm thấy người dùng"));
                }
                //tạo danh sách các trạng thái của view order list bao gồm: đơn nháp, lên đơn, đang giao
                List<int> listStatus = new List<int>
                {
                    (int)TransactionStatus.draft,
                    (int)TransactionStatus.order,
                    (int)TransactionStatus.delivering
                };
                //Danh sách các transaction
                var result = await _transactionService.GetByListStatus(search, listStatus);
                if (result?.Items == null || !result.Items.Any())
                {
                    return Ok(ApiResponse<PagedList<TransactionDto>>.Ok(result ?? new PagedList<TransactionDto>(new List<TransactionDto>(), 1, 10, 0)));
                }
                var listWarehouseId = result.Items.Select(t => t.WarehouseId).Distinct().ToList();
                var listWareHouse = await _warehouseService.GetByListWarehouseId(listWarehouseId);
                if (listWareHouse == null || !listWareHouse.Any())
                {
                    // Không tìm thấy kho nhưng vẫn trả về kết quả với warehouse name là null/empty
                    _logger.LogWarning($"Không tìm thấy warehouse cho các ID: {string.Join(", ", listWarehouseId)}");
                }
                foreach (var t in result.Items)
                {
                    if (user != null)
                    {
                        t.FullName = user.FullName;
                    }

                    //lay tên kho
                    if (listWareHouse != null && listWareHouse.Any())
                    {
                        var warehouse = listWareHouse.FirstOrDefault(w => w.WarehouseId == t.WarehouseId);
                        if (warehouse != null)
                        {
                            t.WarehouseName = warehouse.WarehouseName;
                        }
                    }
                    //gắn statusName cho transaction
                    TransactionStatus statusName = (TransactionStatus)t.Status;
                    t.StatusName = statusName.GetDescription();
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
        /// Lấy ra tất cả các các đơn hàng theo Id khách hàng đang ở trạng thái hoàn thanh, hủy
        /// </summary>
        /// <param name="search"> tìm các đơn hàng theo một số điều kiện</param>
        /// <returns>các đơn hàng thỏa mãn các điều kiện của search</returns>
        [HttpPost("GetOrderHistory")]
        public async Task<IActionResult> GetOrderHistory([FromBody] TransactionSearch search)
        {
            try
            {
                if (search.CustomerId == null || !search.CustomerId.HasValue)
                {
                    return BadRequest(ApiResponse<PagedList<UserDto>>.Fail("Yêu cầu Id khách hàng"));
                }
                var user = await _userService.GetByUserId((int)search.CustomerId);
                if (user == null)
                {
                    return NotFound(ApiResponse<PagedList<UserDto>>.Fail("Không tìm thấy người dùng"));
                }
                //tạo danh sách các trạng thái của view order history bao gồm: hoàn thành, hủy
                List<int> listStatus = new List<int>
                {
                    (int)TransactionStatus.done,
                    (int)TransactionStatus.cancel
                };
                //Danh sách các transaction
                var result = await _transactionService.GetByListStatus(search, listStatus);
                if (result?.Items == null || !result.Items.Any())
                {
                    return Ok(ApiResponse<PagedList<TransactionDto>>.Ok(result ?? new PagedList<TransactionDto>(new List<TransactionDto>(), 1, 10, 0)));
                }
                var listWarehouseId = result.Items.Select(t => t.WarehouseId).Distinct().ToList();
                var listWareHouse = await _warehouseService.GetByListWarehouseId(listWarehouseId);
                if (listWareHouse == null || !listWareHouse.Any())
                {
                    // Không tìm thấy kho nhưng vẫn trả về kết quả với warehouse name là null/empty
                    _logger.LogWarning($"Không tìm thấy warehouse cho các ID: {string.Join(", ", listWarehouseId)}");
                }
                foreach (var t in result.Items)
                {
                    if (user != null)
                    {
                        t.FullName = user.FullName;
                    }

                    //lay tên kho
                    if (listWareHouse != null && listWareHouse.Any())
                    {
                        var warehouse = listWareHouse.FirstOrDefault(w => w.WarehouseId == t.WarehouseId);
                        if (warehouse != null)
                        {
                            t.WarehouseName = warehouse.WarehouseName;
                        }
                    }
                    //gắn statusName cho transaction
                    TransactionStatus statusName = (TransactionStatus)t.Status;
                    t.StatusName = statusName.GetDescription();
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

            // Kiểm tra Id hợp lệ trước
            if (Id <= 0)
            {
                return BadRequest(ApiResponse<FullTransactionExportVM>.Fail("Id không hợp lệ", 400));
            }

            try
            {
                var transaction = new FullTransactionExportVM();
                var detail = await _transactionService.GetByTransactionId(Id);
                if (detail == null)
                {
                    return NotFound(ApiResponse<FullTransactionExportVM>.Fail("Không tìm thấy đơn hàng.", 404));
                }

                transaction.Status = detail.Status;
                transaction.TransactionId = detail.TransactionId;
                transaction.TransactionDate = detail.TransactionDate ?? DateTime.MinValue;
                transaction.WarehouseName = (await _warehouseService.GetById(detail.WarehouseId))?.WarehouseName ?? "N/A";
                
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

                var productDetails = await _transactionDetailService.GetByTransactionId(Id);
                // Đơn hàng có thể không có sản phẩm (đơn hàng trống), vẫn hợp lệ
                var listResult = new List<TransactionDetailOutputVM>();
                if (productDetails != null && productDetails.Any())
                {
                    foreach (var item in productDetails)
                    {
                        var product = await _productService.GetById(item.ProductId);
                        item.ProductName = product != null ? product.ProductName : "N/A";
                    }

                    listResult = productDetails.Select(item => new TransactionDetailOutputVM
                    {
                        TransactionDetailId = item.Id,
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        UnitPrice = item.UnitPrice,
                        WeightPerUnit = item.WeightPerUnit,
                        Quantity = item.Quantity,
                        SubTotal = item.Subtotal
                    }).ToList();
                }

                transaction.list = listResult;
                return Ok(ApiResponse<FullTransactionExportVM>.Ok(transaction));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy dữ liệu đơn hàng");
                return BadRequest(ApiResponse<FullTransactionExportVM>.Fail("Có lỗi xảy ra khi lấy dữ liệu"));
            }
        }
    }
}
