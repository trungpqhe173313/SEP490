using Microsoft.AspNetCore.Mvc;
using NB.Service.Core.Mapper;
using NB.Service.PriceListService;
using NB.Service.ProductService;
using NB.Service.UserService;

namespace NB.API.Controllers
{
    [Route("api/pricelist")]
    public class PriceListController : Controller
    {
        private readonly IPriceListService _priceListService;
        private readonly IUserService _userService;
        private readonly IProductService _productService;
        private readonly IMapper _mapper;
        private readonly ILogger<PriceListController> _logger;
        public PriceListController(
            IPriceListService priceListService,
            IUserService userService,
            IProductService productService,
            IMapper mapper,
            ILogger<PriceListController> logger)
        {
            _priceListService = priceListService;
            _userService = userService;
            _productService = productService;
            _mapper = mapper;
            _logger = logger;
        }


    }
}
