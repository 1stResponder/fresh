using Fresh.PostGIS;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Configuration;
using Fresh.API.Swagger;
using System.Web.Http.Description;
using Swashbuckle.Swagger.Annotations;
using Npgsql;
using System;

namespace Fresh.API.Controllers
{
  /// <summary>
  /// Class:    RulesController
  /// Project:  Fresh.API
  /// Purpose:  This class is used for CRUD operations for rules.
  /// Created:  2016-03-10
  /// Author:   Brian Wilkins - ArdentMC
  /// 
  /// Updates:  none
  /// </summary>

  // setup the base route for the controller
  [RoutePrefix("api")]
  public class RulesController : ApiController
  {
	private PostGISDAL dbDal = new PostGISDAL(ConfigurationManager.ConnectionStrings["FRESH.PostGIS"].ConnectionString,
	  ConfigurationManager.AppSettings["PostGISSchema"], ConfigurationManager.AppSettings["FederationEndpointURL"]);

	// GET: api/Rules
	/// <summary>
	/// Retrieves list of rules in the system
	/// </summary>
	/// <returns>Summary list of rules or failure</returns>
	[Route("Rules")]
	[HttpGet]
	[ResponseType(typeof(RuleDTO))]
	[SwaggerResponse(HttpStatusCode.OK, Type = typeof(RuleDTO))]
	[SwaggerResponse(HttpStatusCode.InternalServerError, "An error occurred when getting the rule.")]
	[SwaggerContentType(ResponseContentType = "text/xml")]
	public HttpResponseMessage Get()
	{

	  try
	  {
		List<RuleDTO> lstRules = new List<RuleDTO>();
		//get all rules
		if (dbDal.ReadRule_All(out lstRules))
		{
		  return Request.CreateResponse(HttpStatusCode.OK, lstRules);
		}
		else
		{
		  return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Error occurred when getting the rules");
		}
	  }
	  catch (NpgsqlException e)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Database error occurred");
	  }
	  catch (Exception e)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to get the rules");
	  }
	}

	// GET: api/Rules/5
	/// <summary>
	/// Retrieves rule by hash
	/// </summary>
	/// <param name="lookupID">ID of the rule to be retrieved</param>
	/// <returns>Summary rule or failure</returns>
	[Route("Rules/Rule/{lookupID}")]
	[HttpGet]
	[SwaggerResponse(HttpStatusCode.OK, Type = typeof(RuleDTO))]
	[SwaggerResponse(HttpStatusCode.InternalServerError, "An error occurred when getting the rule.")]
	[SwaggerResponse(HttpStatusCode.NotFound, "The rule was not found")]
	[SwaggerContentType(ResponseContentType = "text/xml")]
	public HttpResponseMessage Get(int lookupID)
	{

	  try
	  {
		RuleDTO retVal = null;

		//get rule by hash
		if (dbDal.ReadRule(lookupID, out retVal))
		{
		  return Request.CreateResponse(HttpStatusCode.OK, retVal);
		}
		else
		{
		  return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Rule was not found");
		}
	  }
	  catch (NpgsqlException e)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Database error occurred");
	  }
	  catch (Exception e)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to get the rule");
	  }
	}

	// DELETE: api/Rules
	/// <summary>
	/// Deletes all of the rules from the system
	/// </summary>
	/// <returns>Number of rules deleted or failure</returns>
	[Route("Rules")]
	[HttpDelete]
	[SwaggerResponse(HttpStatusCode.OK, "Success")]
	[SwaggerResponse(HttpStatusCode.NotFound, "No rules were found")]
	[SwaggerResponse(HttpStatusCode.InternalServerError, "An error occurred when deleting the rules.")]
	[SwaggerResponse(HttpStatusCode.Conflict, "Unable to delete all of the rules.  At least one DE message is still active.")]
	public HttpResponseMessage Delete()
	{

	  try
	  {
		int iRowsAffected = -1;

		bool wasSuccess = dbDal.DeletedAllRules(out iRowsAffected);

		if (wasSuccess)
		{
		  return Request.CreateResponse(HttpStatusCode.OK, "Number of Rules Deleted:" + iRowsAffected.ToString());
		}
		else if (iRowsAffected == -1)
		{
		  return Request.CreateErrorResponse(HttpStatusCode.NotFound, "No rules found");
		}
		else
		{
		  return Request.CreateErrorResponse(HttpStatusCode.Conflict, "Unable to delete all rules. At least one DE message is still active.");
		}
	  }
	  catch (NpgsqlException e)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Database error occurred");
	  }
	  catch (Exception e)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to delete the rules");
	  }
	  
	}

	// DELETE: api/Rules/5
	/// <summary>
	/// Deletes the specified rule
	/// </summary>
	/// <param name="lookupID">ID of the rule to be deleted</param>
	/// <returns>Success or failure</returns>
	[Route("Rules/Rule/{lookupID?}")]
	[HttpDelete]
	[SwaggerResponse(HttpStatusCode.OK, "Success")]
	[SwaggerResponse(HttpStatusCode.InternalServerError, "An error occurred when deleting the rule.")]
	[SwaggerResponse(HttpStatusCode.NotFound, "The rules was not found")]
	public HttpResponseMessage Delete(int lookupID)
	{
	  try
	  {

		if (!dbDal.DeletedRule(lookupID))
		{
		  return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to delete the rule");
		}
	  }
	  catch (ArgumentException e)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Rule was not found");
	  }
	  catch (NpgsqlException e)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Database error occurred");
	  }
	  catch (Exception e)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to delete the rule");
	  }
	  return Request.CreateResponse(HttpStatusCode.OK, "Success");
	}
  }
}
