using Fresh.PostGIS;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using log4net;
using System.Configuration;
using System.Xml;
using System.Xml.Schema;
using System.IO;
using EMS.EDXL.DE.v1_0;
using Fresh.API.Swagger;
using Swashbuckle.Swagger.Annotations;
using Fresh.Global;

namespace Fresh.API.Controllers
{
  [RoutePrefix("api")]
  public class ValidationController : ApiController
  {
	private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

	/// <summary>
	/// Validates the NIEM message against the schema.  Will return success if the NIEM message that came in was valid.  
	/// Otherwise it will return failure. 
	/// </summary>
	/// <returns>Success or failure</returns>
	[Route("Validate")]
	[HttpPost]
	[SwaggerResponse(HttpStatusCode.OK, "The DE message was valid")]
	[SwaggerResponse(HttpStatusCode.InternalServerError, "An error occurred when validating the message.")]
	[SwaggerResponse(HttpStatusCode.BadRequest, "The DE message was invalid")]
	[SwaggerContentType(RequestContentType = "text/xml")]
	public HttpResponseMessage Validate([FromBody]DEv1_0 value)
	{
	  List<string> errorList = null;

	  try
	  {
		Log.Debug("Checking if DE Message is valid");
		string xml = value.ToString();   // Validates DE portion of message and writes to xml 

		bool isValid = Fresh.Global.DEUtilities.ValidateNiemSchema(xml, out errorList);

		if (isValid)
		{
		  DEUtilities.LogMessage("The message is valid", DEUtilities.LogLevel.Info);
		  return Request.CreateResponse(HttpStatusCode.OK, "Message is valid");
		}
		else
		{
		  DEUtilities.LogMessage("The message was not valid", DEUtilities.LogLevel.Info);
		  string schemaErrorString = "";

		  foreach (string er in errorList)
		  {
			schemaErrorString = schemaErrorString + er + "\n";
		  }

		  return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "The DE was invalid: " + schemaErrorString);
		}
	  }
	  catch (IOException Ex)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "The schema files could not be read");
	  }
	  catch (FormatException Ex)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "The schema files could not be parsed");
	  }
	  catch (Exception Ex)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "The message could not be validated");
	  }
	}


  }
}