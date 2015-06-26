using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.ExceptionHandling;
using System.Web.Http.Validation;
using System.Web.OData.Extensions;
using System.Web.OData.Formatter;
using System.Web.OData.Formatter.Deserialization;
using System.Web.OData.Formatter.Serialization;
using Microsoft.OData.Core;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Library;
using Microsoft.Owin;
using OESoftware.Hosted.OData.Api;
using OESoftware.Hosted.OData.Api.Middleware;
using OESoftware.Hosted.OData.Api.Models;
using OESoftware.Hosted.OData.Api.Routing;
using Owin;

[assembly: OwinStartup(typeof(Startup))]

namespace OESoftware.Hosted.OData.Api
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.Use(typeof(ApiKeyValidation));
            app.Use(typeof(RequestLogging));

            var webApiConfiguration = ConfigureWebApi();
            // Use the extension method provided by the WebApi.Owin library:
            app.UseWebApi(webApiConfiguration);
        }

        private HttpConfiguration ConfigureWebApi()
        {
            var config = new HttpConfiguration();
            config.MapHttpAttributeRoutes();
            config.UseDynamicODataRoute("odata", string.Empty, "HandleAllOData", new ModelProvider());
            config.AddODataQueryFilter();
            config.Services.Replace(typeof(IBodyModelValidator), new ODataBodyModelValidator());
            config.Services.Replace(typeof(IExceptionLogger), new ExceptionLogging());

            config.EnsureInitialized();
            return config;
        }

        private static ODataMediaTypeFormatter CreateFormatterWithoutMediaTypes(ODataSerializerProvider serializerProvider, ODataDeserializerProvider deserializerProvider, params ODataPayloadKind[] payloadKinds)
        {
            ODataMediaTypeFormatter formatter = new ODataMediaTypeFormatter(deserializerProvider, serializerProvider, payloadKinds);
            AddSupportedEncodings(formatter);
            return formatter;
        }

        private static void AddSupportedEncodings(MediaTypeFormatter formatter)
        {
            formatter.SupportedEncodings.Add(new UTF8Encoding(encoderShouldEmitUTF8Identifier: false,
                throwOnInvalidBytes: true));
            formatter.SupportedEncodings.Add(new UnicodeEncoding(bigEndian: false, byteOrderMark: true,
                throwOnInvalidBytes: true));
        }

        
    }
}
