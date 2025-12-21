using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NB.Model.Entities;
using NB.Service.Common;
using NB.Service.Core.Mapper;
using NB.Service.Dto;
using NB.Service.PriceListDetailService;
using NB.Service.PriceListDetailService.Dto;
using NB.Service.PriceListDetailService.ViewModels;
using NB.Service.PriceListService;
using NB.Service.PriceListService.Dto;
using NB.Service.PriceListService.ViewModels;
using NB.Service.ProductService;
using NB.Service.UserService;

namespace NB.API.Controllers
{
    [Authorize]
    [Route("api/pricelist")]
    public class PriceListController : Controller
    {
        private readonly IPriceListService _priceListService;
        private readonly IPriceListDetailService _priceListDetailService;
        private readonly IUserService _userService;
        private readonly IProductService _productService;
        private readonly IMapper _mapper;
        private readonly ILogger<PriceListController> _logger;
        public PriceListController(
            IPriceListService priceListService,
            IPriceListDetailService priceListDetailService,
            IUserService userService,
            IProductService productService,
            IMapper mapper,
            ILogger<PriceListController> logger)
        {
            _priceListService = priceListService;
            _priceListDetailService = priceListDetailService;
            _userService = userService;
            _productService = productService;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpPost("GetData")]
        public async Task<IActionResult> GetData([FromBody] PriceListSearch search)
        {
            try
            {
                var priceLists = await _priceListService.GetAllData(search);
                var result = priceLists.Select(pl => new PriceListOutputVM
                {
                    PriceListId = pl.PriceListId,
                    PriceListName = pl.PriceListName,
                    StartDate = pl.StartDate,
                    EndDate = pl.EndDate,
                    IsActive = pl.IsActive,
                    CreatedAt = pl.CreatedAt
                }).ToList();

                var pagedResult = new PagedList<PriceListOutputVM>(
                    items: result,
                    pageIndex: search.PageIndex,
                    pageSize: search.PageSize,
                    totalCount: result.Count
                );
  
                    
                return Ok(ApiResponse<PagedList<PriceListOutputVM>>.Ok(pagedResult));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách bảng giá");
                return BadRequest(ApiResponse<List<PriceListDto>>.Fail("Có lỗi xảy ra khi lấy danh sách bảng giá.", 400));
            }
        }

        [HttpPost("GetDataForExport")]
        public async Task<IActionResult> GetDataForExport([FromBody] PriceListSearch search)
        {
            try
            {

                var priceLists = await _priceListService.GetAllData(search);
                var result = priceLists.Where(pl => pl.EndDate > DateTime.Now).Select(pl => new PriceListOutputVM
                {
                    PriceListId = pl.PriceListId,
                    PriceListName = pl.PriceListName,
                    StartDate = pl.StartDate,
                    EndDate = pl.EndDate,
                    IsActive = pl.IsActive,
                    CreatedAt = pl.CreatedAt
                }).ToList();

                var pagedResult = new PagedList<PriceListOutputVM>(
                    items: result,
                    pageIndex: search.PageIndex,
                    pageSize: search.PageSize,
                    totalCount: result.Count
                );


                return Ok(ApiResponse<PagedList<PriceListOutputVM>>.Ok(pagedResult));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách bảng giá");
                return BadRequest(ApiResponse<List<PriceListDto>>.Fail("Có lỗi xảy ra khi lấy danh sách bảng giá.", 400));
            }
        }


        [HttpGet("GetDetail/{priceListId}")]
        public async Task<IActionResult> GetDetail(int priceListId)
        {
            try
            {
                var priceList = await _priceListService.GetByPriceListId(priceListId);
                if(priceList == null) 
                {
                    return NotFound(ApiResponse<object>.Fail("Không tìm thấy bảng giá.", 404));
                }
                var pricelistDetails = await _priceListDetailService.GetByPriceListId(priceListId);
                if(pricelistDetails == null)
                {
                    return NotFound(ApiResponse<object>.Fail("Không tìm thấy chi tiết bảng giá.", 404));
                }
                var result = new PriceListOutputVM
                {
                    PriceListId = priceList.PriceListId,
                    PriceListName = priceList.PriceListName,
                    StartDate = priceList.StartDate,
                    EndDate = priceList.EndDate,
                    IsActive = priceList.IsActive,
                    CreatedAt = priceList.CreatedAt,
                    PriceListDetails = pricelistDetails
                };
                return Ok(ApiResponse<PriceListOutputVM>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy bảng giá");
                return BadRequest(ApiResponse<PriceListDto>.Fail("Có lỗi xảy ra khi lấy bảng giá.", 400));
            }
        }

        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromBody] PriceListCreateVM model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ", 400));
            }

