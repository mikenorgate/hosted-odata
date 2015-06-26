using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.OData;
using System.Web.OData.Extensions;
using System.Web.OData.Formatter;
using System.Web.OData.Formatter.Deserialization;
using System.Web.OData.Formatter.Serialization;
using System.Web.OData.Routing;
using Microsoft.OData.Core;
using Microsoft.OData.Edm;
using OESoftware.Hosted.OData.Api.Core;

namespace OESoftware.Hosted.OData.Api.TypeMapping
{
    public class DynamicEntityMediaTypeFormatter : ODataMediaTypeFormatter
    {
        public DynamicEntityMediaTypeFormatter(IEnumerable<ODataPayloadKind> payloadKinds) : base(payloadKinds)
        {
        }

        public DynamicEntityMediaTypeFormatter(ODataDeserializerProvider deserializerProvider, ODataSerializerProvider serializerProvider, IEnumerable<ODataPayloadKind> payloadKinds) : base(deserializerProvider, serializerProvider, payloadKinds)
        {
        }

        public DynamicEntityMediaTypeFormatter(DynamicEntityMediaTypeFormatter formatter, ODataVersion version, HttpRequestMessage request) : base(formatter, version, request)
        {
        }

        public override bool CanReadType(Type type)
        {
            if (type == typeof(IDynamicEntity) || type == typeof(Delta))
            {
                return true;
            }
            return base.CanReadType(type);
        }

        public override Task<object> ReadFromStreamAsync(Type type, Stream readStream, HttpContent content, IFormatterLogger formatterLogger)
        {
            var odataType = GetExpectedPayloadType(Request.ODataProperties().Path);

            var clrType = EdmLibHelpers.GetClrType(odataType, Request.ODataProperties().Model);
            if (clrType != null)
            {
                if (type == typeof (Delta))
                {
                    var deltaType = typeof (Delta<>);
                    type = deltaType.MakeGenericType(clrType);
                }
                else
                {
                    type = clrType;
                }
            }

            return base.ReadFromStreamAsync(type, readStream, content, formatterLogger);
        }

        /// <inheritdoc/>
        public override MediaTypeFormatter GetPerRequestFormatterInstance(Type type, HttpRequestMessage request, MediaTypeHeaderValue mediaType)
        {
            // call base to validate parameters
            base.GetPerRequestFormatterInstance(type, request, mediaType);

            if (Request != null && Request == request)
            {
                // If the request is already set on this formatter, return itself.
                return this;
            }
            else
            {
                ODataVersion version = GetODataResponseVersion(request);
                return new DynamicEntityMediaTypeFormatter(this, version, request);
            }
        }

        private static IEdmTypeReference GetExpectedPayloadType(ODataPath path)
        {
            IEdmTypeReference expectedPayloadType = null;


            // typeless mode. figure out the expected payload type from the OData Path.
            IEdmType edmType = path.EdmType;
            if (edmType != null)
            {
                expectedPayloadType = edmType.ToEdmTypeReference(isNullable: false);
                if (expectedPayloadType.TypeKind() == EdmTypeKind.Collection)
                {
                    IEdmTypeReference elementType = expectedPayloadType.AsCollection().ElementType();
                    if (elementType.IsEntity())
                    {
                        // collection of entities cannot be CREATE/UPDATEd. Instead, the request would contain a single entry.
                        expectedPayloadType = elementType;
                    }
                }
            }

            return expectedPayloadType;
        }
    }
}
