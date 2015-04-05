using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using System.Web.OData.Batch;
using System.Web.OData.Routing;
using System.Web.OData.Routing.Conventions;
using Microsoft.OData.Edm;
using OESoftware.Hosted.OData.Api.Interfaces;

namespace OESoftware.Hosted.OData.Api.Routing
{
    public static class Extensions
    {
        public static void UseDynamicODataRoute(this HttpConfiguration config, string routeName, string routePrefix, string controllerName, IModelProvider modelProvider)
        {
            UseDynamicODataRoute(config, routeName, routePrefix, controllerName, modelProvider, null);
        }

        public static void UseDynamicODataRoute(this HttpConfiguration config, string routeName, string routePrefix, string controllerName, IModelProvider modelProvider,
            ODataBatchHandler batchHandler)
        {
            if (!string.IsNullOrEmpty(routePrefix))
            {
                var prefixLastIndex = routePrefix.Length - 1;
                if (routePrefix[prefixLastIndex] == '/')
                {
                    routePrefix = routePrefix.Substring(0, routePrefix.Length - 1);
                }
            }

            if (batchHandler != null)
            {
                batchHandler.ODataRouteName = routeName;
                var batchTemplate = string.IsNullOrEmpty(routePrefix)
                    ? ODataRouteConstants.Batch
                    : routePrefix + '/' + ODataRouteConstants.Batch;
                config.Routes.MapHttpBatchRoute(routeName + "Batch", batchTemplate, batchHandler);
            }

            IList<IODataRoutingConvention> routingConventions = ODataRoutingConventions.CreateDefault();
            routingConventions = routingConventions.Except(routingConventions.OfType<MetadataRoutingConvention>()).ToList();
            routingConventions.Insert(0, new DynamicODataRoutingConvention(controllerName));
            routingConventions.Insert(1, new ODataMetadataRoutingConvention("MetadataModify"));
            DynamicODataPathRouteConstraint routeConstraint = new DynamicODataPathRouteConstraint(
                modelProvider,
                routeName,
                routingConventions);
            var odataRoute = new DynamicODataRoute(routePrefix, routeConstraint);
            config.Routes.Add(routeName, odataRoute);
        }

    }
}