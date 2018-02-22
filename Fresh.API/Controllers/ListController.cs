using Fresh.API.Swagger;
using Fresh.PostGIS;
using Npgsql;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using System.Web.Http.Description;

namespace Fresh.API.Controllers
{

  /// <summary>
  /// Class:    ListController
  /// Project:  Fresh.API
  /// Purpose:  This class is used for CRUD operations for value lists
  ///  
  /// Updates:  none
  /// </summary>
  [RoutePrefix("api/Values")]
  public class ListController : ApiController
  {
	private PostGISDAL dbDal = new PostGISDAL(ConfigurationManager.ConnectionStrings["FRESH.PostGIS"].ConnectionString,
	  ConfigurationManager.AppSettings["PostGISSchema"], ConfigurationManager.AppSettings["FederationEndpointURL"]);

	/// <summary>
	/// Returns a values list for the provided lookup id
	/// </summary>
	/// <param name="lookupID">ID of the value list to be retrieved</param>
	/// <returns>Value List or error</returns>
	[Route("List/{lookupID}", Name = "GetSingleList")]
	[HttpGet]
	[SwaggerResponse(HttpStatusCode.OK, Type = typeof(SourceValueListDTO))]
	[SwaggerResponse(HttpStatusCode.InternalServerError, "An error occurred when getting the source value list.")]
	[SwaggerResponse(HttpStatusCode.NotFound, "The source value list was not found")]
	[SwaggerContentType(ResponseContentType = "text/xml")]
	public HttpResponseMessage Get(int lookupID)
	{

	  try
	  {
		SourceValueListDTO retVal = null;

		if (dbDal.ReadSourceValue_List(lookupID, out retVal))
		{
		  return Request.CreateResponse(HttpStatusCode.OK, retVal);
		}
		else
		{
		  return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Source Value List not found");
		}
	  }
	  catch (NpgsqlException e)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Database error occurred");
	  }
	  catch (Exception e)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to get the source value list");
	  }

	}

	/// <summary>
	/// Adds a list of values to the system
	/// </summary>
	/// <param name="values">List of values</param>
	/// <returns>list of values or failure</returns>
	[Route("List")]
	[HttpPost]
	[SwaggerResponseRemoveDefaults]
	[SwaggerResponse(HttpStatusCode.Created, Type = typeof(SourceValueListDTO))]
	[SwaggerResponse(HttpStatusCode.InternalServerError, "An error occurred when creating the source value list.")]
	[SwaggerResponse(HttpStatusCode.BadRequest, "Message was not valid.")]
	[SwaggerResponse(HttpStatusCode.Conflict, "The Source Value already exists")]
	[SwaggerContentType("text/xml")]
	public HttpResponseMessage Post([FromBody]SourceValueListDTO values)
	{

	  try
	  {
		//create a new value
		if (dbDal.CreatedSourceValueList(values))
		{
		  HttpResponseMessage msg = Request.CreateResponse(HttpStatusCode.Created, values);

		  string location = HttpContext.Current.Request.Url + "/" + values.LookupID;
		  msg.Headers.Location = new Uri(location);

		  return msg;
		}
		else
		{
		  //something went wrong
		  return Request.CreateResponse(HttpStatusCode.InternalServerError, "An error occurred when creating the source value list");
		}
	  }
	  catch (InvalidOperationException e)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.Conflict, "The Source Value already exists");
	  }
	  catch (ArgumentNullException e)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Message was not valid");
	  }
	  catch (NpgsqlException e)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Database error occurred");
	  }
	  catch (Exception e)
	  {
		return Request.CreateResponse(HttpStatusCode.InternalServerError, "An error occurred when creating the source value list");
	  }
	}

	/// <summary>
	/// Deletes an entire list of values from the system
	/// </summary>
	/// <param name="lookupID">ID of the value list to be deleted</param>
	/// <returns>Success or failure</returns>
	[Route("List/{lookupID}")]
	[HttpDelete]
	[SwaggerResponse(HttpStatusCode.OK, "Success")]
	[SwaggerResponse(HttpStatusCode.NotFound, "The Source Value List was not found")]
	[SwaggerResponse(HttpStatusCode.InternalServerError, "An error occurred when deleting the source value list.")]
	public IHttpActionResult Delete(int lookupID)
	{

	  try
	  {
		//TODO: verify no active feeds/rules are using these values

		//first make sure no rule corresponding to feedId
		//if (!dbDal.ReadRule_Feed(lookupID, new RuleSourceValueDTO()))
		//{
		if (dbDal.DeletedSourceValueList(lookupID))
		{
		  return this.StatusCode(HttpStatusCode.OK);
		}
		//}
	  }
	  catch (ArgumentException e)
	  {
		return Content(HttpStatusCode.NotFound, "The Source Value List was not found");
	  }
	  catch (NpgsqlException e)
	  {
		return Content(HttpStatusCode.InternalServerError, "Database error occurred");
	  }
	  catch (Exception e)
	  {
		return Content(HttpStatusCode.InternalServerError, "Failed to delete the source value list");
	  }

	  return Content(HttpStatusCode.InternalServerError, "Failed to delete the source value list");
	}
  }
}
