using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.OData;
using Microsoft.OData.Edm;
using Newtonsoft.Json.Linq;
using OESoftware.Hosted.OData.Api.Core;

namespace OESoftware.Hosted.OData.Api.Db.Couchbase
{
    public class EntityObjectConverter
    {
        public async Task<JObject> ToDocument(EdmEntityObject entity, string tenantId, IEdmEntityType entityType)
        {
            var result = new JObject();
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
            foreach (var edmProperty in properties)
            {
                var property = edmProperty;

                if (entity.GetChangedPropertyNames().Contains(property.Name))
                {
                    object value;
                    entity.TryGetPropertyValue(property.Name, out value);
                    result.Add(property.Name, JToken.FromObject(value));
                }
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

        public async Task<EdmEntityObject> ToEdmEntityObject(JObject entity, IEdmEntityType entityType)
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
            foreach (var edmProperty in properties)
            {
                var property = edmProperty;

                JToken value;
                if (entity.TryGetValue(property.Name, out value))
                {
                    result.TrySetPropertyValue(property.Name, value.ToObject(EdmTypeToClrType.Parse(property.Type.Definition)));
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

            return result;
        }
    }
}
