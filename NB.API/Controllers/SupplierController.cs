using Microsoft.AspNetCore.Mvc;

namespace NB.API.Controllers
{
    [Route("api/supplier")]
    public class SupplierController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
