using Microsoft.AspNetCore.Mvc;
using NB.Service.Common;
using NB.Service.Dto;
using NB.Service.StockAdjustmentService;
using NB.Service.StockAdjustmentService.Dto;
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
        /// Lấy danh sách phiếu kiểm kho (có phân trang)
        /// </summary>
        [HttpPost("GetData")]
        public async Task<IActionResult> GetData([FromBody] StockAdjustmentSearch search)
        {
            try
            {
                var result = await _stockAdjustmentService.GetPagedListAsync(search);
                return Ok(ApiResponse<PagedList<StockAdjustmentListDto>>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách phiếu kiểm kho");
                return BadRequest(ApiResponse<PagedList<StockAdjustmentListDto>>.Fail("Có lỗi xảy ra"));
            }
        }

        /// <summary>
        /// Tạo phiếu kiểm kho nháp (Draft)
        /// Người dùng đếm hàng và nhập ActualQuantity
        /// Chỉ lưu ProductId và ActualQuantity, không lưu SystemQuantity vào DB
        /// SystemQuantity sẽ được lấy REALTIME từ Inventory khi GET
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

        /// <summary>
        /// Lấy phiếu kiểm kho theo ID
        /// 
        /// Logic SystemQuantity:
        ///   - Nếu Status = Draft: SystemQuantity lấy REALTIME từ Inventory (động)
        ///   - Nếu Status = Resolved: SystemQuantity lấy từ DB (đã lưu khi resolve - cố định)
        /// 
        /// Difference = ActualQuantity - SystemQuantity
        /// </summary>
        [HttpGet("Adjustment/{id}")]
        public async Task<IActionResult> GetAdjustmentById(int id)
        {
            try
            {
                var result = await _stockAdjustmentService.GetDraftByIdAsync(id);
                return Ok(ApiResponse<StockAdjustmentDraftResponseVM>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy phiếu kiểm kho nháp với Id: {Id}", id);
                return BadRequest(ApiResponse<StockAdjustmentDraftResponseVM>.Fail(ex.Message));
            }
        }

        /// <summary>
        /// Cập nhật phiếu kiểm kho nháp
        /// 
        /// Chức năng:
        ///   - Cập nhật ActualQuantity của sản phẩm đã có (DetailId > 0)
        ///   - Thêm sản phẩm mới vào phiếu (DetailId = 0)
        ///   - Xóa sản phẩm khỏi phiếu (IsDeleted = true)
        /// 
        /// Chỉ cho phép update khi Status = Draft
        /// </summary>
        [HttpPut("draft/{id}")]
        public async Task<IActionResult> UpdateDraft(int id, [FromBody] StockAdjustmentDraftUpdateVM model)
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

                var result = await _stockAdjustmentService.UpdateDraftAsync(id, model);
                return Ok(ApiResponse<StockAdjustmentDraftResponseVM>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật phiếu kiểm kho nháp với Id: {Id}", id);
                return BadRequest(ApiResponse<StockAdjustmentDraftResponseVM>.Fail(ex.Message));
            }
        }

        /// <summary>
        /// XÁC NHẬN KIỂM KHO (RESOLVE) - Tác động lên tồn kho thực tế
        /// 
        /// Step 1: Lấy SystemQuantity REALTIME từ Inventory
        /// Step 2: LƯU SystemQuantity vào StockAdjustmentDetail (để audit trail)
        /// Step 3: So sánh Actual vs System:
        ///   - Actual > System → TỒN THIẾU → NHẬP (tăng Inventory, tạo StockBatch mới)
        ///   - Actual < System → THỪA → XUẤT (giảm Inventory, áp dụng FIFO trên StockBatch)
        ///   - Actual = System → KHÔNG LÀM GÌ
        /// Step 4: Update Draft Status → Resolved (không cho sửa lại)
        /// 
        /// Sau khi resolve, SystemQuantity được lưu vĩnh viễn trong DB để xem lại sau này.
        /// </summary>
        [HttpPost("{id}/resolve")]
        public async Task<IActionResult> Resolve(int id)
        {
            try
            {
                var result = await _stockAdjustmentService.ResolveAsync(id);
                return Ok(ApiResponse<StockAdjustmentDraftResponseVM>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xác nhận kiểm kho với Id: {Id}", id);
                return BadRequest(ApiResponse<StockAdjustmentDraftResponseVM>.Fail(ex.Message));
            }
        }
    }
}

