using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Caching;
using System.Text.RegularExpressions;
using System.Web.Http.Controllers;
using System.Web.OData.Routing;
using System.Web.OData.Routing.Conventions;
using OESoftware.Hosted.OData.Api.Attributes;

namespace OESoftware.Hosted.OData.Api.Routing
{
    internal class DynamicODataRoutingConvention : IODataRoutingConvention
    {
        private readonly string _controllerName;
        private static readonly MemoryCache ActionCache = new MemoryCache("ActionCache");

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

            if (ActionCache.Contains(odataPath.PathTemplate))
            {
                return ActionCache.Get(odataPath.PathTemplate).ToString();
            }

            var method = controllerContext.Controller.GetType().GetMethods()
                .FirstOrDefault(
                    m =>
                        m.GetCustomAttributes(typeof(ODataPathAttribute), false).Length > 0 &&
                        Regex.IsMatch(odataPath.PathTemplate, string.Format("^{0}$",m.GetCustomAttribute<ODataPathAttribute>(false).PathTemplate)) &&
                        m.Name.StartsWith(controllerContext.Request.Method.ToString(), StringComparison.InvariantCultureIgnoreCase));

            if (method != null)
            {
                ActionCache.Add(string.Format("{0}:{1}",controllerContext.Request.Method, odataPath.PathTemplate), method.Name, new CacheItemPolicy());
            }

            return method?.Name;
        }

        public string SelectController(ODataPath odataPath, HttpRequestMessage request)
        {
            return (odataPath.Segments.FirstOrDefault() is EntitySetPathSegment) ? _controllerName : null;
        }
    }
}