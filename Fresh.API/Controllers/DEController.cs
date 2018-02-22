// ———————————————————————–
// <copyright file="DEController.cs" company="EDXLSharp">
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//    http://www.apache.org/licenses/LICENSE-2.0
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
// </copyright>
// ———————————————————————–

using Fresh.PostGIS;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using EMS.EDXL.DE;
using EMS.EDXL.DE.v1_0;
using log4net;
using System.Configuration;
using Fresh.Global;
using System.Web;
using Fresh.API.Swagger;
using System.Web.Http.Description;
using Swashbuckle.Swagger.Annotations;
using Npgsql;

// Configure log4net using the .config file
[assembly: log4net.Config.XmlConfigurator(Watch = true)]

namespace Fresh.API.Controllers
{
  /// <summary>
  /// Class:    DEController
  /// Project:  Fresh.API
  /// Purpose:  This class is used for CRUD operations for DE messages
  /// 
  /// Updates:  none
  /// </summary>

  [RoutePrefix("api")]
  public class DEController : ApiController
  {
	#region Private Fields

	/// <summary>
	/// Log4net logging object
	/// </summary>
	private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

	// If true, allows all Des to be delete via the endpoint
    private bool allowDeleteAllDEs = Boolean.Parse(ConfigurationManager.AppSettings["AllowDeleteAllDEs"]);

    private bool ArchiveEnabled = Boolean.Parse(ConfigurationManager.AppSettings["ArchiveEnabled"]);
    private PostGISDAL dbDal = new PostGISDAL(ConfigurationManager.ConnectionStrings["FRESH.PostGIS"].ConnectionString,
      ConfigurationManager.AppSettings["PostGISSchema"], ConfigurationManager.AppSettings["FederationEndpointURL"]);
    private ArchiveDAL archiveDal = new ArchiveDAL(ConfigurationManager.ConnectionStrings["fresh.Archive"].ConnectionString,
      ConfigurationManager.AppSettings["ArchiveSchema"]);

	#endregion

