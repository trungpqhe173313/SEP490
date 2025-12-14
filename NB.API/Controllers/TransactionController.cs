using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NB.Model.Enums;
using NB.Service.Common;
using NB.Service.Core.Enum;
using NB.Service.Dto;
using NB.Service.TransactionService;
using NB.Service.TransactionService.Dto;
using NB.Service.UserService;
using NB.Service.WarehouseService;
using NB.Service.WarehouseService.Dto;

namespace NB.API.Controllers
{
    [Route("api/transaction")]
    [ApiController]
    //[Authorize]
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionService _transactionService;
        private readonly IUserService _userService;
        private readonly IWarehouseService _warehouseService;
        private readonly ILogger<TransactionController> _logger;

        public TransactionController(
            ITransactionService transactionService,
            IUserService userService,
            IWarehouseService warehouseService,
            ILogger<TransactionController> logger)
        {
            _transactionService = transactionService;
            _userService = userService;
            _warehouseService = warehouseService;
            _logger = logger;
        }

        /// <summary>
        /// Lấy chi tiết giao dịch (dùng chung cho Import, Export, Transfer)
        /// - Thông tin transaction: Type, Status, Date, Code, Note
        /// - Thông tin Warehouse, Customer (Export), Supplier (Import)
        /// - Danh sách sản phẩm với số lượng và đơn giá
        /// </summary>
        [HttpGet("{transactionId}")]
        public async Task<IActionResult> GetTransactionById(int transactionId)
        {
            try
            {
                var detail = await _transactionService.GetTransactionDetailById(transactionId);
                return Ok(ApiResponse<TransactionDetailResponseDto>.Ok(detail));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<TransactionDetailResponseDto>.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy chi tiết giao dịch {TransactionId}", transactionId);
                return BadRequest(ApiResponse<TransactionDetailResponseDto>.Fail(ex.Message));
            }
        }

        /// <summary>
        /// Lấy tổng khối lượng ĐÃ NHẬP trong khoảng thời gian
        /// - Tính Status: done(4), checked(8), paidInFull(11), partiallyPaid(12)
        /// - Bao gồm chi tiết theo từng Supplier
        /// </summary>
        [HttpGet("import-weight")]
        public async Task<IActionResult> GetImportWeight(
            [FromQuery] DateTime fromDate,
            [FromQuery] DateTime toDate)
        {
            try
            {
                if (fromDate > toDate)
                {
                    return BadRequest(ApiResponse<ImportWeightSummaryDto>.Fail("fromDate không được lớn hơn toDate"));
                }

                var result = await _transactionService.GetImportWeightAsync(fromDate, toDate);
                return Ok(ApiResponse<ImportWeightSummaryDto>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy khối lượng nhập từ {FromDate} đến {ToDate}", fromDate, toDate);
                return BadRequest(ApiResponse<ImportWeightSummaryDto>.Fail(ex.Message));
            }
        }

        /// <summary>
        /// Lấy tổng khối lượng ĐÃ XUẤT trong khoảng thời gian
        /// - Tính Status: done(4), paidInFull(11), partiallyPaid(12)
        /// - Bao gồm chi tiết theo từng Customer
        /// </summary>
        [HttpGet("export-weight")]
        public async Task<IActionResult> GetExportWeight(
            [FromQuery] DateTime fromDate,
            [FromQuery] DateTime toDate)
        {
            try
            {
                if (fromDate > toDate)
                {
                    return BadRequest(ApiResponse<ExportWeightSummaryDto>.Fail("fromDate không được lớn hơn toDate"));
                }

                var result = await _transactionService.GetExportWeightAsync(fromDate, toDate);
                return Ok(ApiResponse<ExportWeightSummaryDto>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy khối lượng xuất từ {FromDate} đến {ToDate}", fromDate, toDate);
                return BadRequest(ApiResponse<ExportWeightSummaryDto>.Fail(ex.Message));
            }
        }

        /// <summary>
        /// Lấy danh sách giao dịch do người chịu trách nhiệm phụ trách
        /// - Lọc theo ResponsibleId
        /// - Hỗ trợ phân trang và tìm kiếm
        /// </summary>
        [HttpPost("GetData")]
        public async Task<IActionResult> GetData([FromBody] TransactionSearch search)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ", 400));
            }

            try
            {
                var result = await _transactionService.GetDataForExport(search);
                if (result.Items == null || !result.Items.Any())
                {
                    return Ok(ApiResponse<PagedList<TransactionDto>>.Ok(result));
                }

                // Lấy danh sách WarehouseId để query một lần
                var listWarehouseId = result.Items.Select(t => t.WarehouseId).ToList();
                var listWarehouseInId = result.Items.Where(t => t.WarehouseInId.HasValue).Select(t => t.WarehouseInId!.Value).ToList();
                var allWarehouseIds = listWarehouseId.Concat(listWarehouseInId).Distinct().ToList();

                var listWareHouse = await _warehouseService.GetByListWarehouseId(allWarehouseIds);

                if (listWareHouse == null || !listWareHouse.Any())
                {
                    return NotFound(ApiResponse<PagedList<WarehouseDto>>.Fail("Không tìm thấy kho", 404));
                }

                // Lấy danh sách ResponsibleId để query user một lần
                var listResponsibleId = result.Items
                    .Where(t => t.ResponsibleId.HasValue && t.ResponsibleId.Value > 0)
                    .Select(t => t.ResponsibleId!.Value)
                    .Distinct()
                    .ToList();

                var responsibleDict = new Dictionary<int, string>();
                if (listResponsibleId.Any())
                {
                    var responsibleUsers = _userService.GetQueryable()
                        .Where(u => listResponsibleId.Contains(u.UserId))
                        .ToList();

                    foreach (var user in responsibleUsers)
                    {
                        responsibleDict[user.UserId] = user.FullName ?? user.Username ?? "N/A";
                    }
                }

                foreach (var t in result.Items)
                {
                    // Lấy tên kho
                    var warehouse = listWareHouse?.FirstOrDefault(w => w != null && w.WarehouseId == t.WarehouseId);
                    if (warehouse != null)
                    {
                        t.WarehouseName = warehouse.WarehouseName;
                    }

                    // Lấy tên kho đích (nếu có - dùng cho Transfer)
                    if (t.WarehouseInId.HasValue)
                    {
                        var warehouseIn = listWareHouse?.FirstOrDefault(w => w != null && w.WarehouseId == t.WarehouseInId.Value);
                        if (warehouseIn != null)
                        {
                            t.WarehouseInName = warehouseIn.WarehouseName;
                        }
                    }

                    // Gắn tên người chịu trách nhiệm
                    if (t.ResponsibleId.HasValue && responsibleDict.ContainsKey(t.ResponsibleId.Value))
                    {
                        t.ResponsibleName = responsibleDict[t.ResponsibleId.Value];
                    }

                    // Gắn statusName cho transaction
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
                _logger.LogError(ex, "Lỗi khi lấy dữ liệu giao dịch");
                return BadRequest(ApiResponse<PagedList<TransactionDto>>.Fail("Có lỗi xảy ra khi lấy dữ liệu"));
            }
        }

        /// <summary>
        /// Lấy chi tiết giao dịch do người chịu trách nhiệm phụ trách
        /// - Thông tin transaction: Type, Status, Date, Code, Note
        /// - Thông tin Warehouse, Customer (Export), Supplier (Import)
        /// - Thông tin người chịu trách nhiệm
        /// - Danh sách sản phẩm với số lượng và đơn giá
        /// </summary>
        [HttpGet("GetDetail/{transactionId}")]
        public async Task<IActionResult> GetDetail(int transactionId)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ", 400));
            }

