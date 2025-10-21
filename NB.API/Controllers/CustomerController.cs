using Microsoft.AspNetCore.Mvc;

namespace NB.API.Controllers
{
    [Route("api/customer")]
    public class CustomerController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
