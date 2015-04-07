using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Xml;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Csdl;
using Microsoft.OData.Edm.Library;
using Microsoft.OData.Edm.Validation;
using MongoDB.Driver;
using OESoftware.Hosted.OData.Api.Models;

namespace OESoftware.Hosted.OData.Api.DBHelpers
{
    public class EdmModelDbUpdates
    {
        public IEnumerable<EdmError> ModelErrors { get; set; }

        public IEnumerable<DbUpdate> Updates { get; set; }

        public class DbUpdate
        {
            public ExpressionFilterDefinition<SchemaElement> FilterDefinition { get; set; }

            public UpdateDefinition<SchemaElement> UpdateDefinition { get; set; }
        }
    }

    public static class EdmModelExtensions
    {
        public static EdmModelDbUpdates ToDbUpdates(this IEdmModel model)
        {

            IEnumerable<EdmError> errors = new List<EdmError>();
            var updates = new List<EdmModelDbUpdates.DbUpdate>();
            foreach (var schema in model.SchemaElements)
            {
                var xmlBuilder = new StringBuilder();
                var tempModel = new EdmModel();
                tempModel.AddElement(schema);
                using (var xmlWriter = XmlWriter.Create(xmlBuilder, new XmlWriterSettings() { Encoding = Encoding.UTF32 }))
                {
                    tempModel.TryWriteCsdl(xmlWriter, out errors);
                }
                var filter = new ExpressionFilterDefinition<SchemaElement>(f => f.Name == schema.Name && f.Namespace == schema.Namespace);
                var update = Builders<SchemaElement>.Update
                    .Set(schemaElement => schemaElement.Csdl, xmlBuilder.ToString())
                    .Set(schemaElement => schemaElement.ContainerName, model.EntityContainer.Name)
                    .Set(schemaElement => schemaElement.ContainerNamespace, model.EntityContainer.Namespace);
                updates.Add(new EdmModelDbUpdates.DbUpdate { FilterDefinition = filter, UpdateDefinition = update });
            }

            return new EdmModelDbUpdates { ModelErrors = errors, Updates = updates };
        }
    }

}