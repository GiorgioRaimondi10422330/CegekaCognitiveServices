using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace FaceSentiment.Dynamic.WebApp
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
            config.Routes.MapHttpRoute(
                name: "GetApi",
                routeTemplate: "api/{controller}/{action}/{id}",
                defaults: new { controller="Camera",action= "GetVideoContent", id = RouteParameter.Optional }
            );
        }
    }
}
