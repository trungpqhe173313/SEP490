using Microsoft.AspNetCore.Mvc;
using NB.Model.Entities;
using NB.Model.Enums;
using NB.Service.Common;
using NB.Service.Core.Enum;
using NB.Service.Dto;
using NB.Service.ProductService;
using NB.Service.ProductService.Dto;
using NB.Service.ReturnTransactionDetailService;
using NB.Service.ReturnTransactionDetailService.Dto;
using NB.Service.ReturnTransactionService;
using NB.Service.ReturnTransactionService.Dto;
using NB.Service.ReturnTransactionService.ViewModels;
using NB.Service.SupplierService.Dto;
using NB.Service.TransactionService;
using NB.Service.TransactionService.Dto;
using NB.Service.UserService;
using NB.Service.UserService.Dto;
using NB.Service.UserService.ViewModels;
using NB.Service.WarehouseService;
using NB.Service.WarehouseService.Dto;
using NB.Service.SupplierService;
using NB.Service.SupplierService.ViewModels;

namespace NB.API.Controllers
{
    [Route("api/returnorder")]
    public class ReturnOrderController : Controller
    {
        private readonly IReturnTransactionService _returnTransactionService;
        private readonly IReturnTransactionDetailService _returnTransactionDetailService;
        private readonly ITransactionService _transactionService;
        private readonly IProductService _productService;
        private readonly IUserService _userService;
        private readonly IWarehouseService _warehouseService;
        private readonly ISupplierService _supplierService;
        private readonly ILogger<ReturnOrderController> _logger;

        public ReturnOrderController(
            IReturnTransactionService returnTransactionService,
            IReturnTransactionDetailService returnTransactionDetailService,
            ITransactionService transactionService,
            IProductService productService,
            IUserService userService,
            IWarehouseService warehouseService,
            ISupplierService supplierService,
            ILogger<ReturnOrderController> logger)
        {
            _returnTransactionService = returnTransactionService;
            _returnTransactionDetailService = returnTransactionDetailService;
            _transactionService = transactionService;
            _productService = productService;
            _userService = userService;
            _warehouseService = warehouseService;
            _supplierService = supplierService;
            _logger = logger;
        }

        /// <summary>
        /// Lấy ra tất cả các đơn trả hàng (dùng chung cho cả Import và Export)
        /// </summary>
        /// <param name="search">Tìm kiếm đơn trả hàng theo Type (Import/Export), TransactionId, v.v.</param>
        /// <returns>Danh sách đơn trả hàng thỏa mãn điều kiện</returns>
        [HttpPost("GetData")]
        public async Task<IActionResult> GetData([FromBody] ReturnOrderSearch search)
        {
            try
            {
                // Gọi service để lấy dữ liệu
                var result = await _returnTransactionService.GetData(search);
                if (result.Items == null || !result.Items.Any())
                {
                    return Ok(ApiResponse<PagedList<ReturnOrderDto>>.Ok(result));
                }

                // Lấy danh sách WarehouseId để query một lần
                var listWarehouseId = result.Items.Select(t => t.WarehouseId).Distinct().ToList();
                var listWareHouse = await _warehouseService.GetByListWarehouseId(listWarehouseId);

                // Lấy danh sách CustomerId (cho Export)
                var listCustomerId = result.Items
                    .Where(t => t.TransactionType == "Export" && t.CustomerId.HasValue)
                    .Select(t => t.CustomerId!.Value)
                    .Distinct()
                    .ToList();
                var listUser = listCustomerId.Any() 
                    ? _userService.GetAll().Where(u => listCustomerId.Contains(u.UserId)).Select(u => new UserDto
                    {
                        UserId = u.UserId,
                        FullName = u.FullName,
                        Phone = u.Phone,
                        Email = u.Email,
                        Image = u.Image
                    }).ToList() 
                    : new List<UserDto>();

                // Lấy danh sách SupplierId (cho Import) - sẽ query từng cái khi cần

                // Map thông tin bổ sung
                foreach (var item in result.Items)
                {
                    // Lấy tên kho
                    if (listWareHouse != null && listWareHouse.Any())
                    {
                        var warehouse = listWareHouse.FirstOrDefault(w => w != null && w.WarehouseId == item.WarehouseId);
                        if (warehouse != null)
                        {
                            item.WarehouseName = warehouse.WarehouseName;
                        }
                    }

                    // Lấy tên khách hàng (nếu là Export)
                    if (item.TransactionType == "Export" && item.CustomerId.HasValue)
                    {
                        var customer = listUser.FirstOrDefault(u => u.UserId == item.CustomerId);
                        if (customer != null)
                        {
                            item.CustomerName = customer.FullName;
                        }
                    }

                    // Lấy tên nhà cung cấp (nếu là Import)
                    if (item.TransactionType == "Import" && item.SupplierId.HasValue)
                    {
                        var supplier = await _supplierService.GetBySupplierId(item.SupplierId.Value);
                        if (supplier != null)
                        {
                            item.SupplierName = supplier.SupplierName;
                        }
                    }
                }

                return Ok(ApiResponse<PagedList<ReturnOrderDto>>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy dữ liệu đơn trả hàng");
                return BadRequest(ApiResponse<PagedList<ReturnOrderDto>>.Fail("Có lỗi xảy ra khi lấy dữ liệu"));
            }
        }

        /// <summary>
        /// Lấy chi tiết đơn trả hàng (dùng chung cho cả Import và Export)
        /// </summary>
        /// <param name="returnTransactionId">ID của đơn trả hàng</param>
        /// <returns>Chi tiết đơn trả hàng bao gồm các sản phẩm</returns>
        [HttpGet("GetDetail/{returnTransactionId}")]
        public async Task<IActionResult> GetDetail(int returnTransactionId)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ", 400));
            }

