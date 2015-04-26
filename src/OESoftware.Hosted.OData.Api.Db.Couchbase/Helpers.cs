using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.OData;
using Microsoft.OData.Edm;

namespace OESoftware.Hosted.OData.Api.Db.Couchbase
{
    public static class Helpers
    {
        public static string CreateEntityId(string tenantId, EdmEntityObject entity, IEdmEntityType entityType)
        {
            var values = new List<string>();
            foreach (var property in entityType.DeclaredKey.OrderBy(k => k.Name))
            {
                if (
                    !entity.GetChangedPropertyNames()
                        .Any(n => n.Equals(property.Name, StringComparison.InvariantCultureIgnoreCase)))
                {
                    throw new KeyNotFoundException(string.Format("No value for key {0} was found", property.Name));
                }

                object value;
                if (!entity.TryGetPropertyValue(property.Name, out value) || value == null)
                {
                    throw new KeyNotFoundException(string.Format("No value for key {0} was found", property.Name));
                }

                values.Add(value.ToString());
            }

            return string.Format("{0}:{1}:{2}", tenantId, entityType.FullTypeName(), string.Join("_", values));
        }

        public static string CreateEntityId(string tenantId, IDictionary<string, object> keys, IEdmEntityType entityType)
        {
            var values = new List<string>();
            foreach (var property in entityType.DeclaredKey.OrderBy(k => k.Name))
            {
                if (!keys.ContainsKey(property.Name))
                {
                    throw new KeyNotFoundException(string.Format("No value for key {0} was found", property.Name));
                }

                values.Add(keys[property.Name].ToString());
            }

            return string.Format("{0}:{1}:{2}", tenantId, entityType.FullTypeName(), string.Join("_", values));
        }

        public static string CreateEntityId(string tenantId, IDictionary<string, object> keys, EdmEntityObject entity, IEdmEntityType entityType)
        {
            var values = new List<string>();
            foreach (var property in entityType.DeclaredKey.OrderBy(k => k.Name))
            {
                if (
                    !entity.GetChangedPropertyNames()
                        .Any(n => n.Equals(property.Name, StringComparison.InvariantCultureIgnoreCase)))
                {
                    if (!keys.ContainsKey(property.Name))
                    {
                        throw new KeyNotFoundException(string.Format("No value for key {0} was found", property.Name));
                    }
                    values.Add(keys[property.Name].ToString());
                }
                else
                {

                    object value;
                    if (!entity.TryGetPropertyValue(property.Name, out value) || value == null)
                    {
                        throw new KeyNotFoundException(string.Format("No value for key {0} was found", property.Name));
                    }

                    values.Add(value.ToString());
                }
            }

            return string.Format("{0}:{1}:{2}", tenantId, entityType.FullTypeName(), string.Join("_", values));
        }

        public static string CreateCollectionId(string tenantId, IEdmEntityType entityType)
        {
            return string.Format("{0}:c:{1}", tenantId, entityType.FullTypeName());
        }
    }
}
