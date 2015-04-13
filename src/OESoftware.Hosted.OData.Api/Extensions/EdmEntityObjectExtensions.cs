using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Library;
using MongoDB.Bson;
using OESoftware.Hosted.OData.Api.DBHelpers;

namespace OESoftware.Hosted.OData.Api.Extensions
{
    public static class EdmEntityObjectExtensions
    {
        public static async Task<bool> SetComputedKeys(this EdmEntityObject obj, IEdmModel model, HttpRequestMessage request)
        {
            var type = (IEdmEntityType)model.FindDeclaredType(obj.ActualEdmType.FullTypeName());
            var computedKeys =
                type.DeclaredKey.Where(
                    k =>
                        k.VocabularyAnnotations(model)
                            .Any(
                                v =>
                                    v.Term.FullName() ==
                                    Microsoft.OData.Edm.Vocabularies.V1.CoreVocabularyConstants.Computed));

            var gen = new KeyGenerator();

            foreach (var key in computedKeys)
            {
                var keyName = string.Format("{0}.{1}", type.FullName(), key.Name);
                var value = await gen.CreateKey(request, keyName, key.Type.Definition);

                if (!obj.TrySetPropertyValue(key.Name, value))
                {
                    throw new ApplicationException(string.Format("Failed to set computed key {0} for {1}", value, key.Name));
                }
            }

            return true;
        }

        public static BsonDocument ToInsertDocument(this EdmEntityObject obj, IEdmEntityType entityType)
        {
            var keyName = "";
            var edmStructuralProperty = entityType.Key().FirstOrDefault();
            if (edmStructuralProperty != null)
            {
                keyName = edmStructuralProperty.Name;
            }

            var doc = new BsonDocument();
            //Put each property into BsonDocument
            foreach (var property in entityType.DeclaredStructuralProperties())
            {
                object value;
                if (!obj.TryGetPropertyValue(property.Name, out value))
                {
                    value = property.DefaultValueString;
                }
                var name = property.Name;
                //If this is the key move it to _id 
                //TODO: Support of multiple keys
                if (name.Equals(keyName, StringComparison.InvariantCultureIgnoreCase))
                {
                    name = "_id";
                }
                doc.Add(new BsonElement(name, BsonValue.Create(value)));
            }
            //Each navigation property which doesn't have a link already in the document
            //Create an empty array to hold the ids
            foreach (var property in entityType.DeclaredNavigationProperties())
            {
                if (property.ReferentialConstraint == null)
                {
                    doc.Add(new BsonElement(property.Name, new BsonArray()));
                }
                //Need to ignore any property which is linked to by a ReferentialConstraint
                else
                {
                    property.ReferentialConstraint.PropertyPairs.ToList().ForEach(r => doc.Remove(r.DependentProperty.Name));
                }
            }

            if (entityType.IsOpen)
            {
                foreach (var property in obj.TryGetDynamicProperties())
                {
                    doc.Add(new BsonElement(property.Key, BsonValue.Create(property.Value)));
                }
            }

            return doc;
        }
        
    }
}