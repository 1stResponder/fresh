using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Fresh.PostGIS;
using System.Configuration;
using Fresh.API.Swagger;
using System.Web.Http.Description;
using Swashbuckle.Swagger.Annotations;
using Npgsql;

namespace Fresh.API.Controllers
{
  [RoutePrefix("api")]
  public class ViewContentController : ApiController
  {
    private PostGISDAL dbDal = new PostGISDAL(ConfigurationManager.ConnectionStrings["FRESH.PostGIS"].ConnectionString,
    ConfigurationManager.AppSettings["PostGISSchema"], ConfigurationManager.AppSettings["FederationEndpointURL"]);

    // GET: api/viewcontent/{viewName?}
    /// <summary>
    /// Retrieves the feed content data for the specified view, if the view is a valid FeedContent view.
    /// </summary>
    /// <param name="viewName">name of the view to return</param>
    /// <returns>FeedContent data from the view</returns>
    [Route("viewcontent/{viewName?}", Name = "GetFeedContentDataInView")]
    [HttpGet]
    [SwaggerResponse(HttpStatusCode.OK, Type = typeof(Dictionary<string,string>))]
    [SwaggerResponse(HttpStatusCode.InternalServerError, "An error occurred when getting the feed content view data.")]
    [SwaggerResponse(HttpStatusCode.BadRequest, "The requested view is not an approved view.")]
	[SwaggerResponse(HttpStatusCode.NotFound, "The requested view was not found")]
	public HttpResponseMessage Get(string viewName = null)
    {

	  try
	  {
		List<Dictionary<string, string>> data = new List<Dictionary<string, string>>();

		if (viewName == null)
		{
		  data = dbDal.GetFeedContentDataByView(null);
		}
		else
		{
		  // Not sure how dynamic the list of valid views are but this will check to make sure its only using a valid one.
		  bool requestedApprovedView = IsApprovedFeedContentView(viewName);
		  if (requestedApprovedView == false)
		  {
			return Request.CreateErrorResponse(HttpStatusCode.BadRequest, String.Format("The requested view '{0}' is not an approved view.", viewName));
		  }
		  data = dbDal.GetFeedContentDataByView(viewName);
		}

		if (data != null)
		{
		  return Request.CreateResponse(HttpStatusCode.OK, data);
		}
		else
		{
		  return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Error getting feed content view data.");
		}
	  }
	  catch (ArgumentException e)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.NotFound, "The view was not found");
	  }
	  catch (NpgsqlException e)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Database error occurred");
	  }
	  catch (Exception e)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Error getting feed content view data.");
	  }

	}
	
	//TODO consolidate this method for the 2 controllers
	/// <summary>
	/// Checks to see if the view named is a valid FeedContent database view.
	/// In the future this may be a DB or config check, it may be cached and gets refreshed every so often.
	/// </summary>
	/// <param name="viewName"></param>
	/// <returns>True if the view exists and is valid.  False otherwise</returns>
	/// <exception cref="Exception">An error occurred when getting the views</exception>
	/// <exception cref="NpgsqlException">Throw if there was a problem connecting to the database</exception>
	private bool IsApprovedFeedContentView(string viewName)
    {
      //TODO: fetch this from DB and cache it for a period of time
      List<string> goodViews = dbDal.GetFeedViewNames();
      return goodViews.Contains(viewName);
    }

  }
}
