using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NB.Model.Entities;
using NB.Model.Enums;
using NB.Service.Common;
using NB.Service.Core.Enum;
using NB.Service.Core.Mapper;
using NB.Service.Dto;
using NB.Service.FinancialTransactionService;
using NB.Service.FinancialTransactionService.Dto;
using NB.Service.FinancialTransactionService.ViewModels;
using NB.Service.TransactionService;
using NB.Service.UserService;

namespace NB.API.Controllers
{
    [Route("api/financialtransaction")]
    [Authorize]
    public class FinancialTransactionController : Controller
    {
        private readonly ITransactionService _transactionService;
        private readonly IFinancialTransactionService _financialTransactionService;
        private readonly IUserService _userService;
        private readonly IMapper _mapper;
        private readonly ILogger<FinancialTransactionController> _logger;

        public FinancialTransactionController(
            ITransactionService transactionService,
            IFinancialTransactionService financialTransactionService,
            IUserService userService,
            IMapper mapper,
            ILogger<FinancialTransactionController> logger)
        {
            _transactionService = transactionService;
            _financialTransactionService = financialTransactionService;
            _userService = userService;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpPost("CreateFinancialTransaction")]
        public async Task<IActionResult> CreateFinancialTransaction(FinancialTransactionCreateVM model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<FinancialTransactionCreateVM>.Fail("Dữ liệu không hợp lệ"));
            }
            if (!model.Amount.HasValue)
            {
                return BadRequest(ApiResponse<FinancialTransactionCreateVM>.Fail("Số tiền thu chi không được để trống"));
            }
            // Kiểm tra Type phải là ThuKhac HOẶC ChiKhac
            if (model.Type != (int)FinancialTransactionType.ThuKhac && model.Type != (int)FinancialTransactionType.ChiKhac)
            {
                return BadRequest(ApiResponse<FinancialTransactionCreateVM>.Fail("Kiểu thu chi không hợp lệ"));
            }
            try
            {
                var entity = _mapper.Map<FinancialTransactionCreateVM, FinancialTransaction>(model);
                entity.TransactionDate = DateTime.Now;
                if (model.Type == (int) FinancialTransactionType.ChiKhac)
                {
                    // Chi tiền: amount là số âm
                    entity.Amount = -(model.Amount ?? 0);
                    entity.Type = FinancialTransactionType.ChiKhac.ToString();
                }
                else
                {
                    // Thu tiền: amount là số dương
                    entity.Amount = model.Amount ?? 0;
                    entity.Type = FinancialTransactionType.ThuKhac.ToString();
                }

                await _financialTransactionService.CreateAsync(entity);
                return Ok(ApiResponse<string>.Ok("Tạo khoản thu chi thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Có lỗi xảy ra khi tạo khoản thu chi");
                return BadRequest(ApiResponse<string>.Fail("Có lỗi xảy ra khi tạo khoản thu chi"));
            }

        }

