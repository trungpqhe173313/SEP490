using Microsoft.AspNetCore.Mvc;
using NB.Model.Entities;
using NB.Service.Common;
using NB.Service.Core.Mapper;
using NB.Service.Dto;
using NB.Service.SupplierService;
using NB.Service.SupplierService.Dto;

namespace NB.API.Controllers
{
    [Route("api/supplier")]
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
    }
}
