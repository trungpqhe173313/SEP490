using Microsoft.AspNetCore.Mvc;
using NB.Service.Common;
using NB.Service.Core.Mapper;
using NB.Service.Dto;
using NB.Service.ProductService;
using NB.Service.ProductService.Dto;
using NB.Service.SupplierService.Dto;
using NB.Service.TransactionDetailService;
using NB.Service.TransactionDetailService.Dto;
using NB.Service.TransactionService;
using NB.Service.TransactionService.Dto;
using NB.Service.UserService.Dto;

namespace NB.API.Controllers
{
    [Route("api/stockoutput")]
    public class StockOutputController : Controller
    {
        private readonly ITransactionService _transactionService;
        private readonly ITransactionDetailService _transactionDetailService;
        private readonly IProductService _productService;
        private readonly ILogger<EmployeeController> _logger;
        private readonly IMapper _mapper;
        private readonly string transactionType = "Export";
        public StockOutputController(
            ITransactionService transactionService,
            ITransactionDetailService transactionDetailService,
            IProductService productService,
            IMapper mapper,
            ILogger<EmployeeController> logger)
        {
            _transactionService = transactionService;
            _transactionDetailService = transactionDetailService;
            _productService = productService;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpPost("GetData")]
        public async Task<IActionResult> GetData([FromBody]TransactionSearch search)
        {
            try
            {
                search.Type = transactionType;
                var result = await _transactionService.GetData(search);
                return Ok(ApiResponse<PagedList<TransactionDto>>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy dữ liệu đơn hàng");
                return BadRequest(ApiResponse<PagedList<SupplierDto>>.Fail("Có lỗi xảy ra khi lấy dữ liệu"));
            }
        }

        [HttpGet("GetByTransactionId")]
        public async Task<IActionResult> GetByTransactionId(int id)
        {
            try
            {
                var result = await _transactionService.GetByTransactionId(id);
                if (result == null)
                {
                    return NotFound(ApiResponse<UserDto>.Fail("Không tìm thấy đơn hàng", 404));
                }
                return Ok(ApiResponse<TransactionDto>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy đơn hàng với Id: {Id}", id);
                return BadRequest(ApiResponse<TransactionDto>.Fail("Có lỗi xảy ra"));
            }
        }

        [HttpGet("GetTransactionDetailByTransactionId")]
        public async Task<IActionResult> GetTransactionDetailByTransactionId(int id)
        {
            try
            {
                //lay don hàng
                var result = await _transactionDetailService.GetByTransactionId(id);
                if (result == null)
                {
                    return NotFound(ApiResponse<UserDto>.Fail("Không tìm thấy đơn hàng", 404));
                }
                //lay danh sach product id
                List<int> listProductId = result.Select(td => td.ProductId).ToList();
                //lay ra các sản phẩm trong listProductId
                var listProduct = await _productService.GetByIds(listProductId);
                foreach (var t in result)
                {
                    var product = listProduct.FirstOrDefault(p => p.ProductId == t.ProductId);
                    if (product is not null)
                    {
                        t.ProductName = product.ProductName;
                        t.ImageUrl = product.ImageUrl;
                    }
                }

                return Ok(ApiResponse<List<TransactionDetailDto>>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy đơn hàng với Id: {Id}", id);
                return BadRequest(ApiResponse<TransactionDto>.Fail("Có lỗi xảy ra"));
            }
        }
    }
}
