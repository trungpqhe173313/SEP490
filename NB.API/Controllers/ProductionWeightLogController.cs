using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NB.Service.ProductionWeightLogService;
using System.Threading.Tasks;

namespace NB.API.Controllers
{
    [Route("api/production-weight-logs")]
    [ApiController]
    [Authorize]
    public class ProductionWeightLogController : ControllerBase
    {
        private readonly IProductionWeightLogService _productionWeightLogService;

        public ProductionWeightLogController(IProductionWeightLogService productionWeightLogService)
        {
            _productionWeightLogService = productionWeightLogService;
        }

        /// <summary>
        /// Tổng hợp ProductionWeightLog theo ProductionId, nhóm theo từng sản phẩm
        /// </summary>
        /// <param name="productionId">ID của Production Order</param>
        /// <returns>Tổng số lượng bao và tổng khối lượng theo từng sản phẩm</returns>
        [HttpGet("summary/{productionId}")]
        public async Task<IActionResult> GetSummaryByProductionId(int productionId)
        {
            var result = await _productionWeightLogService.GetSummaryByProductionIdAsync(productionId);

            if (!result.Success)
            {
                return StatusCode(result.StatusCode, result);
            }

            return Ok(result.Data);
        }
    }
}
