using System;
using System.Net.Http;
using System.Web.Http;
using System.Web.OData.Extensions;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Library;
using Microsoft.Owin;
using OESoftware.Hosted.OData.Api;
using OESoftware.Hosted.OData.Api.Routing;
using Owin;

[assembly: OwinStartup(typeof(Startup))]

namespace OESoftware.Hosted.OData.Api
{
    public class Startup
    {
        public const string DynamicODataPath = "DynamicODataPath";
        private const string ODataDataSource = "ODataDataSource";

        public void Configuration(IAppBuilder app)
        {
            var webApiConfiguration = ConfigureWebApi();

            // Use the extension method provided by the WebApi.Owin library:
            app.UseWebApi(webApiConfiguration);
        }

        private HttpConfiguration ConfigureWebApi()
        {
            var config = new HttpConfiguration();
            config.UseDynamicODataRoute("odata", string.Empty, "", GetModelFuncFromRequest());
            config.AddODataQueryFilter();
            return config;
        }

        private static Func<HttpRequestMessage, IEdmModel> GetModelFuncFromRequest()
        {
            return request =>
            {
                string odataPath = request.Properties[DynamicODataPath] as string ?? string.Empty;
                string[] segments = odataPath.Split('/');
                string dataSource = segments[0];
                request.Properties[ODataDataSource] = dataSource;
                IEdmModel model = new EdmModel();//TODO: DataSourceProvider.GetEdmModel(dataSource);
                request.Properties[DynamicODataPath] = string.Join("/", segments, 1, segments.Length - 1);
                return model;
            };
        }
    }
}