            if (transactionId <= 0)
            {
                return BadRequest(ApiResponse<TransactionDetailResponseDto>.Fail("TransactionId không hợp lệ", 400));
            }

            try
            {
                var detail = await _transactionService.GetTransactionDetailById(transactionId);
                if (detail == null)
                {
                    return NotFound(ApiResponse<TransactionDetailResponseDto>.Fail("Không tìm thấy giao dịch", 404));
                }

                return Ok(ApiResponse<TransactionDetailResponseDto>.Ok(detail));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<TransactionDetailResponseDto>.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy chi tiết giao dịch {TransactionId}", transactionId);
                return BadRequest(ApiResponse<TransactionDetailResponseDto>.Fail("Có lỗi xảy ra khi lấy dữ liệu"));
            }
        }

        /// <summary>
        /// Cập nhật người chịu trách nhiệm cho transaction
        /// - Chỉ cập nhật nếu có sự thay đổi
        /// - Kiểm tra transaction và user tồn tại
        /// </summary>
        [HttpPut("UpdateResponsible/{transactionId}")]
        public async Task<IActionResult> UpdateResponsible(int transactionId, [FromBody] UpdateTransactionResponsibleRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ", 400));
            }

            if (transactionId <= 0)
            {
                return BadRequest(ApiResponse<object>.Fail("TransactionId không hợp lệ", 400));
            }

            try
            {
                // Lấy transaction entity
                var transaction = await _transactionService.GetByIdAsync(transactionId);
                if (transaction == null)
                {
                    return NotFound(ApiResponse<object>.Fail("Không tìm thấy giao dịch", 404));
                }

                // Kiểm tra nếu ResponsibleId có giá trị thì phải kiểm tra user tồn tại
                if (request.ResponsibleId.HasValue && request.ResponsibleId.Value > 0)
                {
                    var responsibleUser = await _userService.GetByUserId(request.ResponsibleId.Value);
                    if (responsibleUser == null)
                    {
                        return NotFound(ApiResponse<object>.Fail("Không tìm thấy người chịu trách nhiệm với ID này", 404));
                    }
                }

                // Kiểm tra xem có sự thay đổi không
                if (transaction.ResponsibleId == request.ResponsibleId)
                {
                    return Ok(ApiResponse<string>.Ok("Không có sự thay đổi về người chịu trách nhiệm"));
                }

                // Cập nhật ResponsibleId
                transaction.ResponsibleId = request.ResponsibleId;
                await _transactionService.UpdateAsync(transaction);

                return Ok(ApiResponse<string>.Ok("Cập nhật người chịu trách nhiệm thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật người chịu trách nhiệm cho transaction {TransactionId}", transactionId);
                return BadRequest(ApiResponse<object>.Fail("Có lỗi xảy ra khi cập nhật người chịu trách nhiệm"));
            }
        }
    }
}
