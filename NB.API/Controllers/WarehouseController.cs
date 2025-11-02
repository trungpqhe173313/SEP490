using Microsoft.AspNetCore.Mvc;
using NB.Model.Entities;
using NB.Service.Common;
using NB.Service.Core.Forms;
using NB.Service.Dto;
using NB.Service.WarehouseService;
using NB.Service.WarehouseService.Dto;
using NB.Service.WarehouseService.ViewModels;
using NB.Services.WarehouseService.ViewModels;

namespace NB.API.Controllers
{
    [Route("api/warehouses")]
    public class WarehouseController : Controller
    {
        private readonly IWarehouseService _warehouseService;
        private readonly ILogger<WarehouseController> _logger;

        public WarehouseController(
            IWarehouseService warehouseService,
            ILogger<WarehouseController> logger)
        {
            _warehouseService = warehouseService;
            _logger = logger;
        }

        [HttpPost("GetData")]
        public async Task<IActionResult> GetData([FromBody] WarehouseSearch search) 
         {
             try
             {
                var result = await _warehouseService.GetData(search);
                return Ok(ApiResponse<PagedList<WarehouseDto>>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách kho");
                 return BadRequest(ApiResponse<PagedList<WarehouseDto>>.Fail("Có lỗi xảy ra"));
             }
         }


        [HttpGet("GetWarehouseById/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
 
                var result = await _warehouseService.GetById(id);
                if (result == null)
                {
                    return NotFound(ApiResponse<WarehouseDto>.Fail("Không tìm thấy kho", 404));
                }
                return Ok(ApiResponse<WarehouseDto>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy kho với Id: {Id}", id);
                return BadRequest(ApiResponse<WarehouseDto>.Fail("Có lỗi xảy ra"));
            }
        }
        /* Disabled*/
        //[HttpPost("CreateWarehouse")]
        //public async Task<IActionResult> Create([FromBody] WarehouseCreateVM model)
        //{
        //    try
        //    {
        //        var warehouse = new WarehouseDto
        //        {
        //            WarehouseName = model.WarehouseName,
        //            Location = model.Location,
        //            Capacity = model.Capacity,
        //            Status = model.Status,
        //            Note = model.Note,
        //            CreatedAt = DateTime.UtcNow
        //        };

        //        await _warehouseService.CreateAsync(warehouse); 

        //        return Ok(ApiResponse<WarehouseDto>.Ok(warehouse));
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Lỗi khi tạo kho mới");
        //        return BadRequest(ApiResponse<WarehouseDto>.Fail("Có lỗi xảy ra khi tạo kho"));
        //    }
        //}

        [HttpPut("UpdateWarehouse/{id}")]
        public async Task<IActionResult> Update(int id,[FromBody] WarehouseUpdateVM model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ"));
            }

            try
            {
                var entity = await _warehouseService.GetByIdAsync(id);

                if (entity == null)
                {
                    // Trả về Not Found nếu không tìm thấy kho
                    return NotFound(ApiResponse<object>.Fail($"Không tìm thấy kho hàng với ID: {id}"));
                }

                entity.WarehouseName = model.WarehouseName;
                entity.Location = model.Location;
                entity.Capacity = model.Capacity;
                entity.Status = model.Status;
                entity.Note = model.Note;

                await _warehouseService.UpdateAsync(entity);
                return Ok(ApiResponse<Warehouse>.Ok(entity));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật kho với Id: {Id}", model);
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
        }

        [HttpDelete("DeleteWarehouse/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                // Lấy entity theo Id
                var warehouseToDelete = await _warehouseService.GetByIdAsync(id);
                if (warehouseToDelete == null)
                {
                    return NotFound(ApiResponse<bool>.Fail("Không tìm thấy kho để xóa", 404));
                }

                // Gọi phương thức DeleteAsync của service
                await _warehouseService.DeleteAsync(warehouseToDelete);

                return Ok(ApiResponse<bool>.Ok(true));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa kho với Id: {Id}", id);
                return BadRequest(ApiResponse<bool>.Fail(ex.Message));
            }
        }

        [HttpPost("ImportFromExcel")]
        public async Task<IActionResult> ImportFromExcel(IFormFile file)
        {
            try
            {
                // Validate file
                if (file == null || file.Length == 0)
                {
                    return BadRequest(ApiResponse<WarehouseImportResultVM>.Fail("File không được để trống"));
                }

                // Validate file extension
                var allowedExtensions = new[] { ".xlsx", ".xls" };
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest(ApiResponse<WarehouseImportResultVM>.Fail("Chỉ chấp nhận file Excel (.xlsx, .xls)"));
                }

                // Validate file size ( Tối đa 10MB)
                if (file.Length > 10 * 1024 * 1024)
                {
                    return BadRequest(ApiResponse<WarehouseImportResultVM>.Fail("Kích thước file không được vượt quá 10MB"));
                }

                // Process Excel file
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    stream.Position = 0;

                    var result = await _warehouseService.ImportFromExcelAsync(stream);

                    if (result.SuccessCount == 0 && result.FailedCount > 0)
                    {
                        return BadRequest(ApiResponse<WarehouseImportResultVM>.Fail(
                            result.ErrorMessages.Any()
                            ? result.ErrorMessages
                            : new List<string> { "Không có bản ghi nào được nhập thành công." }, 400));
                    }

                    return Ok(ApiResponse<WarehouseImportResultVM>.Ok(result));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi import file Excel");
                return BadRequest(ApiResponse<WarehouseImportResultVM>.Fail($"Có lỗi xảy ra: {ex.Message}"));
            }
        }

        [HttpGet("DownloadTemplate")]
        public IActionResult DownloadTemplate()
        {
            try
            {
                var stream = ExcelTemplateGenerator.GenerateWarehouseTemplate();
                var fileName = $"Warehouse_Import_Template_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                return File(
                    stream,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo file template");
                return BadRequest(ApiResponse<object>.Fail($"Có lỗi xảy ra khi tạo template: {ex.Message}"));
            }
        }
    }
}