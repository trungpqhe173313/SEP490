using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NB.Service.Common;
using NB.Service.Dto;
using NB.Service.TransactionService;
using NB.Service.TransactionService.Dto;

namespace NB.API.Controllers
{
    [Route("api/transaction")]
    [ApiController]
    [Authorize]
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionService _transactionService;
        private readonly ILogger<TransactionController> _logger;

        public TransactionController(
            ITransactionService transactionService,
            ILogger<TransactionController> logger)
        {
            _transactionService = transactionService;
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
    }
}
