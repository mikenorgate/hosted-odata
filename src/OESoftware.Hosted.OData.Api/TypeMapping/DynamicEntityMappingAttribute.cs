using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Formatting;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.OData.Formatter;
using System.Web.OData.Formatter.Deserialization;
using System.Web.OData.Formatter.Serialization;
using Microsoft.OData.Core;

namespace OESoftware.Hosted.OData.Api.TypeMapping
{
    public class DynamicEntityMappingAttribute : Attribute, IControllerConfiguration
    {
        public void Initialize(HttpControllerSettings controllerSettings, HttpControllerDescriptor controllerDescriptor)
        {
            MediaTypeFormatterCollection controllerFormatters = controllerSettings.Formatters;
            var firstFormatter =
                (ODataMediaTypeFormatter)controllerFormatters.First(f => f is ODataMediaTypeFormatter);

            //Need to subclass ODataMediaTypeFormatter and change type in read async
            var formatter = CreateFormatterWithoutMediaTypes(DefaultODataSerializerProvider.Instance,
                DefaultODataDeserializerProvider.Instance, ODataPayloadKind.Feed,
                ODataPayloadKind.Entry,
                ODataPayloadKind.Property,
                ODataPayloadKind.EntityReferenceLink,
                ODataPayloadKind.EntityReferenceLinks,
                ODataPayloadKind.Collection,
                ODataPayloadKind.ServiceDocument,
                ODataPayloadKind.Error,
                ODataPayloadKind.Parameter,
                ODataPayloadKind.Delta);
            firstFormatter.SupportedMediaTypes.ToList().ForEach(s => formatter.SupportedMediaTypes.Add(s));
            firstFormatter.MediaTypeMappings.ToList().ForEach(s => formatter.MediaTypeMappings.Add(s));

            controllerFormatters.Insert(0, formatter);
        }

        private static ODataMediaTypeFormatter CreateFormatterWithoutMediaTypes(ODataSerializerProvider serializerProvider, ODataDeserializerProvider deserializerProvider, params ODataPayloadKind[] payloadKinds)
        {
            ODataMediaTypeFormatter formatter = new DynamicEntityMediaTypeFormatter(deserializerProvider, serializerProvider, payloadKinds);
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
