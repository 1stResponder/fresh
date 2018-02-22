// ———————————————————————–
// <copyright file="ValuesController.cs" company="EDXLSharp">
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

using Fresh.API.Swagger;
using Fresh.PostGIS;
using Npgsql;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;

namespace Fresh.API.Controllers
{ 

  /// <summary>
  /// Class:    ValuesController
  /// Project:  Fresh.API
  /// Purpose:  This class is used for CRUD operations for source values
  /// 
  /// Updates:  none
  /// </summary>
  
  [RoutePrefix("api")]
  public class ValuesController : ApiController
  {
    private PostGISDAL dbDal = new PostGISDAL(ConfigurationManager.ConnectionStrings["FRESH.PostGIS"].ConnectionString, 
      ConfigurationManager.AppSettings["PostGISSchema"], ConfigurationManager.AppSettings["FederationEndpointURL"]);

    
    // GET api/values
    /// <summary>
    /// Returns a list of source values in system
    /// </summary>
    /// <returns>Source value list or error</returns>
    [Route("Values")]
    [HttpGet]
    [SwaggerResponse(HttpStatusCode.OK, Type = typeof(SourceValueDTO))]
    [SwaggerResponse(HttpStatusCode.InternalServerError, "An error occurred when getting the values.")]
    [SwaggerResponse(HttpStatusCode.NotFound, "There are no values")]
    [SwaggerContentType(ResponseContentType = "text/xml")]
    public HttpResponseMessage Get()
    {

	  try
	  {
		List<SourceValueDTO> lstValues = new List<SourceValueDTO>();

		bool wasSuccessful = dbDal.ReadSourceValue_AllItems(out lstValues);

		//get all values
		if(wasSuccessful && lstValues.Count > 0)
		{
		  return Request.CreateResponse(HttpStatusCode.OK, lstValues);
		}
		else
		{
		  return Request.CreateErrorResponse(HttpStatusCode.NotFound, "No values were found");
		}
	  }
	  catch (NpgsqlException e)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Database error occurred");
	  }
	  catch (Exception e)
	  {
		return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Error occurred when getting the values");
	  }
	}

    /// <summary>
    /// Deletes all of the source values from the system
    /// </summary>
    /// <returns>Success or failure</returns>
    [Route("Values")]
    [HttpDelete]
    [SwaggerResponse(HttpStatusCode.OK, "Success")]
    [SwaggerResponse(HttpStatusCode.InternalServerError, "An error occurred when deleting the value.")]
    public IHttpActionResult Delete()
    {

	  try
	  {
		//TODO:add consistency check with RULES
		//first make sure no rule corresponding to feedID
		//if (!dbDal.ReadRule_Feed(lookupID.Value, new RuleSourceValueDTO()))
		//{
		if (dbDal.DeletedAllSourceValues())
		{
		  return this.StatusCode(HttpStatusCode.OK);
		}
		else
		{
		  return Content(HttpStatusCode.InternalServerError, "Failed to delete the value");
		}
		//}
	  }
	  catch (NpgsqlException e)
	  {
		return Content(HttpStatusCode.InternalServerError, "Database error occurred");
	  }
	  catch (Exception e)
	  {
		return Content(HttpStatusCode.InternalServerError, "Failed to delete the value");
	  }
    }
  }
}
