using Fresh.PostGIS;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using System.Configuration;
using Fresh.API.Swagger;
using Swashbuckle.Swagger.Annotations;
using Npgsql;
using System;

namespace Fresh.API.Controllers
{
  /// <summary>
  /// Class:    RulesFedController
  /// Project:  Fresh.API
  /// Purpose:  This class is used for CRUD operations for rules with federation Uri.
  /// Created:  2016-03-10
  /// Author:   Brian Wilkins - ArdentMC
  /// 
  /// Updates:  none
  /// </summary>

  // setup the base route for the controller
  [RoutePrefix("api/Rules")]
  public class RulesFedController : ApiController
  {
	/// <summary>
	/// DAL class to interface to database
	/// </summary>
	private PostGISDAL dbDal = new PostGISDAL(ConfigurationManager.ConnectionStrings["FRESH.PostGIS"].ConnectionString,
	  ConfigurationManager.AppSettings["PostGISSchema"], ConfigurationManager.AppSettings["FederationEndpointURL"]);//TODO: once all needed functions written, change to IDatabaseDAL and set to appropriate DB dal

	/// <summary>
	/// Returns all of the rules with federation URI
	/// </summary>
	/// <returns>Set of federation rules or failure</returns>
	[Route("Federation")]
	[HttpGet]
	[SwaggerResponse(HttpStatusCode.OK, Type = typeof(RuleFedDTO))]
	[SwaggerResponse(HttpStatusCode.InternalServerError, "An error occurred when getting the federation rules.")]
	[SwaggerResponse(HttpStatusCode.NotFound, "There are no federation rules")]
	[SwaggerContentType(ResponseContentType = "text/xml")]
	public HttpResponseMessage Get()
	{

	  try
	  {
		List<RuleFedDTO> lstRules;

		bool wasSuccess = dbDal.ReadRule_AllFedUri(out lstRules);

		if (!wasSuccess || lstRules == null)
		{
		  return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to get the Federation rules");
		}
		else if (wasSuccess && lstRules.Count > 0)
		{
		  return Request.CreateResponse(HttpStatusCode.OK, lstRules);
		}
		else
		{
		  return Request.CreateErrorResponse(HttpStatusCode.NotFound, "No Federation Rules Found");
		}
	  }
	  catch (NpgsqlException e)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Database error occurred");
	  }
	  catch (Exception e)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to get the Federation rules");
	  }
	}

	/// <summary>
	/// Gets a rule with federation URI by hash
	/// </summary>
	/// <param name="lookupID">Lookup id of the federation rule</param>
	/// <returns>Rule with federation URI or failure</returns>
	[Route("Federation/{lookupID}", Name = "GetSingleFederationRule")]
	[HttpGet]
	[SwaggerResponse(HttpStatusCode.OK, Type = typeof(RuleFedDTO))]
	[SwaggerResponse(HttpStatusCode.InternalServerError, "An error occurred when getting the federation rule.")]
	[SwaggerResponse(HttpStatusCode.NotFound, "The federation rule was not found")]
	[SwaggerContentType(ResponseContentType = "text/xml")]
	public HttpResponseMessage Get(int lookupID)
	{

	  try
	  {
		RuleFedDTO retVal = null;

		//get rule by hash
		if (dbDal.ReadRule_FedUri(lookupID, out retVal))
		{
		  return Request.CreateResponse(HttpStatusCode.OK, retVal);
		}
		else
		{
		  return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to get the federation rule");
		}

	  }
	  catch (ArgumentException e)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.NotFound, "The Federation Rule was not found");
	  }
	  catch (NpgsqlException e)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Database error occurred");
	  }
	  catch (Exception e)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to get the federation rule");
	  }
	}

	// POST: api/Rules/Federation
	/// <summary>
	/// Adds a rule with federation URI to the system
	/// </summary>
	/// <param name="value">Content to set the rule</param>
	/// <returns>Rule with federation URI or failure</returns>
	[Route("Federation")]
	[HttpPost]
	[SwaggerResponseRemoveDefaults]
	[SwaggerResponse(HttpStatusCode.Created, Type = typeof(RuleFedDTO))]
	[SwaggerResponse(HttpStatusCode.InternalServerError, "An error occurred when creating the federation rule.")]
	[SwaggerResponse(HttpStatusCode.BadRequest, "The message was not a valid Federation Rule.")]
	[SwaggerContentType("text/xml")]
	public IHttpActionResult Post([FromBody]RuleFedDTO value)
	{

	  try
	  {
		int ruleHash = -1;

		//create a new rule
		if (dbDal.CreatedRule_FedUri(value, out ruleHash))
		{
		  return this.CreatedAtRoute("GetSingleFederationRule", new
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
	  catch (InvalidOperationException e)
	  {
		return Content(HttpStatusCode.BadRequest, "The Federation Rule was not valid.  It was missing a required value or had an invalid one.");
	  }
	  catch (ArgumentNullException e)
	  {
		return Content(HttpStatusCode.BadRequest, "The message was not valid");
	  }
	  catch (NpgsqlException e)
	  {
		return Content(HttpStatusCode.InternalServerError, "Database error occurred");
	  }
	  catch (Exception e)
	  {
		return Content(HttpStatusCode.InternalServerError, "Failed to add the Federation Rule");
	  }
	}

	// PUT: api/Rules/Federation/5
	// PUT: api/Rules/Federation/5/?destination=""
	/// <summary>
	/// Updates rule.  If a destination is specified, it adds the destination
	/// string to the list of Federation URIs.
	/// </summary>
	/// <param name="lookupID">ID of the rule to be updated</param>
	/// <param name="value">Content to update the rule</param>
	/// <param name="destination">Federation URI to be added to rule</param>
	/// <returns>Success or failure</returns>
	[Route("Federation/{lookupID}")]
	[HttpPut]
	[SwaggerResponse(HttpStatusCode.OK, Type = typeof(RuleFedDTO))]
	[SwaggerResponse(HttpStatusCode.InternalServerError, "An error occurred when updating the federation rule.")]
	[SwaggerResponse(HttpStatusCode.BadRequest, "The message was not a valid Federation Rule.")]
	[SwaggerContentType("text/xml")]
	public IHttpActionResult Put(int lookupID, [FromBody]RuleFedDTO value, string destination = null)
	{

	  if (value == null)
	  {
		return Content(HttpStatusCode.BadRequest, "The message was not valid");
	  }

	  try
	  {
		//If the destination is given, update the existing rule with the new destination.
		if (destination != null)
		{
		  if (!dbDal.AddRuleFed(lookupID, destination))
		  {
			return this.StatusCode(HttpStatusCode.InternalServerError);
		  }
		}
		// Otherwise, attempt to update rule.
		else
		{
		  if (!dbDal.UpdatedRule_FedUri(lookupID, value))
		  {
			return this.StatusCode(HttpStatusCode.InternalServerError);
		  }
		}
	  }
	  catch (ArgumentException e)
	  {
		return Content(HttpStatusCode.BadRequest, "The Federation Rule was not valid.  It was missing a required value or contained an invalid value.");
	  }
	  catch (InvalidOperationException e)
	  {
		return Content(HttpStatusCode.BadRequest, "The Federation Rule was not valid.  It was missing a required value or contained an invalid value.");
	  }
	  catch (NpgsqlException e)
	  {
		return Content(HttpStatusCode.InternalServerError, "Database error occurred");
	  }
	  catch (Exception e)
	  {
		return Content(HttpStatusCode.InternalServerError, "Failed to add the Federation Rule");
	  }
	  return Ok();
	}



	// DELETE: api/Rules/Federation/5
	// DELETE: api/Rules/Federation/5?destination=""
	/// <summary>
	/// Deletes a rule's Federation URI list. If a destination string is specified it deletes the 
	/// specified Federation URI from the rule's Federation URI list
	/// </summary>
	/// <param name="lookupID">ID of the rule to be deleted</param>
	/// <param name="destination">Federation URI to be deleted</param>
	/// <returns>Success or failure</returns>
	[Route("Federation/{lookupID}")]
	[HttpDelete]
	[SwaggerResponse(HttpStatusCode.OK, "Success")]
	[SwaggerResponse(HttpStatusCode.InternalServerError, "An error occurred when deleting the federation rule.")]
	[SwaggerResponse(HttpStatusCode.NotFound, "The Federation rule was not found")]
	public IHttpActionResult Delete(int lookupID, string destination = null)
	{

	  try
	  {
		// If the request includes a destination, only remove the Fed URI from the rule.
		if (destination != null)
		{
		  if (!dbDal.DeletedRuleFed(lookupID, destination))
		  {
			return this.StatusCode(HttpStatusCode.InternalServerError);

		  }
		}
		else
		{
		  if (!dbDal.DeletedAllRuleFed(lookupID))
		  {
			return this.StatusCode(HttpStatusCode.InternalServerError);
		  }
		}
	  }
	  catch (ArgumentNullException e)
	  {
		return Content(HttpStatusCode.InternalServerError, "Failed to delete the Federation Rule(s)");
	  }
	  catch (ArgumentException e)
	  {
		return Content(HttpStatusCode.NotFound, "The Federation rule was not found");
	  }
	  catch (NpgsqlException e)
	  {
		return Content(HttpStatusCode.InternalServerError, "Database error occurred");
	  }
	  catch (Exception e)
	  {
		return Content(HttpStatusCode.InternalServerError, "Failed to delete the Federation Rule(s)");
	  }
	  return Ok();
	}

  }
}
