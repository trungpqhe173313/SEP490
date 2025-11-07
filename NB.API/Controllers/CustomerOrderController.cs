using Microsoft.AspNetCore.Mvc;
using NB.Model.Enums;
using NB.Service.Common;
using NB.Service.Core.Enum;
using NB.Service.Core.Mapper;
using NB.Service.Dto;
using NB.Service.InventoryService;
using NB.Service.ProductService;
using NB.Service.StockBatchService;
using NB.Service.TransactionDetailService;
using NB.Service.TransactionService;
using NB.Service.TransactionService.Dto;
using NB.Service.UserService;
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
    }
}