	// GET: api/DE
	/// <summary>
	/// Retrieves a light-weight list of what DE messages are in the system
	/// </summary>
	/// <returns>Summary list of DE messages or failure</returns>
	[Route("DE", Name = "All DE")]
    [HttpGet]
    [SwaggerResponse(HttpStatusCode.OK, Type = typeof(DELiteDTO))]
    [SwaggerResponse(HttpStatusCode.InternalServerError, "An error occurred when getting the DE.")]
	[SwaggerResponse(HttpStatusCode.NotFound, "There were no DE messages found")]
	[SwaggerContentType(ResponseContentType = "text/xml")]
    public HttpResponseMessage Get()
    {

	  try
	  {
		List<DELiteDTO> lstDEs = dbDal.ReadDE_Lite(null);

		if (lstDEs != null && lstDEs.Count > 0)
		{
		  return Request.CreateResponse(HttpStatusCode.OK, lstDEs);
		}
		else if (lstDEs != null)
		{
		  return Request.CreateErrorResponse(HttpStatusCode.NotFound, "There were no DEs Found");
		}
		else
		{
		  return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "General error occurred when getting all of the DE");
		}
	  }
	  catch (NpgsqlException Ex)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Database error occurred.");
	  }
	  catch (Exception Ex)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Error Occurred When Getting All the DE");
	  }
		
    }

    // GET: api/DE/5
    /// <summary>
    /// Retrieves the specified DE message from the system
    /// </summary>
    /// <param name="lookupID">Lookup id of the message to return</param>
    /// <returns>DE message or failure</returns>
    [Route("DE/{lookupID}", Name = "GetSingleDERule")]
    [HttpGet]
    [SwaggerResponse(HttpStatusCode.OK, Type = typeof(DEv1_0))]
    [SwaggerResponse(HttpStatusCode.InternalServerError, "Error getting the DE Information")]
    [SwaggerResponse(HttpStatusCode.NotFound, "The DE was not found")]
    [SwaggerContentType(ResponseContentType = "text/xml")]
    public HttpResponseMessage Get(int lookupID)
    {

	  try
	  {
		DEv1_0 de1 = dbDal.ReadDE(lookupID);

		if (de1 != null)
		{
		  return Request.CreateResponse(HttpStatusCode.OK, de1);
		}
		else
		{
		  return Request.CreateErrorResponse(HttpStatusCode.NotFound, "DE: " + lookupID.ToString() + " not found.");
		}
	  }
	  catch (NpgsqlException e)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Database Error Occurred");
	  }
	  catch (ArgumentException e)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.NotFound, "DE: " + lookupID.ToString() + " not found.");
	  }
	  catch (Exception e)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Error occurred when getting the DE with id: " + lookupID.ToString());
	  }
    }

    // POST: api/DE
    /// <summary>
    /// Adds a new DE message to the system
    /// </summary>
    /// <param name="value">DE message</param>
    /// <returns>DE message or failure</returns>
    [Route("DE")]
    [HttpPost]
    [SwaggerResponseRemoveDefaults]
    [SwaggerResponse(HttpStatusCode.Created, Type = typeof(DELiteDTO))]
    [SwaggerResponse(HttpStatusCode.OK, "Successful Update or Cancellation of the DE.")]
    [SwaggerResponse(HttpStatusCode.InternalServerError, "An error occurred during the operation.")]
    [SwaggerResponse(HttpStatusCode.BadRequest, "Unsupported DE Distribution Type Found or the DE message was invalid.")]
    [SwaggerContentType("text/xml")]
    public HttpResponseMessage Post([FromBody]DEv1_0 value)
    {

	  // Not a Valid DE Message
	  if (value == null)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "The message was not a valid format.  The DE could not be created");
	  }

	  string erMsg;

	  if (DEUtilities.ValidateDE(value, out erMsg) == false)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.BadRequest, erMsg);
	  }

	  // Getting DE lookup ID
	  int deID = DEUtilities.ComputeDELookupID(value);

      if (value.DistributionType == TypeValue.Update)
      {
        return Put(deID, value);
      }
      else if (value.DistributionType == TypeValue.Cancel)
      {
        if (this.ArchiveEnabled)
        {
          string clientAddress = HttpContext.Current.Request.UserHostAddress;
          archiveDal.ArchiveDE(value, clientAddress);
        }

        return Delete(deID);
      }
      else if (value.DistributionType == TypeValue.Report)
      {
		try
		{
		  if (dbDal.CreatedDE(value, out deID))
		  {
			DELiteDTO retVal = new DELiteDTO();
			retVal.LookupID = deID;
			retVal.DateTimeSent = value.DateTimeSent;
			retVal.DistributionID = value.DistributionID;
			retVal.SenderID = value.SenderID;
			this.CreatedAtRoute("GetSingleDERule", new
			{
			  lookupID = deID
			}, value);
			if (this.ArchiveEnabled)
			{
			  string clientAddress = HttpContext.Current.Request.UserHostAddress;
			  archiveDal.ArchiveDE(value, clientAddress);
			}
			return Request.CreateResponse(HttpStatusCode.Created, retVal);
		  }
		  else
		  {
			// something went wrong
			return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "The DE could not be created");
		  }
		}
		catch (ArgumentNullException e)
		{
		  return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "The DE was invalid");
		}
		catch (NpgsqlException e)
		{
		  return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Database error occurred");
		}
		catch (FederationException e)
		{
		  return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Error occurred when federating the DE");
		}
		catch (Exception e)
		{
		  return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Error occurred when adding the DE");
		}
      }
      else
      {
        return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Unsupported DE Distribution Type Found");
      }
    }

    // PUT: api/DE/5
    /// <summary>
    /// Updates a DE message
    /// </summary>
    /// <param name="lookupID">Lookup id of the message to update</param>
    /// <param name="value">Updated message</param>
    /// <returns>Success or failure</returns>
    [Route("DE/{lookupID}")]
    [HttpPut]
	[SwaggerResponseRemoveDefaults]
	[SwaggerResponse(HttpStatusCode.OK, "The DE was updated successfully.")]
	[SwaggerResponse(HttpStatusCode.Created, Type = typeof(DELiteDTO), Description ="The DE was created successfully")]
	[SwaggerResponse(HttpStatusCode.InternalServerError, "An error occurred when updating or adding the DE.")]
	[SwaggerResponse(HttpStatusCode.Conflict, "The DE was not valid for an update.  The DE may be missing required components or be out of date (the DE in the db is more recent")]
	[SwaggerResponse(HttpStatusCode.BadRequest, "The message did not contain a valid DE.")]
	[SwaggerContentType("text/xml")]
    public HttpResponseMessage Put(int lookupID, [FromBody]DEv1_0 value)
    {
	  string errMsg; // Holds error message

	  //--- Validating the DE
	  if (DEUtilities.ValidateDE(value, out errMsg) == false)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.BadRequest, errMsg);
	  }

	  //--- Checking if Update or Add

	  // If false we are creating a DE. If true, this is an update.
	  bool deExists;  

	  try
	  {
		deExists = dbDal.DEExists(value);

		if (deExists)
		{
		  DEUtilities.LogMessage(string.Format("The DE with lookupID {0} exists.  This is an update.", lookupID), DEUtilities.LogLevel.Debug);
		}
		else
		{
		  DEUtilities.LogMessage(string.Format("The DE with lookupID {0} does not exist.  We are adding a new DE.", lookupID), DEUtilities.LogLevel.Debug);
		}		
	  }
	  catch (NpgsqlException e)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Database error occurred.");
	  }
	  catch (Exception e)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Error occurred when validating the request.");
	  }

	  try
	  {

		bool wasSuccessful = dbDal.PutDE(value);

		// If the operation was a success
		if (wasSuccessful)
		{
		  if (this.ArchiveEnabled)
		  {
			string clientAddress = HttpContext.Current.Request.UserHostAddress;
			archiveDal.ArchiveDE(value, clientAddress);
		  }

		  // If this was an update
		  if (deExists)
		  {
			return Request.CreateResponse(HttpStatusCode.OK, "Success");
		  }
		  else // If this was an add
		  {
			HttpResponseMessage msg = Request.CreateResponse(HttpStatusCode.Created, value);

			// Getting location for new DE
			string location = "" + HttpContext.Current.Request.Url;

			if (!(location.Contains(""+lookupID)))
			{
			  location = location.TrimEnd('/') + "/" + lookupID;
			}

			msg.Headers.Location = new Uri(location);

			return msg;
		  }

		}
		else // If the operation failed
		{
		  // If this was an update
		  if (deExists)
		  {
			errMsg = "Error occurred when updating the DE";
		  }
		  else // If this was an add
		  {
			errMsg = "Error occurred when adding the DE";
		  }

		  return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, errMsg);
		}

	  }
	  catch (NpgsqlException e)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Database error occurred");
	  }
	  catch (InvalidOperationException e)
	  {
		if (deExists)
		{
		  return Request.CreateErrorResponse(HttpStatusCode.Conflict, "This was not a valid update.  Make sure the updated DE message is valid and the most recent update.");
		}
		else
		{
		  return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to add the DE");
		}

	  }
	  catch (FederationException e)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Error occurred when federating the DE");
	  }
	  catch (Exception e)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to add the DE");
	  }
    }

    // DELETE: api/DE
    // DELETE: api/DE/5
    /// <summary>
    /// Deletes the specified DE message
    /// </summary>
    /// <param name="lookupID">Lookup Id of the message to delete</param>
    /// <returns>Success or failure</returns>
    [Route("DE/{lookupID?}")]
    [HttpDelete]
    [SwaggerResponse(HttpStatusCode.OK, "Success")]
    [SwaggerResponse(HttpStatusCode.InternalServerError, "An error occurred when deleting the DE(s).")]
    [SwaggerResponse(HttpStatusCode.BadRequest, "No look up DE ID was specified for deletion request.")]
    [SwaggerContentType(ResponseContentType = "text/xml")]
    public HttpResponseMessage Delete(int lookupID = 0)
    {
	  try
	  {

		if (lookupID != 0) // Deleting a single DE
		{
		  // Was Successful
		  if (dbDal.DeletedDE(lookupID))
		  {
			return Request.CreateResponse(HttpStatusCode.OK, "Success");
		  }
		  else // Operation Failed
		  {
			return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to delete the DE");
		  }

		}
		else if (allowDeleteAllDEs) // Deleting all of the DE
		{
		  int iRowsAffected = -1;

		  if (dbDal.DeleteAllDE(out iRowsAffected))
		  {
			return Request.CreateResponse(HttpStatusCode.OK, "Number of DE Deleted:" + iRowsAffected.ToString());
		  }
		  else
		  {
			return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to delete all DE");
		  }
		}
		else // If the lookupID was 0 and we are not allowing bulk delete
		{
		  return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "A look up ID is required for this request");
		}

	  }
	  catch (NpgsqlException e)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Database error occurred");
	  }
	  catch (Exception e)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to delete DE(s)");
	  }
	}

    // PUT: api/DE/position/5
    /// <summary>
    /// Updates the position on the specified DE using the information 
    /// passed in the body of the request.
    /// </summary>
    /// <param name="lookupID">The ID of the DE to update.</param>
    /// <param name="value">The information used to update the DE.</param>
    [Route("DE/position/{lookupID}")]
    [HttpPut]
    [SwaggerResponse(HttpStatusCode.OK, "Success")]
    [SwaggerResponse(HttpStatusCode.InternalServerError, "An error occured when updating the DE position.")]
	[SwaggerResponse(HttpStatusCode.NotFound, "The DE could not be found")]
	[SwaggerResponse(HttpStatusCode.Conflict, "The DE was found but was not valid for this operation")]
	[SwaggerResponse(HttpStatusCode.BadRequest, "The message was in an invalid format or the lookup IDs are mismatched.")]
    [SwaggerContentType("text/xml")]
    public HttpResponseMessage PutPosition(int lookupID, [FromBody]DEPositionDTO value)
    {
	  // Not a Valid Message
	  if (value == null)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "The message was not a valid format.");
	  }

	  if (value.LookupID == 0)
      {
        value.LookupID = lookupID;
      }
      else if (value.LookupID != lookupID)
      {
        return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Look Up IDs are mismatched.");
      }

	  try
	  {

		if (!dbDal.UpdateDEPosition(value))
		{
		  return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to update DE position");
		}
		else
		{
		  return Request.CreateResponse(HttpStatusCode.OK, "Success");
		}
	  }
	  catch (NpgsqlException e)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Database error occurred");
	  }
	  catch (InvalidOperationException e)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.Conflict, "The DE was found but was not valid for this operation.  Make sure the DE contains the necesarry information.");
	  }
	  catch (ArgumentNullException e)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "The message was not valid");
	  }
	  catch (ArgumentException e)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.NotFound, "The DE could not be found.");
	  }
	  catch (Exception e)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to update DE position");
	  }
	}


    // GET: api/DE/Search
    /// <summary>
    /// Retrieves list of DE messages that match the search criteria.
    /// </summary>
    /// <param name="searchParams">[Required] The search parameters to use to search for DEs.  
    /// Must contain a FROM parameter that is before the current time and before the TO parameter.</param>
    /// <returns>Summary list of DE messages or failure</returns>
    [Route("DE/Search")]
    [HttpGet]
    [SwaggerResponse(HttpStatusCode.OK, Type = typeof(DELiteDTO))]
    [SwaggerResponse(HttpStatusCode.InternalServerError, "An error occur when searching for the DE(s).")]
    [SwaggerResponse(HttpStatusCode.BadRequest, "The message was not valid.")]
    [SwaggerContentType("text/xml")]
    public HttpResponseMessage Search([FromUri]DESearchDTO searchParams)
    {
      DateTime now = DateTime.Now;

	  // Not a Valid Message
	  if (searchParams == null)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "The message was not a valid format.");
	  }

	  // check for valid params  
	  // check for FROM - it must be present
	  if (!searchParams.DateTimeFrom.HasValue)
      {
        return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "The required FROM parameter is missing.");
      }
      int compareVal = searchParams.DateTimeFrom.Value.CompareTo(now);
      if (compareVal >= 0) // FROM is >= NOW
      {
        return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "The FROM parameter should be earlier than NOW.");
      }
      if (searchParams.DateTimeTo.HasValue)
      {
        // if TO > NOW set TO = NOW
        compareVal = searchParams.DateTimeTo.Value.CompareTo(now);
        if (compareVal > 0) // TO > NOW
        {
          searchParams.DateTimeTo = now;
        }
        compareVal = searchParams.DateTimeFrom.Value.CompareTo(searchParams.DateTimeTo.Value);
        if (compareVal > 0) // FROM is > TO
        {
          return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "The FROM parameter should be earlier than or the same as the TO parameter.");
        }
      }
      else // default the TO DateTime to NOW.
      {
        searchParams.DateTimeTo = now;
      }

	  try
	  {

		// Return type defaults to the lite version
		if ("full".Equals(searchParams.ReturnType, StringComparison.InvariantCultureIgnoreCase)) // full DTOs
		{
		  List<DEFullDTO> lstDEs = dbDal.SearchForDEs(searchParams);
		  if (lstDEs != null)
		  {
			return Request.CreateResponse(HttpStatusCode.OK, lstDEs);
		  }
		  else
		  {
			return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Error occurred when searcing for DE");
		  }
		}
		else  // lite DTOs
		{
		  List<DELiteDTO> lstDEs = dbDal.ReadDE_Lite(searchParams);

		  if (lstDEs != null)
		  {
			return Request.CreateResponse(HttpStatusCode.OK, lstDEs);
		  }
		  else
		  {
			return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Error occurred when searcing for DE");
		  }
		}
	  }
	  catch (NpgsqlException e)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Database error occurred");
	  }
	  catch (Exception e)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Error occurred when searcing for DE");
	  }
    }
  }
}