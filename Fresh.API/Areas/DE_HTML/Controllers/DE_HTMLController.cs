using EMS.EDXL.DE;
using EMS.EDXL.DE.v1_0;
using Fresh.API.Areas.DE_HTML.Models;
using Fresh.Global;
using Fresh.Global.ContentHelpers;
using Fresh.PostGIS;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Fresh.API.Areas.DE_HTML.Controllers
{

  /// <summary>
  /// Controller used to handle requests for enhanced HTML for displaying DE details, including
  /// Sensor details if present.
  /// </summary>
  [RouteArea("DE_HTML", AreaPrefix = "api/html")]
  public class DE_HTMLController : Controller
  {

    private PostGISDAL dbDal = new PostGISDAL(ConfigurationManager.ConnectionStrings["FRESH.PostGIS"].ConnectionString,
                              ConfigurationManager.AppSettings["PostGISSchema"], ConfigurationManager.AppSettings["FederationEndpointURL"]);


    /// <summary>
    /// GET: api/html/de/123
    /// 
    /// This is the main route
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [Route("de/{id:int}")]
    [HttpGet]
    public ActionResult GetDEDetails(int id)
    {
      ViewBag.DeHash = id;
      return View("DE_View");
    }

    /// <summary>
    /// GET:  api/html/de/dedetails/123
    /// 
    /// This is the route called by the main view.  Returns a partial view
    /// to enable updating only part of the browser content.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [Route("DE/DEDetails/{id:int}")]
    [HttpGet]
    public ActionResult GetEnhancedDetails(int id)
    {
      DEv1_0 de = dbDal.ReadDE(id);

      ContentObject co = de.ContentObjects[0];

      EMLCContent evtHelper = (EMLCContent)DEUtilities.FeedContent(de, co);
      string addr = DEUtilities.ReverseGeocodeLookup(evtHelper.Location().Latitude.ToString(), evtHelper.Location().Longitude.ToString());

      DE_Details_ViewModel vm = new DE_Details_ViewModel(evtHelper, addr, de.DateTimeSent);

      // return partial view, not full view
      return PartialView("DE_View_partial", vm);
    }
  }
}