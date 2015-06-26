// Copyright (C) 2015 Michael Norgate

// This software may be modified and distributed under the terms of 
// the Creative Commons Attribution Non-commercial license.  See the LICENSE file for details.

#region usings

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.OData.Edm;
using OESoftware.Hosted.OData.Api.Core;

#endregion

namespace OESoftware.Hosted.OData.Api.Db.Couchbase
{
    /// <summary>
    /// Compute a value
    /// </summary>
    public class ValueGenerator : IValueGenerator
    {

        private IDictionary<Type, Func<string, string, string, Task<object>>> _typeValueGenerators = new Dictionary<Type, Func<string, string, string, Task<object>>>()
        {
            { typeof(Int16), CreateInt16Key },
            { typeof(Int32), CreateInt32Key },
            { typeof(Int64), CreateInt64Key },
            { typeof(Decimal), CreateDecimalKey },
            { typeof(Double), CreateDoubleKey },
            { typeof(Guid), CreateGuidKey },
            { typeof(Single), CreateSingleKey },
        };


        /// <summary>
        /// Computes a value for the given property
        /// </summary>
        /// <param name="tenantId">The id of the tenant</param>
        /// <param name="entity"><see cref="IDynamicEntity"/></param>
        public async Task ComputeValues(string tenantId, IDynamicEntity entity)
        {
            var computedProperties = entity.GetComputedProperties();
            foreach (var computedProperty in computedProperties)
            {
                var value =
                    await
                        ComputeValue(tenantId, computedProperty.Name, computedProperty.PropertyType,
                            entity.GetType().FullName);
                entity.SetProperty(computedProperty.Name, value);
            }
        }

        /// <summary>
        /// Computes a value for the given property
        /// </summary>
        /// <param name="tenantId">The id of the tenant</param>
        /// <param name="propertyName">The name of the property</param>
        /// <param name="propertyType">The type of the property</param>
        /// <param name="entityType">The type of the entity</param>
        /// <returns>Generated value</returns>
        public async Task<object> ComputeValue(string tenantId, string propertyName, Type propertyType, string entityType)
        {
            if (!_typeValueGenerators.ContainsKey(propertyType))
            {
                throw new ApplicationException(string.Format("Unable to compute value of type {0}",
                       propertyType.FullName));
            }
            return await _typeValueGenerators[propertyType].Invoke(tenantId, propertyName, entityType);
        }

        private static async Task<object> CreateInt16Key(string tenantId, string keyName, string entityType)
        {
            return Convert.ToInt16(await GetNextFromCounters(tenantId, keyName, entityType));
        }

        private static async Task<object> CreateInt32Key(string tenantId, string keyName, string entityType)
        {
            return Convert.ToInt32(await GetNextFromCounters(tenantId, keyName, entityType));
        }

        private static async Task<object> CreateInt64Key(string tenantId, string keyName, string entityType)
        {
            return Convert.ToInt64(await GetNextFromCounters(tenantId, keyName, entityType));
        }

        private static async Task<object> CreateDecimalKey(string tenantId, string keyName, string entityType)
        {
            return Convert.ToDecimal(await GetNextFromCounters(tenantId, keyName, entityType));
        }

        private static async Task<object> CreateDoubleKey(string tenantId, string keyName, string entityType)
        {
            return Convert.ToDouble(await GetNextFromCounters(tenantId, keyName, entityType));
        }

        private static async Task<object> CreateGuidKey(string tenantId, string keyName, string entityType)
        {
            return Guid.NewGuid();
        }

        private static async Task<object> CreateSingleKey(string tenantId, string keyName, string entityType)
        {
            return Convert.ToSingle(await GetNextFromCounters(tenantId, keyName, entityType));
        }

        private static async Task<ulong> GetNextFromCounters(string tenantId, string keyName,
            string entityType)
        {
            if (tenantId == null)
            {
                throw new ApplicationException("Invalid DB identifier");
            }

            var bucket = BucketProvider.GetBucket();
            var id = string.Format("{0}:{1}:Counter:{2}", tenantId, entityType, keyName);
            var result = await bucket.IncrementAsync(id);
            if (!result.Success)
            {
                if (result.Exception != null)
                {
                    throw result.Exception;
                }
                throw new ApplicationException(result.Message);
            }

            return result.Value;
        }

        public Task<object> ComputeValue(string tenantId, string propertyName, IEdmType propertyType, IEdmType entityType)
        {
            throw new NotImplementedException();
        }
    }
}