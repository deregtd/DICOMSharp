using DICOMSharp.Util;
using Microsoft.AspNet.WebApi.Extensions.Compression.Server;
using Newtonsoft.Json.Serialization;
using PSWebsite.Utilities;
using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Net.Http.Extensions.Compression.Core.Compressors;
using System.Net.Http.Headers;
using System.Reflection;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;

namespace PSWebsite
{
    public class MvcApplication : HttpApplication
    {
        protected void Application_Start()
        {
            RegisterRoutes();

            GlobalFilters.Filters.Add(new HandleErrorAttribute());

            // This makes Json the default response for the browser
            GlobalConfiguration.Configuration.Formatters.JsonFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/html"));

            // Use camel case for JSON data.
            GlobalConfiguration.Configuration.Formatters.JsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();

            // Mark config complete since otherwise web api chokes
            GlobalConfiguration.Configuration.EnsureInitialized();

            // Start the DICOM server -- it may or may not start listening, depending on settings
            var dicomServer = PSUtils.GetDicomServer();
            dicomServer.LoadSettings();
            dicomServer.StartServer();
        }

        protected void Application_End()
        {
            var dicomServer = PSUtils.GetDicomServer();
            if (dicomServer != null)
            {
                dicomServer.StopServer();
            }

            PSUtils.GetDb().Close();
        }

        void Application_Error(object sender, EventArgs e)
        {
            var logger = PSUtils.GetLogger();
            logger.Log(DICOMSharp.Logging.LogLevel.Error, "Exception in Application_Error: " + e.ToString());
        }

        static private Assembly GetWebEntryAssembly()
        {
            if (System.Web.HttpContext.Current == null ||
                System.Web.HttpContext.Current.ApplicationInstance == null)
            {
                return null;
            }

            var type = System.Web.HttpContext.Current.ApplicationInstance.GetType();
            while (type != null && type.Namespace == "ASP")
            {
                type = type.BaseType;
            }

            return type == null ? null : type.Assembly;
        }

        public static void RegisterRoutes()
        {
            // Web API routes
            GlobalConfiguration.Configuration.MapHttpAttributeRoutes();
            
            var dbSettings = (NameValueCollection)ConfigurationManager.GetSection("serverSettings");
            if (dbSettings != null)
            {
                var compressionEnabled = false;
                if (bool.TryParse(dbSettings["EnableCompression"], out compressionEnabled) && compressionEnabled)
                {
                    GlobalConfiguration.Configuration.MessageHandlers.Insert(0, new ServerCompressionHandler(new GZipCompressor(), new DeflateCompressor()));
                }
            }



            GlobalConfiguration.Configuration.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{Action}",
                defaults: new { Action = "HttpVerb", id = RouteParameter.Optional }
            );

            // MVC routes
            RouteTable.Routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            RouteTable.Routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}",
                defaults: new { controller = "Home", action = "Index" }
            );
        }
    }
}
