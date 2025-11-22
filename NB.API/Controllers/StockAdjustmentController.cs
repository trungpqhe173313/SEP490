using Microsoft.AspNetCore.Mvc;
using NB.Service.Common;
using NB.Service.Dto;
using NB.Service.StockAdjustmentService;
using NB.Service.StockAdjustmentService.ViewModels;

namespace NB.API.Controllers
{
    [Route("api/stock-adjustment")]
    public class StockAdjustmentController : Controller
    {
        private readonly IStockAdjustmentService _stockAdjustmentService;
        private readonly ILogger<StockAdjustmentController> _logger;

        public StockAdjustmentController(
            IStockAdjustmentService stockAdjustmentService,
            ILogger<StockAdjustmentController> logger)
        {
            _stockAdjustmentService = stockAdjustmentService;
            _logger = logger;
        }

        /// <summary>
        /// Tạo phiếu kiểm kho nháp (Draft)
        /// Người dùng đếm hàng và nhập ActualQuantity
        /// Chỉ lưu ProductId và ActualQuantity, không tính toán Diff, không thay đổi tồn kho
        /// </summary>
        [HttpPost("draft")]
        public async Task<IActionResult> CreateDraft([FromBody] StockAdjustmentDraftCreateVM model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse<StockAdjustmentDraftResponseVM>.Fail(string.Join(", ", errors)));
                }

                var result = await _stockAdjustmentService.CreateDraftAsync(model);
                return Ok(ApiResponse<StockAdjustmentDraftResponseVM>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo phiếu kiểm kho nháp");
                return BadRequest(ApiResponse<StockAdjustmentDraftResponseVM>.Fail(ex.Message));
            }
        }
    }
}

