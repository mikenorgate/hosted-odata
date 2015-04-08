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
using OESoftware.Hosted.OData.Api.DBHelpers;
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

            var dbConnection = DBConnectionFactory.Open(dbIdentifier); //TODO: Get db name;
            var collection = dbConnection.GetCollection<BsonDocument>("_schema");

            var schema = (await (await collection.FindAsync(new BsonDocument())).ToListAsync()).FirstOrDefault();
            if (schema == null) return new EdmModel();

            using (var stringReader = new StringReader(schema.GetElement("value").Value.AsString))
            {
                using (var xmlReader = XmlReader.Create(stringReader))
                {
                    return EdmxReader.Parse(xmlReader);
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

            var dbConnection = DBConnectionFactory.Open(dbIdentifier);
            var collection = dbConnection.GetCollection<BsonDocument>("_schema");

            await collection.DeleteOneAsync(new BsonDocumentFilterDefinition<BsonDocument>(new BsonDocument()));

            var doc = new BsonDocument();
            doc.Add(new BsonElement("_id", new BsonInt32(1)));
            doc.Add(new BsonElement("value", new BsonString(xmlBuilder.ToString())));
            await collection.InsertOneAsync(doc);

            ModelCache.Remove(dbIdentifier);

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