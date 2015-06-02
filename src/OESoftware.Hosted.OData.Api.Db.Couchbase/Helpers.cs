// Copyright (C) 2015 Michael Norgate

// This software may be modified and distributed under the terms of 
// the Creative Commons Attribution Non-commercial license.  See the LICENSE file for details.

#region usings

using System;
using System.Collections.Generic;
using System.Data.HashFunction;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.OData;
using Microsoft.OData.Edm;

#endregion

namespace OESoftware.Hosted.OData.Api.Db.Couchbase
{
    /// <summary>
    /// Helpers for db operations
    /// </summary>
    public static class Helpers
    {
        /// <summary>
        /// Create a id key for a entity in a collection
        /// </summary>
        /// <param name="tenantId">The id of the tenant</param>
        /// <param name="entity"><see cref="EdmEntityObject"/></param>
        /// <param name="entityType"><see cref="IEdmEntityType"/></param>
        /// <returns>An id key</returns>
        public static async Task<string> CreateEntityId(string tenantId, EdmEntityObject entity,
            IEdmEntityType entityType)
        {
            var values = new List<string>();
            var allKeyProperties = GetAllKeys(entityType);
            foreach (var property in allKeyProperties.OrderBy(k => k.Name))
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

        /// <summary>
        /// Create a id key for a entity in a collection
        /// </summary>
        /// <param name="tenantId">The id of the tenant</param>
        /// <param name="keys">A dictionary of the entity keys</param>
        /// <param name="entityType"><see cref="IEdmEntityType"/></param>
        /// <returns>An id key</returns>
        public static async Task<string> CreateEntityId(string tenantId, IDictionary<string, object> keys,
            IEdmEntityType entityType)
        {
            var values = new List<string>();
            var allKeyProperties = GetAllKeys(entityType);
            foreach (var property in allKeyProperties.OrderBy(k => k.Name))
            {
                if (!keys.ContainsKey(property.Name))
                {
                    throw new KeyNotFoundException(string.Format("No value for key {0} was found", property.Name));
                }

                values.Add(keys[property.Name].ToString());
            }

            return string.Format("{0}:{1}:{2}", tenantId, entityType.FullTypeName(), await HashKeyValues(values));
        }

        /// <summary>
        /// Create a id key for a entity in a collection
        /// Takes the keys from keys if not in entity
        /// </summary>
        /// <param name="tenantId">The id of the tenant</param>
        /// <param name="keys">A dictionary of the entity keys</param>
        /// <param name="entity"><see cref="EdmEntityObject"/></param>
        /// <param name="entityType"><see cref="IEdmEntityType"/></param>
        /// <returns>An id key</returns>
        public static async Task<string> CreateEntityId(string tenantId, IDictionary<string, object> keys,
            EdmEntityObject entity, IEdmEntityType entityType)
        {
            var values = new List<string>();
            var allKeyProperties = GetAllKeys(entityType);
            foreach (var property in allKeyProperties.OrderBy(k => k.Name))
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

        /// <summary>
        /// Create a id key for the collection
        /// </summary>
        /// <param name="tenantId">The id of the tenant</param>
        /// <param name="entityType"><see cref="IEdmEntityType"/></param>
        /// <returns>An id key</returns>
        public static string CreateCollectionId(string tenantId, IEdmEntityType entityType)
        {
            return string.Format("{0}:c:{1}", tenantId, entityType.FullTypeName());
        }

        /// <summary>
        /// Create a id key for the singleton
        /// </summary>
        /// <param name="tenantId">The id of the tenant</param>
        /// <param name="singleton"><see cref="IEdmSingleton"/></param>
        /// <returns>An id key</returns>
        public static string CreateSingletonId(string tenantId, IEdmSingleton singleton)
        {
            return string.Format("{0}:{1}", tenantId, singleton.EntityType().FullTypeName());
        }

        /// <summary>
        ///     Create a hash from a list of values
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
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        public static string ToHex(byte[] bytes)
        {
            var result = new StringBuilder(bytes.Length*2);

            foreach (byte t in bytes)
                result.Append(t.ToString("x2"));

            return result.ToString();
        }

        /// <summary>
        /// Get all the properties for a type
        /// </summary>
        /// <param name="entityType"><see cref="IEdmStructuredType"/></param>
        /// <returns>List of <see cref="IEdmProperty"/></returns>
        private static List<IEdmStructuralProperty> GetAllKeys(IEdmEntityType entityType)
        {
            var properties = new List<IEdmStructuralProperty>();
            if (entityType.DeclaredKey != null)
            {
                properties.AddRange(entityType.DeclaredKey);
            }

            var tempType = entityType;
            while (tempType.BaseType != null)
            {
                tempType = tempType.BaseType as IEdmEntityType;
                if (tempType.DeclaredKey != null)
                {
                    properties.AddRange(tempType.DeclaredKey);
                }
            }

            return properties;
        }
    }
}