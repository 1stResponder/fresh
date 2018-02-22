using System.Web.Mvc;

namespace Fresh.API.Areas.DE_HTML
{
    public class DE_HTMLAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "DE_HTML";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
      
            context.MapRoute(
                "DE_HTML_default",
                "DE_HTML/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}