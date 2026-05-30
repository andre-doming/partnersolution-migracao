using System.Web.Mvc;

namespace SolutionsTools.WebApi.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return new RedirectResult("~/swagger");
        }
    }
}
