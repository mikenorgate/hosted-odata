using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Web.Http.Controllers;
using System.Web.OData.Routing;
using System.Web.OData.Routing.Conventions;
using OESoftware.Hosted.OData.Api.Attributes;

namespace OESoftware.Hosted.OData.Api.Routing
{
    internal class DynamicODataRoutingConvention : IODataRoutingConvention
    {
        private readonly string _controllerName;

        public DynamicODataRoutingConvention(string controllerName)
        {
            _controllerName = controllerName;
        }

        public string SelectAction(
            ODataPath odataPath,
            HttpControllerContext controllerContext,
            ILookup<string, HttpActionDescriptor> actionMap)
        {
            if (odataPath.EdmType == null) return null;

            var method = controllerContext.Controller.GetType().GetMethods()
                .FirstOrDefault(
                    m =>
                        m.GetCustomAttributes(typeof(ODataPathAttribute), false).Length > 0 &&
                        m.GetCustomAttribute<ODataPathAttribute>(false).PathTemplate.Equals(odataPath.PathTemplate, StringComparison.InvariantCultureIgnoreCase) &&
                        m.Name.StartsWith(controllerContext.Request.Method.ToString(), StringComparison.InvariantCultureIgnoreCase));

            return method?.Name;
        }

        public string SelectController(ODataPath odataPath, HttpRequestMessage request)
        {
            return (odataPath.Segments.FirstOrDefault() is EntitySetPathSegment) ? _controllerName : null;
        }
    }
}