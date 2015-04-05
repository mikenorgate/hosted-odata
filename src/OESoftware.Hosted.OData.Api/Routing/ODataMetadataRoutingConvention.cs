using System;
using System.Linq;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.OData.Routing;
using System.Web.OData.Routing.Conventions;

namespace OESoftware.Hosted.OData.Api.Routing
{
    internal class ODataMetadataRoutingConvention : IODataRoutingConvention
    {
        private readonly string _controllerName;

        public ODataMetadataRoutingConvention(string controllerName)
        {
            _controllerName = controllerName;
        }

        public string SelectAction(
            ODataPath odataPath,
            HttpControllerContext controllerContext,
            ILookup<string, HttpActionDescriptor> actionMap)
        {
            if (odataPath == null)
            {
                throw new ArgumentNullException("odataPath");
            }

            if (controllerContext == null)
            {
                throw new ArgumentNullException("controllerContext");
            }

            if (actionMap == null)
            {
                throw new ArgumentNullException("actionMap");
            }

            if (controllerContext.Request.Method == HttpMethod.Get)
            {

                if (odataPath.PathTemplate == "~")
                {
                    return "GetServiceDocument";
                }

                if (odataPath.PathTemplate == "~/$metadata")
                {
                    return "GetMetadata";
                }
            }
            else
            {
                if (actionMap.Contains(controllerContext.Request.Method.ToString()))
                {
                    return controllerContext.Request.Method.ToString();
                }
            }

            return null;
        }

        public string SelectController(ODataPath odataPath, HttpRequestMessage request)
        {
            if(odataPath == null)
            {
                throw new ArgumentNullException("odataPath");
            }

            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            if (odataPath.PathTemplate == "~" ||
                odataPath.PathTemplate == "~/$metadata")
            {
                return _controllerName;
            }

            return null;
        }
    }
}