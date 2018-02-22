// ———————————————————————–
// <copyright file="RouteConfig.cs" company="EDXLSharp">
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
//using System.Web.Http.Routing;

namespace Fresh.API
{
  public class RouteConfig
  {
    public static void RegisterRoutes(RouteCollection routes)
    {
      routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

      // enable MVC attribute routes
      routes.MapMvcAttributeRoutes();

	  // Documentation Route
	  routes.MapRoute(
		  name: "DocumentationPg",
		  url: "documentation",
		  defaults: new
		  {
			controller = "Home",
			action = "Documentation",
			id = UrlParameter.Optional
		  },
		  namespaces: new[] { "Fresh.API.Controllers" }
		  );

	  // Swagger Route
	  routes.MapRoute(
		  name: "SwaggerUI",
		  url: "api/swagger",
		  defaults: new
		  {
			controller = "Home",
			action = "Swagger",
			id = UrlParameter.Optional
		  },
		  namespaces: new[] { "Fresh.API.Controllers" }
		  );

	  // Default home page route
	  routes.MapRoute(
          name: "Default",
          url: "{controller}/{action}/{id}",
          defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional },
          namespaces: new[] { "Fresh.API.Controllers" }
          );

	  

	}
  }
}
