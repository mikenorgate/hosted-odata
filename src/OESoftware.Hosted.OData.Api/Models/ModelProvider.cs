using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Csdl;
using Microsoft.OData.Edm.Library;
using Microsoft.OData.Edm.Library.Annotations;
using Microsoft.OData.Edm.Validation;
using MongoDB.Bson;
using MongoDB.Driver;
using OESoftware.Hosted.OData.Api.Db.Couchbase;
using OESoftware.Hosted.OData.Api.Interfaces;

namespace OESoftware.Hosted.OData.Api.Models
{
    public class ModelProvider : IModelProvider
    {
        private const string DynamicODataPath = "DynamicODataPath";
        private const string ODataDataSource = "ODataDataSource";
        private static readonly MemoryCache ModelCache = new MemoryCache("ModelCache");

        public async Task<IEdmModel> FromRequest(HttpRequestMessage request)
        {
            string odataPath = request.Properties[DynamicODataPath] as string ?? string.Empty;
            string[] segments = odataPath.Split('/');
            string dataSource = segments[0];
            request.Properties[ODataDataSource] = dataSource;
            request.Properties[DynamicODataPath] = string.Join("/", segments, 1, segments.Length - 1);

            var dbIdentifier = request.GetOwinEnvironment()["DbId"] as string;
            if (dbIdentifier == null)
            {
                throw new ApplicationException("Invalid DB identifier");
            }

            var cached = ModelCache.Get(dbIdentifier) as IEdmModel;
            if (cached != null)
            {
                return cached;
            }

            using (var bucket = BucketProvider.GetBucket("Internal"))
            {
                var schema = bucket.Get<string>(string.Format("Application:Schema:{0}", dbIdentifier));
                if (!schema.Success)
                {
                    return new EdmModel();
                }

                using (var stringReader = new StringReader(schema.Value))
                {
                    using (var xmlReader = XmlReader.Create(stringReader))
                    {
                        var model = EdmxReader.Parse(xmlReader);
                        ModelCache.Add(dbIdentifier, model, new CacheItemPolicy());
                        return model;
                    }
                }
            }
        }

        public async Task<bool> SaveModel(IEdmModel model, HttpRequestMessage request)
        {
            var dbIdentifier = request.GetOwinEnvironment()["DbId"] as string;
            if (dbIdentifier == null)
            {
                throw new ApplicationException("Invalid DB identifier");
            }

            var xmlBuilder = new StringBuilder();
            IEnumerable<EdmError> errors;
            using (var xmlWriter = XmlWriter.Create(xmlBuilder, new XmlWriterSettings() { Encoding = Encoding.UTF32 }))
            {
                EdmxWriter.TryWriteEdmx(model, xmlWriter, EdmxTarget.OData, out errors);
            }

            if (errors.Any())
            {
                return false;
            }

            using (var bucket = BucketProvider.GetBucket("Internal"))
            {
                var id = string.Format("Application:Schema:{0}", dbIdentifier);
                var schema = bucket.GetDocument<string>(id);
                if (!schema.Success)
                {
                    bucket.Insert(id, xmlBuilder.ToString());
                }
                else
                {
                    schema.Document.Content = xmlBuilder.ToString();
                    var replace = bucket.Replace(schema.Document);
                    ModelCache.Remove(dbIdentifier);
                    if (!replace.Success)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public IEdmModel FromXml(string xml, out IEnumerable<EdmError> errors)
        {
            using (var stringReader = new StringReader(xml))
            {
                using (var xmlReader = XmlReader.Create(stringReader))
                {
                    IEdmModel model;
                    return EdmxReader.TryParse(xmlReader, out model, out errors) ? model : null;
                }
            }
        }
    }
}