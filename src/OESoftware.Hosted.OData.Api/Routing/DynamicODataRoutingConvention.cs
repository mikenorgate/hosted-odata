using System.Linq;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.OData.Routing;
using System.Web.OData.Routing.Conventions;

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
            return null;
        }

        public string SelectController(ODataPath odataPath, HttpRequestMessage request)
        {
            return (odataPath.Segments.FirstOrDefault() is EntitySetPathSegment) ? _controllerName : null;
        }
    }
}