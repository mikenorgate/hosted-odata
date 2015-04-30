using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.OData;
using Microsoft.OData.Edm;
using Newtonsoft.Json.Linq;
using OESoftware.Hosted.OData.Api.Core;
using System.Runtime.Caching;
using Microsoft.OData.Edm.Vocabularies.V1;

namespace OESoftware.Hosted.OData.Api.Db.Couchbase
{
    public class EntityObjectConverter
    {
        public async Task<JObject> ToDocument(EdmEntityObject entity, string tenantId, IEdmEntityType entityType, bool generateKeys, IEdmModel model)
        {
            Contract.Requires(model != null);

            var properties =
                entityType.DeclaredProperties.Where(
                    p =>
                        (entityType.NavigationProperties() == null || !entityType.NavigationProperties()
                            .Any(
                                n =>
                                    n.ReferentialConstraint.PropertyPairs.Any(
                                        r =>
                                            r.DependentProperty.Name.Equals(p.Name,
                                                StringComparison.InvariantCultureIgnoreCase))))).ToList();

            return await ToDocumentInternal(entity, tenantId, entityType, properties, generateKeys, model);


        }

        public async Task<JObject> ToDocument(EdmComplexObject entity, string tenantId, IEdmComplexType entityType, bool generateKeys, IEdmModel model)
        {
            Contract.Requires(model != null);

            var properties =
                entityType.DeclaredProperties.ToList();

            return await ToDocumentInternal(entity, tenantId, entityType, properties, generateKeys, model);
        }

        public async Task<EdmEntityObject> ToEdmEntityObject(JObject entity, string tenantId, IEdmEntityType entityType)
        {
            return await Task<EdmEntityObject>.Factory.StartNew(() =>
            {
                var result = new EdmEntityObject(entityType);
                var properties =
                    entityType.DeclaredProperties.Where(
                        p =>
                            (entityType.NavigationProperties() == null || !entityType.NavigationProperties()
                                .Any(
                                    n =>
                                        n.ReferentialConstraint.PropertyPairs.Any(
                                            r =>
                                                r.DependentProperty.Name.Equals(p.Name,
                                                    StringComparison.InvariantCultureIgnoreCase))))).ToList();

                ToEdmEntityObjectInternal(entity, tenantId, entityType, properties, result);

                return result;
            }).ConfigureAwait(false);
        }

        public async Task<EdmComplexObject> ToEdmEntityObject(JObject entity, string tenantId, IEdmComplexType entityType)
        {
            return await Task<EdmComplexObject>.Factory.StartNew(() =>
            {
                var result = new EdmComplexObject(entityType);
                var properties =
                    entityType.DeclaredProperties.ToList();

                ToEdmEntityObjectInternal(entity, tenantId, entityType, properties, result);

                return result;
            }).ConfigureAwait(false);
        }

        private async Task<JObject> ToDocumentInternal(EdmStructuredObject entity, string tenantId, IEdmStructuredType entityType, IList<IEdmProperty> properties, bool generateKeys, IEdmModel model)
        {
            Contract.Requires(model != null);

            var keyGen = new KeyGenerator();
            var result = new JObject();
            foreach (var edmProperty in properties.Where(
                            k =>
                                k.VocabularyAnnotations(model).All(v => v.Term.FullName() != CoreVocabularyConstants.Computed)))
            {
                var property = edmProperty;

                object value;
                entity.TryGetPropertyValue(property.Name, out value);
                var complex = value as EdmComplexObject;
                if (complex != null)
                {
                    var obj = await ToDocument(complex, tenantId, complex.GetEdmType().Definition as IEdmComplexType, generateKeys, model);
                    result.Add(property.Name, obj);
                }
                else
                {
                    result.Add(property.Name, value == null ? null : JToken.FromObject(value));
                }
            }

            if (generateKeys)
            {
                var tasks =
                    (from key in
                        entityType.DeclaredProperties.Where(
                            k =>
                                k.VocabularyAnnotations(model)
                                    .Any(v => v.Term.FullName() == CoreVocabularyConstants.Computed))
                        let key1 = key
                        select
                            keyGen.CreateKey(tenantId, key.Name, key.Type.Definition, entityType)
                                .ContinueWith((task) =>
                                {
                                    entity.TrySetPropertyValue(key1.Name, task.Result);
                                    result[key1.Name] = JToken.FromObject(task.Result);
                                })).ToList
                        ();
                await Task.WhenAll(tasks);
            }

            var dynamicProperties =
                entity.GetChangedPropertyNames()
                    .Where(
                        e =>
                            !entityType.DeclaredProperties.Any(
                                d => d.Name.Equals(e, StringComparison.InvariantCultureIgnoreCase))).ToList();

            foreach (var dynamicMemberName in dynamicProperties)
            {
                if (!entityType.IsOpen)
                {
                    throw new ApplicationException("Dynamic properties not supported on this type");
                }
                object value;
                entity.TryGetPropertyValue(dynamicMemberName, out value);
                result.Add(dynamicMemberName, JToken.FromObject(value));
            }
            
            return result;
        }

        private void ToEdmEntityObjectInternal<T, I>(JObject entity, string tenantId, I entityType, IList<IEdmProperty> properties, T result) where T : EdmStructuredObject where I : IEdmStructuredType
        {
            foreach (var edmProperty in properties)
            {
                var property = edmProperty;

                JToken value;
                if (entity.TryGetValue(property.Name, out value))
                {
                    if (property.Type.Definition is IEdmComplexType)
                    {
                        var obj = ToEdmEntityObject(value as JObject, tenantId, property.Type.Definition as IEdmComplexType).Result;
                        result.TrySetPropertyValue(property.Name, obj);
                    }
                    else
                    {
                        result.TrySetPropertyValue(property.Name,
                            value.ToObject(EdmTypeToClrType.Parse(property.Type.Definition)));
                    }
                }
            }
            var dynamicProperties =
                entity.Properties()
                    .Where(
                        e =>
                            !entityType.DeclaredProperties.Any(
                                d => d.Name.Equals(e.Name, StringComparison.InvariantCultureIgnoreCase))).ToList();
            foreach (var dynamicMemberName in dynamicProperties)
            {
                if (!entityType.IsOpen)
                {
                    throw new ApplicationException("Dynamic properties not supported on this type");
                }
                JToken value;
                if (entity.TryGetValue(dynamicMemberName.Name, out value))
                {
                    result.TrySetPropertyValue(dynamicMemberName.Name, value.ToObject<string>());
                }
            }
        }
    }
}
