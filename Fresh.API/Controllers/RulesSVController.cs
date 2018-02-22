using Fresh.PostGIS;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Configuration;
using Swashbuckle.Swagger.Annotations;
using Fresh.API.Swagger;
using System.Web.Http.Description;
using Npgsql;
using System;
using Fresh.Global;

namespace Fresh.API.Controllers
{
  /// <summary>
  /// Class:    RulesSVController
  /// Project:  Fresh.API
  /// Purpose:  This class is used for CRUD operations for rules with feeds.
  /// Created:  2016-03-10
  /// Author:   Brian Wilkins - ArdentMC
  /// 
  /// Updates:  none
  /// </summary>

  // setup the base route for the controller
  [RoutePrefix("api/Rules")]
  public class RulesSVController : ApiController
  {
	private PostGISDAL dbDal = new PostGISDAL(ConfigurationManager.ConnectionStrings["FRESH.PostGIS"].ConnectionString,
	  ConfigurationManager.AppSettings["PostGISSchema"], ConfigurationManager.AppSettings["FederationEndpointURL"]);

	// GET: api/RulesSV
	/// <summary>
	/// Retrieves list of SV rules in the system
	/// </summary>
	/// <returns>Summary list of SV rules or failure</returns>  
	[Route("Feed")]
	[HttpGet]
	[SwaggerResponse(HttpStatusCode.OK, Type = typeof(RuleSourceValueDTO))]
	[SwaggerResponse(HttpStatusCode.InternalServerError, "An error occurred when getting the rule source value.")]
	[SwaggerResponse(HttpStatusCode.NotFound, "There are no rules.")]
	[SwaggerContentType(ResponseContentType = "text/xml")]
	public HttpResponseMessage Get()
	{

	  try
	  {
		List<RuleSourceValueDTO> lstRules;

		bool wasSuccessful = dbDal.ReadRule_AllFeeds(out lstRules);

		if (!wasSuccessful || lstRules == null)
		{
		  return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to put get the feeds");
		}
		else if (wasSuccessful && lstRules.Count > 0)
		{
		  return Request.CreateResponse(HttpStatusCode.OK, lstRules);
		}
		else
		{
		  return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Feeds empty");
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

	// GET: api/RulesSV/5
	/// <summary>
	/// Gets a SV rule by hash 
	/// </summary>
	/// <param name="lookupID">ID of the rule to retrieve</param>
	/// <returns>SV rule or failure</returns>
	[Route("Feed/{lookupID}", Name = "GetSingleFeedRule")]
	[HttpGet]
	[SwaggerContentType("text/xml")]
	[SwaggerResponse(HttpStatusCode.OK, Type = typeof(RuleSourceValueDTO))]
	[SwaggerResponse(HttpStatusCode.InternalServerError, "An error occurred when getting the rule source value.")]
	[SwaggerResponse(HttpStatusCode.NotFound, "The rule was not found.")]
	public HttpResponseMessage Get(int lookupID)
	{

	  try
	  {
		RuleSourceValueDTO retVal = null;

		//get rule by hash
		if (dbDal.ReadRule_Feed(lookupID, out retVal))
		{
		  return Request.CreateResponse(HttpStatusCode.OK, retVal);
		}
		else
		{
		  return Request.CreateErrorResponse(HttpStatusCode.NotFound, "No rule was found");
		}
	  }
	  catch (ArgumentException e)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.NotFound, "No rule was found");
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

	// POST: api/RulesSV
	/// <summary>
	/// Adds a SV rule to the system
	/// </summary>
	/// <param name="value">Rule</param>
	/// <returns>SV Rule or failure</returns>
	[Route("Feed")]
	[HttpPost]
	[SwaggerResponseRemoveDefaults]
	[SwaggerResponse(HttpStatusCode.Created, Type = typeof(RuleSourceValueDTO))]
	[SwaggerResponse(HttpStatusCode.InternalServerError, "An error occurred when creating the source value rule.")]
	[SwaggerResponse(HttpStatusCode.BadRequest, "The message was not a valid source value")]
	[SwaggerContentType("text/xml")]
	public IHttpActionResult Post([FromBody]RuleSourceValueDTO value)
	{

	  try
	  {
		int ruleHash = -1;

		if (value == null)
		{
		  return Content(HttpStatusCode.BadRequest, "The message could not be read");
		}

		//create a new rule
		if (dbDal.CreatedRule_Feed(value, out ruleHash))
		{
		  return this.CreatedAtRoute("GetSingleFeedRule", new
		  {
			lookupID = ruleHash
		  }, value);
		}
		else
		{
		  // something went wrong
		  return this.StatusCode(HttpStatusCode.InternalServerError);
		}
	  }
	  catch (NpgsqlException e)
	  {
		return Content(HttpStatusCode.InternalServerError, "Database error occurred");
	  }
	  catch (Exception e)
	  {
		return Content(HttpStatusCode.InternalServerError, "Failed to add the source value rule");
	  }
	}

	// PUT: api/RulesSV/5
	/// <summary>
	/// Updates a SV rule
	/// </summary>
	/// <param name="lookupID">ID of the rule to be updated</param>
	/// <param name="value">Updated SV Rule</param>
	/// <returns>Success or failure</returns>
	[Route("Feed/{lookupID}")]
	[HttpPut]
	[SwaggerContentType(RequestContentType = "text/xml")]
	[SwaggerResponse(HttpStatusCode.OK, "Success")]
	[SwaggerResponse(HttpStatusCode.BadRequest, "The message was not a valid Rule Source Value")]
	[SwaggerResponse(HttpStatusCode.InternalServerError, "An error occurred when updating the source value rule.")]
	public IHttpActionResult Put(int lookupID, [FromBody]RuleSourceValueDTO value)
	{

	  if (value == null)
	  {
		return Content(HttpStatusCode.BadRequest, "The message was not a valid Rule Source Value DTO");
	  }

	  try
	  {
		//create a new rule
		if (dbDal.UpdatedRule_Feed(lookupID, value))
		{
		  return Ok();
		}
		else
		{
		  // something went wrong
		  return this.StatusCode(HttpStatusCode.InternalServerError);
		}
	  }
	  catch (NpgsqlException e)
	  {
		return Content(HttpStatusCode.InternalServerError, "Database error occurred");
	  }
	  catch (Exception e)
	  {
		return Content(HttpStatusCode.InternalServerError, "Failed to ");
	  }
	}


	// PUT: api/RulesSV/5
	/// <summary>
	/// Updates a SV rule
	/// </summary>
	/// <param name="lookupID">ID of the rule to be updated</param>
	/// <param name="feedid">Id of the feed to be updated</param>
	/// <param name="feedvalue">Content to update the rule</param>
	/// <returns>Success or failure</returns>
	[Route("Feed/{lookupID}/feedid/{feedid}/feedvalue/{feedvalue}")]
	//[Route("Feed/{lookupID}")]
	[HttpPut]
	[SwaggerResponse(HttpStatusCode.OK, "Success")]
	[SwaggerResponse(HttpStatusCode.BadRequest, "The feedID and feedValue are required")]
	[SwaggerResponse(HttpStatusCode.NotFound, "The rule was not found")]
	[SwaggerResponse(HttpStatusCode.InternalServerError, "An error occurred when updating the source value rule.")]
	public IHttpActionResult Put(int lookupID, string feedid, string feedvalue)
	{

	  try
	  {
		//create a new rule
		if (dbDal.UpdateRuleFeed(lookupID, feedid, feedvalue))
		{
		  return Ok();
		}
		else
		{
		  // something went wrong
		  return this.StatusCode(HttpStatusCode.InternalServerError);
		}
	  }
	  catch (ArgumentNullException e)
	  {
		return Content(HttpStatusCode.BadRequest, "The feedID and feedValue are required");
	  }
	  catch (ArgumentException e)
	  {
		return Content(HttpStatusCode.NotFound, "The rule was not found");
	  }
	  catch (NpgsqlException e)
	  {
		return Content(HttpStatusCode.InternalServerError, "Database error occurred");
	  }
	  catch (Exception e)
	  {
		return Content(HttpStatusCode.InternalServerError, "Failed to add the rule");
	  }
	}


	// DELETE: api/RulesSV/5
	/// <summary>
	/// Deletes the specified SV rule
	/// </summary>
	/// <param name="lookupID">ID of the rule to be deleted</param>
	/// <returns>Success or failure</returns>
	[Route("Feed/{lookupID}")]
	[HttpDelete]
	[SwaggerResponse(HttpStatusCode.OK, "Success")]
	[SwaggerResponse(HttpStatusCode.InternalServerError, "An error occurred when deleting the source value rule.")]
	public IHttpActionResult Delete(int lookupID)
	{

	  try
	  {

		if (!dbDal.DeletedRuleFeed(lookupID))
		{
		  return this.StatusCode(HttpStatusCode.InternalServerError);
		}
	  }
	  catch (NpgsqlException e)
	  {
		return Content(HttpStatusCode.InternalServerError, "Database error occurred");
	  }
	  catch (Exception e)
	  {
		return Content(HttpStatusCode.InternalServerError, "Failed to delete the source value rule");
	  }
	  return Ok();
	}

	/// <summary>
	/// TODO: Implement Method
	/// Deletes the specified SV rule
	/// </summary>
	/// <param name="lookupID">ID of the rule to be deleted</param>
	/// <param name="feedid">Id of the feed to be updated</param>
	/// <param name="feedvalue">Content to update the rule</param>
	/// <returns>Success or failure</returns>
	[ApiExplorerSettings(IgnoreApi =true)]
	[Route("Feed/{lookupID}/feedid/{feedid}/feedvalue/{feedvalue}")]
	// [Route("Feed/{lookupID}")]
	[HttpDelete]
	//[SwaggerResponse(HttpStatusCode.OK, "Success")]
	//[SwaggerResponse(HttpStatusCode.InternalServerError, "An error occurred when deleting the source value rule.")]
	[SwaggerResponse(HttpStatusCode.NotImplemented, "The endpoint is not currently implemented")]
	
	public IHttpActionResult Delete(int lookupID, string feedid, string feedvalue)
	{

	  try
	  {
		throw new NotImplementedException("This endpoint is not currently fleshed out");
	  }
	  catch (Exception Ex)
	  {
		DEUtilities.LogMessage(string.Format("[{0}] {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, Ex.Message), DEUtilities.LogLevel.Error, Ex);
		return Content(HttpStatusCode.NotImplemented, "");
	  }

	  /*
	   * // This code will simply delete all of the feeds, not a specific one
	  try
	  {
		if (!dbDal.DeletedRuleFeed(lookupID))
		{
		  return this.StatusCode(HttpStatusCode.InternalServerError);
		}
	  }
	  catch (NpgsqlException e)
	  {
		return Content(HttpStatusCode.InternalServerError, "Database error occurred");
	  }
	  catch (Exception e)
	  {
		return Content(HttpStatusCode.InternalServerError, "Failed to delete the source value rule");
	  }
	  return Ok();*/

	}
  }
}
