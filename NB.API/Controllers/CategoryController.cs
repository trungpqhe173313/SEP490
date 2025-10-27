using Microsoft.AspNetCore.Mvc;
using NB.Service.CategoryService;
using NB.Service.CategoryService.Dto;
using NB.Service.Common;
using NB.Service.Dto;

namespace NB.API.Controllers
{
    [Route("api/categories")]
    public class CategoryController : Controller
    {
        private readonly ICategoryService _categoryService;
        private readonly ILogger<CategoryController> _logger;
        public CategoryController(
            ICategoryService categoryService,
            ILogger<CategoryController> logger)
        {
            _categoryService = categoryService;
            _logger = logger;
        }

        [HttpPost("GetData")]
        public async Task<IActionResult> GetData([FromBody] CategorySearch search)
        {
            try
            {
                var categoryList = await _categoryService.GetData();

                
                var filteredCategories = string.IsNullOrEmpty(search.CategoryName)
                    ? categoryList
                    : categoryList
                        .Where(c => c.CategoryName != null &&
                                   c.CategoryName.Contains(search.CategoryName, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                
                var pagedResult = PagedList<CategoryDto?>.CreateFromList(filteredCategories, search);
                foreach(var category in pagedResult.Items)
                {
                    category.IsActive = category.IsActive;
                }

                return Ok(ApiResponse<PagedList<CategoryDto?>>.Ok(pagedResult));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách danh mục");
                return BadRequest(ApiResponse<CategoryDto>.Fail("Có lỗi xảy ra khi lấy danh sách danh mục."));
            }
        }

        [HttpGet("GetById/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ"));
            }
            try
            {
                var category = await _categoryService.GetById(id);
                if (category == null)
                {
                    return NotFound(ApiResponse<object>.Fail($"Không tìm thấy danh mục với ID: {id}", 404));
                }
                return Ok(ApiResponse<CategoryDto?>.Ok(category));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy danh mục với ID: {id}");
                return BadRequest(ApiResponse<CategoryDto>.Fail("Có lỗi xảy ra khi lấy danh mục."));
            }
        }

        [HttpPost("CreateCategory")]
        public async Task<IActionResult> CreateCategory([FromBody] CategoryCreateVM model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ"));
            }
            var IsCategoryExist = await _categoryService.GetByName(model.CategoryName);
            if (!(IsCategoryExist == null))
            {
                return BadRequest(ApiResponse<object>.Fail("Tên danh mục đã tồn tại"));
            }
            try
            {
                CategoryDto newCategory = new CategoryDto
                {
                    CategoryName = model.CategoryName,
                    Description = model.Description,
                    CreatedAt = model.CreatedAt
                };
                await _categoryService.CreateAsync(newCategory);
                return Ok(ApiResponse<CategoryDto>.Ok(newCategory));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo danh mục mới");
                return BadRequest(ApiResponse<CategoryDto>.Fail("Có lỗi xảy ra khi tạo danh mục."));
            }
        }

        [HttpPut("UpdateCategory/{Id}")]
        public async Task<IActionResult> UpdateCategory(int Id,[FromBody] CategoryUpdateVM model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ"));
            }
            try
            {
                
                var category = await _categoryService.GetById(Id);
                if(category == null)
                {
                    return BadRequest(ApiResponse<object>.Fail("Không tồn tại danh mục với Id này"));
                }
                category.CategoryName = model.CategoryName;
                category.Description = model.Description;
                category.IsActive = model.IsActive;
                category.UpdateAt = model.UpdatedAt;

                await _categoryService.UpdateAsync(category);

                var categoryUpdate = new CategoryDto
                {
                    CategoryId = category.CategoryId,
                    CategoryName = category.CategoryName,
                    Description = category.Description,
                    IsActive = category.IsActive,
                    CreatedAt = category.CreatedAt,
                    UpdateAt = model.UpdatedAt
                };

                return Ok(ApiResponse<CategoryDto>.Ok(categoryUpdate));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi cập nhật danh mục với ID: {Id}");
                return BadRequest(ApiResponse<CategoryDto>.Fail("Có lỗi xảy ra khi cập nhật danh mục."));
            }
        }

        [HttpDelete("DeleteCategory/{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ"));
            }
            try
            {
                var category = await _categoryService.GetById(id);
                if (category == null)
                {
                    return NotFound(ApiResponse<object>.Fail($"Không tìm thấy danh mục với ID: {id}", 404));
                }

                category.IsActive = false;
                await _categoryService.UpdateAsync(category);
                return Ok(ApiResponse<object>.Ok("Xóa danh mục thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi xóa danh mục với ID: {id}");
                return BadRequest(ApiResponse<CategoryDto>.Fail("Có lỗi xảy ra khi xóa danh mục."));
            }
        }
    }
}