            try
            {
                // Validate dates
                if (model.StartDate >= model.EndDate)
                {
                    return BadRequest(ApiResponse<object>.Fail("Ngày bắt đầu phải nhỏ hơn ngày kết thúc.", 400));
                }

                // Map to DTO
                var priceListDto = new PriceListDto
                {
                    PriceListName = model.PriceListName,
                    StartDate = model.StartDate,
                    EndDate = model.EndDate,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                await _priceListService.CreateAsync(priceListDto);
                int newPriceListId = priceListDto.PriceListId;

                // Nếu có chi tiết bảng giá
                if (model.PriceListDetails != null && model.PriceListDetails.Any())
                {
                    foreach (var detail in model.PriceListDetails)
                    {
                        // Validate product có tồn tại
                        var product = await _productService.GetByIdAsync(detail.ProductId);
                        if (product == null)
                        {
                            return BadRequest(ApiResponse<object>.Fail($"Sản phẩm với ID {detail.ProductId} không tồn tại.", 400));
                        }

                        var detailDto = new PriceListDetailDto
                        {
                            PriceListId = newPriceListId,
                            ProductId = detail.ProductId,
                            Price = detail.UnitPrice,
                            Note = detail.Note
                        };

                        await _priceListDetailService.CreateAsync(detailDto);
                    }
                }

                return Ok(ApiResponse<object>.Ok("Tạo bảng giá thành công."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo bảng giá");
                return BadRequest(ApiResponse<object>.Fail("Có lỗi xảy ra khi tạo bảng giá.", 400));
            }
        }

        [HttpPut("Update/{priceListId}")]
        public async Task<IActionResult> Update(int priceListId, [FromBody] PriceListUpdateVM model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ", 400));
            }

            try
            {
                // Kiểm tra nếu bảng giá tồn tại
                var existingPriceList = await _priceListService.GetByPriceListId(priceListId);
                if (existingPriceList == null)
                {
                    return NotFound(ApiResponse<object>.Fail("Không tìm thấy bảng giá.", 404));
                }

                // Kiểm tra ngày
                if (model.StartDate >= model.EndDate)
                {
                    return BadRequest(ApiResponse<object>.Fail("Ngày bắt đầu phải nhỏ hơn ngày kết thúc.", 400));
                }

                // Update bảng giá
                existingPriceList.PriceListName = model.PriceListName;
                existingPriceList.StartDate = model.StartDate;
                existingPriceList.EndDate = model.EndDate;
                existingPriceList.IsActive = model.IsActive;

                await _priceListService.UpdateAsync(existingPriceList);

                return Ok(ApiResponse<object>.Ok("Cập nhật bảng giá thành công."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật bảng giá");
                return BadRequest(ApiResponse<object>.Fail("Có lỗi xảy ra khi cập nhật bảng giá.", 400));
            }
        }

        [HttpPut("UpdatePriceListDetail/{priceListId}")]
        public async Task<IActionResult> UpdatePriceListDetail(int priceListId, [FromBody] PriceListDetailUpdateVM model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ", 400));
            }

            try
            {
                // Validate nếu bảng giá tồn tại
                var existingPriceList = await _priceListService.GetByPriceListId(priceListId);
                if (existingPriceList == null)
                {
                    return NotFound(ApiResponse<object>.Fail("Không tìm thấy bảng giá.", 404));
                }

                // Update chi tiết bảng giá - Xóa, Cập nhật, Thêm mới
                if (model.PriceListDetails != null && model.PriceListDetails.Any())
                {
                    // Lấy details hiện tại từ database
                    var existingDetails = await _priceListDetailService.GetById(priceListId);
                    var existingDetailsList = existingDetails?.ToList() ?? new List<PriceListDetailDto>();

                    // Lấy danh sách ProductId từ model gửi lên
                    var incomingProductIds = model.PriceListDetails.Select(d => d.ProductId).ToList();

                    // Xóa các record không còn trong danh sách gửi lên
                    var detailsToDelete = existingDetailsList
                        .Where(ed => !incomingProductIds.Contains((int)ed.ProductId))
                        .ToList();

                    foreach (var detail in detailsToDelete)
                    {
                        await _priceListDetailService.DeleteAsync(detail);
                    }

                    // 2. Update hoặc Tạo Detail mới
                    foreach (var incomingDetail in model.PriceListDetails)
                    {
                        //Kiểm tra sản phẩm tồn tại
                        var product = await _productService.GetByIdAsync(incomingDetail.ProductId);
                        if (product == null)
                        {
                            return BadRequest(ApiResponse<object>.Fail($"Sản phẩm với ID {incomingDetail.ProductId} không tồn tại.", 400));
                        }

                        // Kiểm tra nếu detail đã tồn tại
                        var existingDetail = existingDetailsList
                            .FirstOrDefault(ed => ed.ProductId == incomingDetail.ProductId);

                        if (existingDetail != null)
                        {
                            // Update detail
                            existingDetail.Price = incomingDetail.UnitPrice;

                            await _priceListDetailService.UpdateAsync(existingDetail);
                        }
                        else
                        {
                            // Create Detail mới
                            var newDetail = new PriceListDetailDto
                            {
                                PriceListId = priceListId,
                                ProductId = incomingDetail.ProductId,
                                Price = incomingDetail.UnitPrice,
                                Note = incomingDetail.Note
                            };

                            await _priceListDetailService.CreateAsync(newDetail);
                        }
                    }
                }

                return Ok(ApiResponse<object>.Ok("Cập nhật chi tiết bảng giá thành công."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật chi tiết bảng giá");
                return BadRequest(ApiResponse<object>.Fail("Có lỗi xảy ra khi cập nhật chi tiết bảng giá.", 400));
            }
        }

        [HttpDelete("Delete/{priceListId}")]
        public async Task<IActionResult> Delete(int priceListId)
        {
            try
            {
                var existingPriceList = await _priceListService.GetByPriceListId(priceListId);
                if (existingPriceList == null)
                {
                    return NotFound(ApiResponse<object>.Fail("Không tìm thấy bảng giá.", 404));
                }

                // Soft delete:
                existingPriceList.IsActive = false;

                await _priceListService.UpdateAsync(existingPriceList);

                return Ok(ApiResponse<object>.Ok("Vô hiệu hóa bảng giá thành công."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi vô hiệu hóa bảng giá");
                return BadRequest(ApiResponse<object>.Fail("Có lỗi xảy ra khi vô hiệu hóa bảng giá.", 400));
            }
        }
    }
}
