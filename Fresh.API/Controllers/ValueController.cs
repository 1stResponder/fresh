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
using System.Web.Http;
using System.Web.Http.Description;
using System.Web.Http.Results;

namespace Fresh.API.Controllers
{
  ///<summary>
  /// Class:    ValueController
  /// Project:  Fresh.API
  /// Purpose:  This class is used for CRUD operations for values
  /// 
  /// Updates:  none
  /// </summary>
  
  [RoutePrefix("api/Values")]
  public class ValueController : ApiController
  {
    private PostGISDAL dbDal = new PostGISDAL(ConfigurationManager.ConnectionStrings["FRESH.PostGIS"].ConnectionString, 
      ConfigurationManager.AppSettings["PostGISSchema"], ConfigurationManager.AppSettings["FederationEndpointURL"]);

	/// <summary>
	/// Returns a single value from a list
	/// </summary>
	/// <param name="lookupID">ID of the value to be retrieved</param>
	/// <returns>Value or error</returns>
	[Route("Value/{lookupID}", Name = "GetSingleValue")]
	[HttpGet]
	[SwaggerResponse(HttpStatusCode.OK, Type = typeof(SourceValueDTO))]
	[SwaggerResponse(HttpStatusCode.InternalServerError, "An error occurred when getting the source value.")]
	[SwaggerResponse(HttpStatusCode.NotFound, "Source Value not found")]
	[SwaggerContentType(ResponseContentType = "text/xml")]
	public HttpResponseMessage Get(int lookupID)
	{

	  try
	  {
		SourceValueDTO retVal = null;

		if (dbDal.ReadSourceValue_Item(lookupID, out retVal))
		{
		  return Request.CreateResponse(HttpStatusCode.OK, retVal);
		}
		else
		{
		  return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to get the value");
		}
	  }
	  catch (NpgsqlException e)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Database error occurred");
	  }
	  catch (ArgumentException e)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.NotFound, "The Source Value was not found");
	  }
	  catch (Exception e)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to get the value");
	  }
	}

	/// <summary>
	/// Adds a new value to a list in the system
	/// </summary>
	/// <param name="value">Value to add</param>
	/// <returns>Value or error</returns>
	[Route("Value")]
    [HttpPost]
    [SwaggerResponseRemoveDefaults]
    [SwaggerResponse(HttpStatusCode.Created, Type = typeof(SourceValueDTO))]
    [SwaggerResponse(HttpStatusCode.InternalServerError, "An error occurred when creating the value.")]
	[SwaggerResponse(HttpStatusCode.BadRequest, "The message was not valid")]
	[SwaggerResponse(HttpStatusCode.Conflict, "The Source Value already exists")]
	[SwaggerContentType("text/xml")]
    public IHttpActionResult Post([FromBody]SourceValueDTO value)
    {

	  try
	  {
		int? feedHash = null;

		//create a new value
		if (dbDal.CreatedSourceValue(value, out feedHash))
		{
		  return this.CreatedAtRoute("GetSingleValue", new
		  {
			lookupID = feedHash.Value
		  }, value);
		}
		else
		{
		  //something went wrong
		  return Content(HttpStatusCode.InternalServerError, "Failed to add the Source Value");
		}
	  }
	  catch (NpgsqlException e)
	  {
		return Content(HttpStatusCode.InternalServerError, "Database error occurred");
	  }
	  catch (ArgumentNullException e)
	  {
		return Content(HttpStatusCode.BadRequest, "The message was not valid");
	  }
	  catch (InvalidOperationException e)
	  {
		return Content(HttpStatusCode.Conflict, "The Source Value already exists");
	  }
	  catch (Exception e)
	  {
		return Content(HttpStatusCode.InternalServerError, "Failed to add the Source Value");
	  }
	}

	/// <summary>
	/// Deletes a single value from the system
	/// </summary>
	/// <param name="lookupID">ID of the value to be deleted</param>
	/// <returns>Success or failure</returns>
	[Route("Value/{lookupID}")]
    [SwaggerResponse(HttpStatusCode.OK, "Success")]
    [SwaggerResponse(HttpStatusCode.InternalServerError, "An error occurred when deleting the value.")]
	[SwaggerResponse(HttpStatusCode.NotFound, "The source value rule was not found")]
	[SwaggerResponse(HttpStatusCode.Conflict, "The source value feed corresponds to an active rule")]
	[HttpDelete]
    public IHttpActionResult Delete(int lookupID)
    {

	  try
	  {
		RuleSourceValueDTO sv;

		bool wasFound = true;

		try
		{
		  //first make sure no rule corresponding to feedID
		  wasFound = dbDal.ReadRule_Feed(lookupID, out sv);
		}
		catch (ArgumentException e)
		{
		  wasFound = false;
		}

		
		if (!wasFound)
		{
		  if (dbDal.DeletedSourceValue(lookupID))
		  {
			return this.StatusCode(HttpStatusCode.OK);
		  }
		}
		else
		{
		  return Content(HttpStatusCode.Conflict, "This feed corresponds to an active rule");
		}

	  }
	  catch (NpgsqlException e)
	  {
		return Content(HttpStatusCode.InternalServerError, "Database error occurred");
	  }
	  catch (ArgumentException e)
	  {
		return Content(HttpStatusCode.NotFound, "This lookupID does not point to a source value rule");
	  }
	  catch (Exception e)
	  {
		return Content(HttpStatusCode.InternalServerError, "Failed to delete the source value");
	  }

	  return Content(HttpStatusCode.InternalServerError, "Failed to delete the source value");
	}
  }
}
