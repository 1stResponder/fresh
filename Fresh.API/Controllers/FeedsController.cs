using System.Net;
using System.Net.Http;
using System.Web.Http;
using Fresh.PostGIS;
using System.Configuration;
using Fresh.Global;
using System.Collections.Generic;
using System;
using System.Web.Http.Description;
using Fresh.API.Swagger;
using Swashbuckle.Swagger.Annotations;
using System.Web;
using Npgsql;

namespace Fresh.API.Controllers
{
  [RoutePrefix("api")]
  public class FeedsController : ApiController
  {

	private PostGISDAL dbDal = new PostGISDAL(ConfigurationManager.ConnectionStrings["FRESH.PostGIS"].ConnectionString,
	ConfigurationManager.AppSettings["PostGISSchema"], ConfigurationManager.AppSettings["FederationEndpointURL"]);

	private bool allowDeleteAllViews = Boolean.Parse(ConfigurationManager.AppSettings["AllowDeleteAllViews"]);

	// GET: api/feeds
	/// <summary>
	/// Retrieves the feed details for all feeds.
	/// </summary>
	/// <returns>List of the FeedDTO of the feeds</returns>
	[Route("feeds", Name = "GetAllFeeds")]
	[HttpGet]
	[SwaggerResponse(HttpStatusCode.OK, Type = typeof(FeedDTO))]
	[SwaggerResponse(HttpStatusCode.InternalServerError, "An error occurred when getting the feed data.")]
	[SwaggerResponse(HttpStatusCode.NotFound, "No feeds were found")]
	[SwaggerContentType(ResponseContentType = "text/xml")]
	public HttpResponseMessage GetAllFeedDetail()
	{

	  try
	  {
		List<FeedDTO> result = dbDal.GetAllFeedDetails();
		if (result == null)
		{
		  return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Error getting feed data.");
		}
		else if (result.Count > 0)
		{
		  return Request.CreateResponse(HttpStatusCode.OK, result);
		}
		else
		{
		  return Request.CreateErrorResponse(HttpStatusCode.NotFound, "No feeds were found");
		}

	  }
	  catch (NpgsqlException e)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Database error occurred");
	  }
	  catch (Exception e)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to put get the feeds");
	  }
	}

	// GET: api/feeds/{lookupID}
	/// <summary>
	/// Retrieves the feed detail for the specified lookupID.
	/// </summary>
	/// <param name="lookupID">LookupID of the feed to return</param>
	/// <returns>FeedDTO of the feed</returns>
	[Route("feeds/{lookupID}", Name = "GetFeed")]
	[HttpGet]
	[SwaggerResponse(HttpStatusCode.OK, Type = typeof(FeedDTO))]
	[SwaggerResponse(HttpStatusCode.InternalServerError, "An error occurred when getting the feed data.")]
	[SwaggerResponse(HttpStatusCode.BadRequest, "The Feed was not found.")]
	[SwaggerContentType(ResponseContentType = "text/xml")]
	public HttpResponseMessage GetFeedDetail(int lookupID)
	{

	  try
	  {

		FeedDTO feed = null;

		bool result = dbDal.ReadFeed(lookupID, out feed);

		if (result == false)
		{
		  return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "The Feed was not found.");
		}
		else
		{
		  return Request.CreateResponse(HttpStatusCode.OK, feed);
		}
	  }
	  catch (NpgsqlException e)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Database error occurred");
	  }
	  catch (Exception e)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to put get the feeds");
	  }
	}

	// GET: api/feeds/byviewname/{viewName}
	/// <summary>
	/// Retrieves the feed detail for the specified view, if the view is a valid FeedContent view.
	/// </summary>
	/// <param name="viewName">name of the view to return</param>
	/// <returns>ViewDTO of the view</returns>
	[Route("feeds/byviewname/{viewName}", Name = "GetFeedByViewName")]
	[HttpGet]
	[SwaggerResponse(HttpStatusCode.OK, Type = typeof(FeedDTO))]
	[SwaggerResponse(HttpStatusCode.InternalServerError, "An error occurred when getting the feed data.")]
	[SwaggerResponse(HttpStatusCode.BadRequest, "The view name was invalid or the requested view is not an approved view.")]
	[SwaggerContentType(ResponseContentType = "text/xml")]
	public HttpResponseMessage GetViewDetail(string viewName)
	{

	  try
	  {

		if (viewName == null)
		{
		  return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Must specify a valid view name.");
		}
		else
		{
		  bool requestedApprovedView = IsApprovedFeedContentView(viewName);
		  if (requestedApprovedView == false)
		  {
			return Request.CreateErrorResponse(HttpStatusCode.BadRequest, String.Format("The requested view '{0}' is not an approved view.", viewName));
		  }

		  FeedDTO dto = dbDal.GetFeedByViewName(viewName);
		  if (dto != null)
		  {
			return Request.CreateResponse(HttpStatusCode.OK, dto);
		  }
		  else
		  {
			return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Error getting feed data.");
		  }
		}

	  }
	  catch (NpgsqlException e)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Database error occurred");
	  }
	  catch (Exception e)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to put get the feeds");
	  }
	}

	// POST: api/feeds
	/// <summary>
	/// Adds a feed to the system
	/// </summary>
	/// <param name="value">Feed</param>
	/// <returns>Feed or failure</returns>
	[Route("feeds")]
	[HttpPost]
	[SwaggerResponseRemoveDefaults]
	[SwaggerResponse(HttpStatusCode.Created, Type = typeof(FeedDTO))]
	[SwaggerResponse(HttpStatusCode.InternalServerError, "An error occurred when getting the feed data.")]
	[SwaggerResponse(HttpStatusCode.BadRequest, "The request contained no feed data to create.")]
	[SwaggerContentType("text/xml")]
	public HttpResponseMessage Post([FromBody]FeedDTO value)
	{

	  try
	  {
		if (value == null)
		{
		  return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "The request contained no feed data to create.");
		}

		if (value.LookupID == 0 && value.SourceID != null && value.SourceValue != null)
		{
		  int lookupID = DEUtilities.ComputeHash(new List<string> { value.SourceID, value.SourceValue });
		  value.LookupID = lookupID;
		}

		//create a new feed
		if (dbDal.AddedFeed(value))
		{
		  HttpResponseMessage msg = Request.CreateResponse(HttpStatusCode.Created, value);

		  string location = HttpContext.Current.Request.Url + "/" + value.LookupID;
		  msg.Headers.Location = new Uri(location);

		  return msg;
		}
		else
		{
		  return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Error getting feed data.");
		}
	  }
	  catch (NpgsqlException e)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Database error occurred");
	  }
	  catch (Exception e)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to put get the feeds");
	  }
	}

	// DELETE: api/feeds/{lookupID}
	/// <summary>
	/// Deletes the specified feed or all feeds if the lookupID is not specified.
	/// </summary>
	/// <param name="lookupID">Lookup Id of the view to delete</param>
	/// <returns>Success or failure</returns>
	[Route("feeds/{lookupID?}")]
	[HttpDelete]
	[SwaggerResponse(HttpStatusCode.OK, "Success")]
	[SwaggerResponse(HttpStatusCode.InternalServerError, "An error occurred when deleting the feed.")]
	[SwaggerResponse(HttpStatusCode.BadRequest, "No feed lookup ID was specified for deletion request.")]
	public HttpResponseMessage Delete(int lookupID = 0)
	{

	  try
	  {

		if (lookupID != 0)
		{
		  if (!dbDal.DeletedFeed(lookupID))
		  {
			return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to delete feed");
		  }
		  return Request.CreateResponse(HttpStatusCode.OK, "Success");
		}
		else
		{
		  if (allowDeleteAllViews)
		  {
			int iRowsAffected = -1;

			if (dbDal.DeletedAllFeeds(out iRowsAffected))
			{
			  return Request.CreateResponse(HttpStatusCode.OK, "Number of feeds Deleted:" + iRowsAffected.ToString());
			}
			else
			{
			  return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to delete all feeds.");
			}
		  }
		  else
		  {
			return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "No feed lookup ID was specified for deletion request.");
		  }
		}
	  }
	  catch (NpgsqlException e)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Database error occurred");
	  }
	  catch (Exception e)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to put get the feeds");
	  }
	}

	//TODO consolidate this method for the 2 controllers
	/// <summary>
	/// Checks to see if the view named is a valid FeedContent database view.
	/// In the future this may be a DB or config check, it may be cached and gets refreshed every so often.
	/// </summary>
	/// <param name="viewName"></param>
	/// <returns></returns>
	private bool IsApprovedFeedContentView(string viewName)
	{
	  //TODO: fetch this from DB and cache it for a period of time
	  List<string> goodViews = dbDal.GetFeedViewNames();
	  return goodViews.Contains(viewName);
	}



  }
}