        /// <summary>
        /// Lấy danh sách giao dịch tài chính với phân trang và tìm kiếm
        /// </summary>
        /// <param name="search">Điều kiện tìm kiếm và phân trang</param>
        /// <returns>Danh sách giao dịch tài chính thỏa mãn điều kiện</returns>
        [HttpPost("GetData")]
        public async Task<IActionResult> GetData([FromBody] FinancialTransactionSearch search)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ", 400));
            }

            try
            {
                var result = await _financialTransactionService.GetData(search);
                
                // Kiểm tra Items null hoặc empty để tránh NullReferenceException
                if (result.Items == null || !result.Items.Any())
                {
                    return Ok(ApiResponse<PagedList<FinancialTransactionDto>>.Ok(result));
                }
                
                // Lấy danh sách CreatedBy để query user một lần (tối ưu performance)
                var listCreatedById = result.Items
                    .Where(ft => ft.CreatedBy.HasValue && ft.CreatedBy.Value > 0)
                    .Select(ft => ft.CreatedBy!.Value)
                    .Distinct()
                    .ToList();

                // Query tất cả user một lần (tối ưu performance)
                var userDict = new Dictionary<int, string>();
                if (listCreatedById.Any())
                {
                    var users = _userService.GetQueryable()
                        .Where(u => listCreatedById.Contains(u.UserId))
                        .ToList();
                    
                    foreach (var user in users)
                    {
                        userDict[user.UserId] = user.FullName ?? user.Username ?? "N/A";
                    }
                }

                // Enrich thông tin: thêm TypeName, TypeInt từ enum và CreatedByName
                foreach (var ft in result.Items)
                {
                    // Thêm TypeName và TypeInt từ enum
                    if (!string.IsNullOrEmpty(ft.Type))
                    {
                        if (Enum.TryParse<FinancialTransactionType>(ft.Type, out var typeEnum))
                        {
                            ft.TypeName = typeEnum.GetDescription();
                            ft.TypeInt = (int)typeEnum;
                        }
                    }

                    // Thêm CreatedByName
                    if (ft.CreatedBy.HasValue && ft.CreatedBy.Value > 0)
                    {
                        if (userDict.ContainsKey(ft.CreatedBy.Value))
                        {
                            ft.CreatedByName = userDict[ft.CreatedBy.Value];
                        }
                    }
                }

                return Ok(ApiResponse<PagedList<FinancialTransactionDto>>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách giao dịch tài chính");
                return BadRequest(ApiResponse<PagedList<FinancialTransactionDto>>.Fail("Có lỗi xảy ra khi lấy dữ liệu"));
            }
        }

        /// <summary>
        /// Lấy chi tiết giao dịch tài chính theo ID
        /// </summary>
        /// <param name="Id">FinancialTransactionId</param>
        /// <returns>Chi tiết giao dịch tài chính</returns>
        [HttpGet("GetDetail/{Id}")]
        public async Task<IActionResult> GetDetail(int Id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ", 400));
            }

            if (Id <= 0)
            {
                return BadRequest(ApiResponse<FinancialTransactionDto>.Fail("Id không hợp lệ", 400));
            }

            try
            {
                var result = await _financialTransactionService.GetByIdAsync(Id);
                if (result == null)
                {
                    return NotFound(ApiResponse<FinancialTransactionDto>.Fail("Không tìm thấy giao dịch tài chính", 404));
                }

                // Enrich thông tin: thêm TypeName và TypeInt từ enum
                if (!string.IsNullOrEmpty(result.Type))
                {
                    if (Enum.TryParse<FinancialTransactionType>(result.Type, out var typeEnum))
                    {
                        result.TypeName = typeEnum.GetDescription();
                        result.TypeInt = (int)typeEnum;
                    }
                }

                // Lấy tên người tạo nếu có CreatedBy
                if (result.CreatedBy.HasValue && result.CreatedBy.Value > 0)
                {
                    var creator = await _userService.GetByUserId(result.CreatedBy.Value);
                    if (creator != null)
                    {
                        result.CreatedByName = creator.FullName ?? creator.Username ?? "N/A";
                    }
                }

                return Ok(ApiResponse<FinancialTransactionDto>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy chi tiết giao dịch tài chính với Id: {Id}", Id);
                return BadRequest(ApiResponse<FinancialTransactionDto>.Fail("Có lỗi xảy ra khi lấy dữ liệu"));
            }
        }

        /// <summary>
        /// Cập nhật giao dịch tài chính
        /// </summary>
        /// <param name="Id">FinancialTransactionId</param>
        /// <param name="model">Thông tin cập nhật</param>
        /// <returns>Kết quả cập nhật</returns>
        [HttpPut("UpdateFinancialTransaction/{Id}")]
        public async Task<IActionResult> UpdateFinancialTransaction(int Id, [FromBody] FinancialTransactionUpdateVM model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<FinancialTransactionUpdateVM>.Fail("Dữ liệu không hợp lệ", 400));
            }

            if (Id <= 0)
            {
                return BadRequest(ApiResponse<FinancialTransactionUpdateVM>.Fail("Id không hợp lệ", 400));
            }

            if (!model.Amount.HasValue)
            {
                return BadRequest(ApiResponse<FinancialTransactionUpdateVM>.Fail("Số tiền thu chi không được để trống"));
            }

            try
            {
                // Lấy entity từ database (sử dụng GetEntityByIdAsync để entity được track)
                var entity = await _financialTransactionService.GetByIdAsync(Id);
                
                if (entity == null)
                {
                    return NotFound(ApiResponse<FinancialTransactionUpdateVM>.Fail("Không tìm thấy giao dịch tài chính", 404));
                }

                // Xác định Type mới (nếu có) hoặc giữ nguyên Type cũ
                string newType = entity.Type;
                if (model.Type.HasValue)
                {
                    // Validate Type
                    if (model.Type != (int)FinancialTransactionType.ThuKhac && 
                        model.Type != (int)FinancialTransactionType.ChiKhac &&
                        model.Type != (int)FinancialTransactionType.UngLuong &&
                        model.Type != (int)FinancialTransactionType.ThanhToanLuong &&
                        model.Type != (int)FinancialTransactionType.ThuTienKhach &&
                        model.Type != (int)FinancialTransactionType.ThanhToanNhanHang)
                    {
                        return BadRequest(ApiResponse<FinancialTransactionUpdateVM>.Fail("Kiểu thu chi không hợp lệ"));
                    }

                    // Set Type mới
                    if (model.Type == (int)FinancialTransactionType.ChiKhac)
                    {
                        newType = FinancialTransactionType.ChiKhac.ToString();
                    }
                    else if (model.Type == (int)FinancialTransactionType.ThuKhac)
                    {
                        newType = FinancialTransactionType.ThuKhac.ToString();
                    }
                    else if (model.Type == (int)FinancialTransactionType.UngLuong)
                    {
                        newType = FinancialTransactionType.UngLuong.ToString();
                    }
                    else if (model.Type == (int)FinancialTransactionType.ThanhToanLuong)
                    {
                        newType = FinancialTransactionType.ThanhToanLuong.ToString();
                    }
                    else if (model.Type == (int)FinancialTransactionType.ThuTienKhach)
                    {
                        newType = FinancialTransactionType.ThuTienKhach.ToString();
                    }
                    else if (model.Type == (int)FinancialTransactionType.ThanhToanNhanHang)
                    {
                        newType = FinancialTransactionType.ThanhToanNhanHang.ToString();
                    }
                }

                // Cập nhật thông tin
                if (!string.IsNullOrEmpty(model.Description))
                {
                    entity.Description = model.Description;
                }
                if (!string.IsNullOrEmpty(model.PaymentMethod))
                {
                    entity.PaymentMethod = model.PaymentMethod;
                }
                    entity.Type = newType;

                // Xử lý Amount theo Type
                if (newType == FinancialTransactionType.ChiKhac.ToString() ||
                    newType == FinancialTransactionType.UngLuong.ToString() ||
                    newType == FinancialTransactionType.ThanhToanLuong.ToString() ||
                    newType == FinancialTransactionType.ThanhToanNhanHang.ToString())
                {
                    // Chi tiền: amount là số âm
                    entity.Amount = -(model.Amount ?? 0);
                }
                else
                {
                    // Thu tiền: amount là số dương
                    entity.Amount = model.Amount ?? 0;
                }

                await _financialTransactionService.UpdateAsync(entity);
                return Ok(ApiResponse<string>.Ok("Cập nhật giao dịch tài chính thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật giao dịch tài chính với Id: {Id}", Id);
                return BadRequest(ApiResponse<string>.Fail("Có lỗi xảy ra khi cập nhật giao dịch tài chính"));
            }
        }

        /// <summary>
        /// Xóa giao dịch tài chính
        /// </summary>
        /// <param name="Id">FinancialTransactionId</param>
        /// <returns>Kết quả xóa</returns>
        [HttpDelete("DeleteFinancialTransaction/{Id}")]
        public async Task<IActionResult> DeleteFinancialTransaction(int Id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ", 400));
            }

            if (Id <= 0)
            {
                return BadRequest(ApiResponse<object>.Fail("Id không hợp lệ", 400));
            }

            try
            {
                // Lấy entity từ database
                var entity = await _financialTransactionService.GetByIdAsync(Id);
                if (entity == null)
                {
                    return NotFound(ApiResponse<object>.Fail("Không tìm thấy giao dịch tài chính", 404));
                }

                await _financialTransactionService.DeleteAsync(entity);
                return Ok(ApiResponse<string>.Ok("Xóa giao dịch tài chính thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa giao dịch tài chính với Id: {Id}", Id);
                return BadRequest(ApiResponse<string>.Fail("Có lỗi xảy ra khi xóa giao dịch tài chính"));
            }
        }
    }
}
