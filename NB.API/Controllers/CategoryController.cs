using Microsoft.AspNetCore.Mvc;
using NB.API.Utils;
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
                var categoryList = await _categoryService.GetDataWithProducts();

                var searchString = Helper.RemoveDiacritics(search.CategoryName);
                // Lọc danh mục dựa trên tên danh mục nếu được cung cấp
                var filteredCategories = string.IsNullOrEmpty(searchString)
                    ? categoryList
                    : categoryList
                        .Where(c => c.CategoryName != null &&
                                   Helper.RemoveDiacritics(c.CategoryName) // Chuẩn hóa tên sản phẩm
                                        .Contains(searchString, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                if (filteredCategories.Count == 0)
                {
                    return NotFound(ApiResponse<object>.Fail("Không tìm thấy danh mục với tên tương tự.", 404));
                }

                var pagedResult = PagedList<CategoryDetailDto>.CreateFromList(filteredCategories, search);

                return Ok(ApiResponse<PagedList<CategoryDetailDto>>.Ok(pagedResult));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách danh mục");
                return BadRequest(ApiResponse<CategoryDto>.Fail("Có lỗi xảy ra khi lấy danh sách danh mục.", 400));
            }
        }

        [HttpGet("GetById/{Id}")]
        public async Task<IActionResult> GetById(int Id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ", 400));
            }
            if (Id <= 0)
            {
                return BadRequest(ApiResponse<object>.Fail($"ID {Id} không hợp lệ", 400));
            }
            try
            {
                var category = await _categoryService.GetByIdWithProducts(Id);
                if (category == null)
                {
                    return NotFound(ApiResponse<object>.Fail($"Không tìm thấy danh mục với ID: {Id}", 404));
                }
                return Ok(ApiResponse<CategoryDetailDto?>.Ok(category));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy danh mục với ID: {Id}");
                return BadRequest(ApiResponse<CategoryDto>.Fail("Có lỗi xảy ra khi lấy danh mục.", 400));
            }
        }

        [HttpPost("CreateCategory")]
        public async Task<IActionResult> CreateCategory([FromBody] CategoryCreateVM model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ", 400));
            }
            var IsCategoryExist = await _categoryService.GetByName(model.CategoryName.Replace(" ", ""));
            if (!(IsCategoryExist == null))
            {
                return BadRequest(ApiResponse<object>.Fail("Tên danh mục đã tồn tại", 400));
            }
            try
            {
                CategoryDto newCategory = new CategoryDto
                {
                    CategoryName = model.CategoryName.Replace(" ", ""),
                    Description = model.Description,
                    IsActive = true,
                    CreatedAt = model.CreatedAt
                };
                await _categoryService.CreateAsync(newCategory);
                return Ok(ApiResponse<CategoryDto>.Ok(newCategory));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo danh mục mới");
                return BadRequest(ApiResponse<CategoryDto>.Fail("Có lỗi xảy ra khi tạo danh mục.", 400));
            }
        }

        [HttpPut("UpdateCategory/{Id}")]
        public async Task<IActionResult> UpdateCategory(int Id, [FromBody] CategoryUpdateVM model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ", 400));
            }
            try
            {

                var category = await _categoryService.GetById(Id);
                if (category == null)
                {
                    return NotFound(ApiResponse<object>.Fail("Không tồn tại danh mục với Id này", 404));
                }
                var cateName = category.CategoryName.Trim().Replace(" ", "");
                var existingCategory = await _categoryService.GetByName(cateName);
                if ( existingCategory != null && existingCategory.CategoryId != Id)
                {
                    return BadRequest(ApiResponse<object>.Fail("Tên danh mục này đã được đăng kí", 400));
                }
                category.CategoryName = model.CategoryName.Replace(" ", "");
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
                return BadRequest(ApiResponse<CategoryDto>.Fail("Có lỗi xảy ra khi cập nhật danh mục.", 400));
            }
        }

        [HttpDelete("DeleteCategory/{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ", 400));
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
                return BadRequest(ApiResponse<CategoryDto>.Fail("Có lỗi xảy ra khi xóa danh mục.", 400));
            }
        }
    }
}