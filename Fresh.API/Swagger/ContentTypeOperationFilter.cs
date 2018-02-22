using Swashbuckle.Swagger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Http.Description;

namespace Fresh.API.Swagger
{
  /// <summary>
  /// Custom filter.  Allows us to specify xml as our response type
  /// </summary>
  public class ContentTypeOperationFilter : IOperationFilter
  {

    /// <summary>
    /// Applies the filter
    /// </summary>
    /// <param name="operation">The Operation</param>
    /// <param name="schemaRegistry">The Schmea Registry</param>
    /// <param name="apiDescription">The API Description</param>
    public void Apply(Operation operation, SchemaRegistry schemaRegistry, ApiDescription apiDescription)
    {
      ApplyContentAttribute(ref operation, schemaRegistry, apiDescription);
    }

    private void ApplyContentAttribute(ref Operation operation, SchemaRegistry schemaRegistry, ApiDescription apiDescription)
    {
      // Applying Content Type Attribute
      var attributes = apiDescription.GetControllerAndActionAttributes<SwaggerContentType>().FirstOrDefault();

      if (attributes != null)
      {

        if (attributes.RequestContentType != null)
        {

          if(!string.IsNullOrWhiteSpace(attributes.RequestContentType))
          {
            operation.consumes.Clear();
            operation.consumes.Add(attributes.RequestContentType);
          }
          else
          {
            throw new ArgumentException("Invalid Content Type for Request");
          }

          
        }

        if (attributes.ResponseContentType != null)
        {

          if (!string.IsNullOrWhiteSpace(attributes.ResponseContentType))
          {
            operation.produces.Clear();
            operation.produces.Add(attributes.ResponseContentType);
          }
          else
          {
            throw new ArgumentException("Invalid Content Type for Response");
          }
          
        }

      }

    }
  }
}