            if (returnTransactionId <= 0)
            {
                return BadRequest(ApiResponse<object>.Fail("Id không hợp lệ", 400));
            }

            try
            {
                // Lấy ReturnTransaction
                var returnTransaction = await _returnTransactionService.GetByIdAsync(returnTransactionId);
                if (returnTransaction == null)
                {
                    return NotFound(ApiResponse<object>.Fail("Không tìm thấy đơn trả hàng", 404));
                }

                // Lấy Transaction gốc
                var transaction = await _transactionService.GetByTransactionId(returnTransaction.TransactionId);
                if (transaction == null)
                {
                    return NotFound(ApiResponse<object>.Fail("Không tìm thấy đơn hàng gốc", 404));
                }

                // Lấy chi tiết đơn trả hàng
                var returnDetails = await _returnTransactionDetailService.GetByReturnTransactionId(returnTransactionId);

                if (returnDetails == null || !returnDetails.Any())
                {
                    return NotFound(ApiResponse<object>.Fail("Không có chi tiết đơn trả hàng", 404));
                }

                // Tạo response object
                var result = new ReturnOrderDetailDto
                {
                    ReturnTransactionId = returnTransaction.ReturnTransactionId,
                    TransactionId = transaction.TransactionId,
                    TransactionType = transaction.Type,
                    TransactionDate = transaction.TransactionDate ?? DateTime.MinValue,
                    Reason = returnTransaction.Reason,
                    CreatedAt = returnTransaction.CreatedAt ?? DateTime.MinValue,
                    WarehouseName = (await _warehouseService.GetById(transaction.WarehouseId))?.WarehouseName ?? "N/A",
                    Status = transaction.Status
                };

                // Xử lý theo loại transaction (Import hoặc Export)
                if (transaction.Type == "Export")
                {
                    // Lấy thông tin khách hàng
                    if (transaction.CustomerId.HasValue)
                    {
                        var customer = await _userService.GetByIdAsync(transaction.CustomerId.Value);
                        if (customer != null)
                        {
                            result.Customer = new CustomerOutputVM
                            {
                                UserId = customer.UserId,
                                FullName = customer.FullName,
                                Phone = customer.Phone,
                                Email = customer.Email,
                                Image = customer.Image
                            };
                        }
                        else
                        {
                            result.Customer = new CustomerOutputVM
                            {
                                UserId = null,
                                FullName = "N/A",
                                Phone = "N/A",
                                Email = "N/A",
                                Image = "N/A"
                            };
                        }
                    }
                }
                else if (transaction.Type == "Import")
                {
                    // Lấy thông tin nhà cung cấp
                    if (transaction.SupplierId.HasValue)
                    {
                        var supplier = await _supplierService.GetBySupplierId(transaction.SupplierId.Value);
                        if (supplier != null)
                        {
                            result.Supplier = new SupplierOutputVM
                            {
                                SupplierId = supplier.SupplierId,
                                SupplierName = supplier.SupplierName,
                                Email = supplier.Email,
                                Phone = supplier.Phone,
                                Status = supplier.IsActive switch
                                {
                                    false => "Ngừng hoạt động",
                                    true => "Đang hoạt động",
                                    _ => "N/A"
                                }
                            };
                        }
                        else
                        {
                            result.Supplier = new SupplierOutputVM
                            {
                                SupplierId = null,
                                SupplierName = "N/A",
                                Email = "N/A",
                                Phone = "N/A",
                                Status = "N/A"
                            };
                        }
                    }
                }

                // Lấy chi tiết sản phẩm
                var detailList = new List<ReturnOrderDetailItemDto>();
                foreach (var detail in returnDetails)
                {
                    var product = await _productService.GetById(detail.ProductId);
                    detailList.Add(new ReturnOrderDetailItemDto
                    {
                        ReturnTransactionDetailId = detail.Id,
                        ProductId = detail.ProductId,
                        ProductName = product?.ProductName ?? "N/A",
                        Quantity = detail.Quantity
                    });
                }

                result.Items = detailList;

                return Ok(ApiResponse<ReturnOrderDetailDto>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy chi tiết đơn trả hàng");
                return BadRequest(ApiResponse<object>.Fail("Có lỗi xảy ra khi lấy chi tiết đơn trả hàng"));
            }
        }
    }
}

