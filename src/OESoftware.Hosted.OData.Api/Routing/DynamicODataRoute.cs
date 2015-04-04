using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http.Routing;
using System.Web.OData.Routing;

namespace OESoftware.Hosted.OData.Api.Routing
{
    internal class DynamicODataRoute : ODataRoute
    {
        private const string ODataDataSource = "ODataDataSource";

        private static readonly string EscapedHashMark = Uri.HexEscape('#');
        private static readonly string EscapedQuestionMark = Uri.HexEscape('?');

        private readonly bool _canGenerateDirectLink;

        public DynamicODataRoute(string routePrefix, ODataPathRouteConstraint pathConstraint)
            : base(routePrefix, pathConstraint)
        {
            _canGenerateDirectLink = routePrefix != null && RoutePrefix.IndexOf('{') == -1;
        }

        public override IHttpVirtualPathData GetVirtualPath(
            HttpRequestMessage request,
            IDictionary<string, object> values)
        {
            if (values == null || !values.Keys.Contains(HttpRouteKey, StringComparer.OrdinalIgnoreCase))
            {
                return null;
            }

            object odataPathValue;
            if (!values.TryGetValue(ODataRouteConstants.ODataPath, out odataPathValue))
            {
                return null;
            }

            var odataPath = odataPathValue as string;
            if (odataPath != null)
            {
                return GenerateLinkDirectly(request, odataPath) ?? base.GetVirtualPath(request, values);
            }

            return null;
        }

        private HttpVirtualPathData GenerateLinkDirectly(HttpRequestMessage request, string odataPath)
        {
            var configuration = request.GetConfiguration();
            if (configuration == null || !_canGenerateDirectLink)
            {
                return null;
            }

            var dataSource = request.Properties[ODataDataSource] as string;
            var link = CombinePathSegments(RoutePrefix, dataSource);
            link = CombinePathSegments(link, odataPath);
            link = UriEncode(link);

            return new HttpVirtualPathData(this, link);
        }

        private static string CombinePathSegments(string routePrefix, string odataPath)
        {
            return string.IsNullOrEmpty(routePrefix)
                ? odataPath
                : (string.IsNullOrEmpty(odataPath) ? routePrefix : routePrefix + '/' + odataPath);
        }

        private static string UriEncode(string str)
        {
            var escape = Uri.EscapeUriString(str);
            escape = escape.Replace("#", EscapedHashMark);
            escape = escape.Replace("?", EscapedQuestionMark);
            return escape;
        }
    }
}