// Copyright (C) 2015 Michael Norgate

// This software may be modified and distributed under the terms of 
// the Creative Commons Attribution Non-commercial license.  See the LICENSE file for details.

#region usings

using System;
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
        /// <summary>
        /// Computes a value for the given property
        /// </summary>
        /// <param name="tenantId">The id of the tenant</param>
        /// <param name="propertyName">The name of the property</param>
        /// <param name="propertyType">The type of the property</param>
        /// <param name="entityType">The type of the entity</param>
        /// <returns>Generated value</returns>
        public async Task<object> ComputeValue(string tenantId, string propertyName, IEdmType propertyType, IEdmType entityType)
        {
            object value;
            switch (propertyType.FullTypeName())
            {
                case EdmConstants.EdmInt16TypeName:
                {
                    value = await CreateInt16Key(tenantId, propertyName, entityType);
                    break;
                }
                case EdmConstants.EdmInt32TypeName:
                {
                    value = await CreateInt32Key(tenantId, propertyName, entityType);
                    break;
                }
                case EdmConstants.EdmInt64TypeName:
                {
                    value = await CreateInt64Key(tenantId, propertyName, entityType);
                    break;
                }
                case EdmConstants.EdmDecimalTypeName:
                {
                    value = await CreateDecimalKey(tenantId, propertyName, entityType);
                    break;
                }
                case EdmConstants.EdmDoubleTypeName:
                {
                    value = await CreateDoubleKey(tenantId, propertyName, entityType);
                    break;
                }
                case EdmConstants.EdmGuidTypeName:
                {
                    value = CreateGuidKey();
                    break;
                }
                case EdmConstants.EdmSingleTypeName:
                {
                    value = await CreateSingleKey(tenantId, propertyName, entityType);
                    break;
                }
                default:
                {
                    throw new ApplicationException(string.Format("Unable to compute value of type {0}",
                        propertyType.FullTypeName()));
                }
            }

            return value;
        }

        private async Task<Int16> CreateInt16Key(string tenantId, string keyName, IEdmType entityType)
        {
            return Convert.ToInt16(await GetNextFromCounters(tenantId, keyName, entityType));
        }

        private async Task<Int32> CreateInt32Key(string tenantId, string keyName, IEdmType entityType)
        {
            return Convert.ToInt32(await GetNextFromCounters(tenantId, keyName, entityType));
        }

        private async Task<Int64> CreateInt64Key(string tenantId, string keyName, IEdmType entityType)
        {
            return Convert.ToInt64(await GetNextFromCounters(tenantId, keyName, entityType));
        }

        private async Task<Decimal> CreateDecimalKey(string tenantId, string keyName, IEdmType entityType)
        {
            return Convert.ToDecimal(await GetNextFromCounters(tenantId, keyName, entityType));
        }

        private async Task<Double> CreateDoubleKey(string tenantId, string keyName, IEdmType entityType)
        {
            return Convert.ToDouble(await GetNextFromCounters(tenantId, keyName, entityType));
        }

        private Guid CreateGuidKey()
        {
            return Guid.NewGuid();
        }

        private async Task<Single> CreateSingleKey(string tenantId, string keyName, IEdmType entityType)
        {
            return Convert.ToSingle(await GetNextFromCounters(tenantId, keyName, entityType));
        }

        private async Task<ulong> GetNextFromCounters(string tenantId, string keyName,
            IEdmType entityType)
        {
            if (tenantId == null)
            {
                throw new ApplicationException("Invalid DB identifier");
            }

            using (var bucket = BucketProvider.GetBucket())
            {
                var id = string.Format("{0}:{1}:Counter:{2}", tenantId, entityType.FullTypeName(), keyName);
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
        }
    }
}