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
    }
}
