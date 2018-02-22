using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Fresh.API.Swagger
{

  /// <summary>
  /// Attribute to specify the content type of the request and response objects
  /// This will override the default content types
  /// </summary>
  [AttributeUsage(AttributeTargets.Method)]
  public sealed class SwaggerContentType : Attribute
  {

    #region Constructor

    /// <summary>
    /// Intializes the SwaggerContentType attribute
    /// </summary>
    public SwaggerContentType()
    {
    }

    /// <summary>
    /// Intializes the SwaggerContentType attribute
    /// </summary>
    /// <param name="contentType">The content type for both the response and request</param>
    public SwaggerContentType(string contentType)
    {
      RequestContentType = contentType;
      ResponseContentType = contentType;
    }

    #endregion

    /// <summary>
    /// Holds the Request Content Type
    /// </summary>
    public string RequestContentType { get; set; }

    /// <summary>
    /// Holds the Response Content Type
    /// </summary>
    public string ResponseContentType { get; set; }

  }
}