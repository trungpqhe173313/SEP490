using Microsoft.AspNetCore.Mvc;
using NB.Model.Entities;
using NB.Service.Common;
using NB.Service.Core.Mapper;
using NB.Service.Dto;
using NB.Service.SupplierService;
using NB.Service.SupplierService.Dto;
using NB.Service.SupplierService.ViewModels;

namespace NB.API.Controllers
{
    [Route("api/suppliers")]
    public class SupplierController : Controller
    {
        private readonly ISupplierService _supplierService;
        private readonly IMapper _mapper;
        private readonly ILogger<Supplier> _logger;

        public SupplierController(
            ISupplierService supplierService,
            IMapper mapper,
            ILogger<Supplier> logger)
        {
            _supplierService = supplierService;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpPost("GetData")]
        public async Task<IActionResult> GetData([FromBody] SupplierSearch search)
        {
            try
            {
                var result = await _supplierService.GetData(search);
                return Ok(ApiResponse<PagedList<SupplierDto>>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy dữ liệu nhà cung cấp");
                return BadRequest(ApiResponse<PagedList<SupplierDto>>.Fail("Có lỗi xảy ra khi lấy dữ liệu"));
            }
        }

        [HttpGet("GetBySupplierId/{id}")]
        public async Task<IActionResult> GetBySupplierId(int id)
        {
            try
            {
                var result = await _supplierService.GetBySupplierId(id);
                if (result == null)
                {
                    return NotFound(ApiResponse<SupplierDto>.Fail("Không tìm thấy nhà cung cấp", 404));
                }
                return Ok(ApiResponse<SupplierDto>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy dữ liệu nhà cung cấp với Id: {Id}", id);
                return BadRequest(ApiResponse<SupplierDto>.Fail("Có lỗi xảy ra"));
            }
        }

        [HttpPost("CreateSupplier")]
        public async Task<IActionResult> CreateSupplier([FromBody] SupplierCreateVM model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<Supplier>.Fail("Dữ liệu không hợp lệ"));
            }
            try
            {
                var exsitingEmail = await _supplierService.GetByEmail(model.Email);
                if (exsitingEmail != null)
                {
                    return BadRequest(ApiResponse<Supplier>.Fail("Email nhà cung cấp đã tồn tại"));
                }

                var entity = _mapper.Map<SupplierCreateVM, Supplier>(model);
                entity.IsActive = true;
                entity.CreatedAt = DateTime.Now;

                await _supplierService.CreateAsync(entity);
                return Ok(ApiResponse<Supplier>.Ok(entity));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo nhà cung cấp");
                return BadRequest(ApiResponse<Supplier>.Fail("Có lỗi xảy ra khi tạo nhà cung cấp"));
            }
        }

        [HttpPut("UpdateSupplier/{id}")]
        public async Task<IActionResult> UpdateSupplier(int id, [FromBody] SupplierEditVM model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<Supplier>.Fail("Dữ liệu không hợp lệ"));
            }
            try
            {
                var existingSupplier = await _supplierService.GetBySupplierId(id);
                if (existingSupplier == null)
                {
                    return NotFound(ApiResponse<Supplier>.Fail("Không tìm thấy nhà cung cấp", 404));
                }
                // Kiểm tra email có bị trùng không
                if (!string.Equals(existingSupplier.Email, model.Email, StringComparison.OrdinalIgnoreCase))
                {
                    var exsitingEmail = await _supplierService.GetByEmail(model.Email);
                    if (exsitingEmail != null)
                    {
                        return BadRequest(ApiResponse<Supplier>.Fail("Email nhà cung cấp đã tồn tại"));
                    }
                }
                var entity = _mapper.Map<SupplierEditVM, Supplier>(model);
                entity.SupplierId = id;
                await _supplierService.UpdateAsync(entity);
                return Ok(ApiResponse<Supplier>.Ok(entity));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật nhà cung cấp với Id: {Id}", id);
                return BadRequest(ApiResponse<Supplier>.Fail("Có lỗi xảy ra khi cập nhật nhà cung cấp"));
            }
        }

        [HttpDelete("DeleteSupplier/{id}")]
        public async Task<IActionResult> DeleteSupplier(int id)
        {
            try
            {
                var entity = await _supplierService.GetByIdAsync(id);
                if (entity == null)
                {
                    return NotFound(ApiResponse<object>.Fail("Không tìm thấy nhà cung cấp"));
                }
                entity.IsActive = false;
                await _supplierService.UpdateAsync(entity);
                return Ok(ApiResponse<bool>.Ok(true));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa nhà cung cấp với ID: {id}", id);
                return BadRequest(ApiResponse<object>.Fail("Có lỗi xảy ra khi xóa nhà cung cấp"));
            }
        }
    }
}
