using System;
using System.Collections.Generic;
using System.Data.HashFunction;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.OData;
using Microsoft.OData.Edm;

namespace OESoftware.Hosted.OData.Api.Db.Couchbase
{
    public static class Helpers
    {
        public async static Task<string> CreateEntityId(string tenantId, EdmEntityObject entity, IEdmEntityType entityType)
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

            return string.Format("{0}:{1}:{2}", tenantId, entityType.FullTypeName(), await HashKeyValues(values));
        }

        public async static Task<string> CreateEntityId(string tenantId, IDictionary<string, object> keys, IEdmEntityType entityType)
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

            return string.Format("{0}:{1}:{2}", tenantId, entityType.FullTypeName(), await HashKeyValues(values));
        }

        public async static Task<string> CreateEntityId(string tenantId, IDictionary<string, object> keys, EdmEntityObject entity, IEdmEntityType entityType)
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

            return string.Format("{0}:{1}:{2}", tenantId, entityType.FullTypeName(), await HashKeyValues(values));
        }

        public static string CreateCollectionId(string tenantId, IEdmEntityType entityType)
        {
            return string.Format("{0}:c:{1}", tenantId, entityType.FullTypeName());
        }

        /// <summary>
        /// Create a hash from a list of values
        /// </summary>
        /// <param name="values">The list of values</param>
        /// <returns>A 64bit hash</returns>
        /// <remarks>This is used to hash the keys, this makes sure that long keys wont take us over the 250 byte key limit</remarks>
        public static async Task<string> HashKeyValues(List<string> values)
        {
            var hash = new xxHash(64);
            using (var s = GenerateStreamFromString(string.Join("_", values)))
            {
                return ToHex(await hash.ComputeHashAsync(s));
            }
        }

        private static Stream GenerateStreamFromString(string s)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        public static string ToHex(byte[] bytes)
        {
            StringBuilder result = new StringBuilder(bytes.Length * 2);

            for (int i = 0; i < bytes.Length; i++)
                result.Append(bytes[i].ToString("x2"));

            return result.ToString();
        }
    }
}
