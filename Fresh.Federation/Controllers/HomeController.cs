using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Fresh.Federation.Controllers
{
  [RoutePrefix("api")]
  public class HomeController : Controller
  {
    public ActionResult Index()
    {
      ViewBag.Title = "FRESH Federation Home Page";

      return View();
    }
  }
}
