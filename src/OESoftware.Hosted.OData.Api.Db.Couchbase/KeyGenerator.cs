using System;
using System.Threading.Tasks;
using Microsoft.OData.Edm;
using OESoftware.Hosted.OData.Api.Core;

namespace OESoftware.Hosted.OData.Api.Db.Couchbase
{
    public class KeyGenerator : IValueGenerator
    {
        public async Task<object> CreateKey(string tenantId, string keyName, IEdmType keyType, IEdmType entityType)
        {
            object value = null;
            switch (keyType.FullTypeName())
            {
                case EdmConstants.EdmInt16TypeName:
                    {
                        value = await CreateInt16Key(tenantId, keyName, keyType, entityType);
                        break;
                    }
                case EdmConstants.EdmInt32TypeName:
                    {
                        value = await CreateInt32Key(tenantId, keyName, keyType, entityType);
                        break;
                    }
                case EdmConstants.EdmInt64TypeName:
                    {
                        value = await CreateInt64Key(tenantId, keyName, keyType, entityType);
                        break;
                    }
                case EdmConstants.EdmDecimalTypeName:
                    {
                        value = await CreateDecimalKey(tenantId, keyName, keyType, entityType);
                        break;
                    }
                case EdmConstants.EdmDoubleTypeName:
                    {
                        value = await CreateDoubleKey(tenantId, keyName, keyType, entityType);
                        break;
                    }
                case EdmConstants.EdmGuidTypeName:
                    {
                        value = await CreateGuidKey(tenantId, keyName, keyType, entityType);
                        break;
                    }
                case EdmConstants.EdmSingleTypeName:
                    {
                        value = await CreateSingleKey(tenantId, keyName, keyType, entityType);
                        break;
                    }
                default:
                    {
                        throw new ApplicationException(string.Format("Unable to compute value of type {0}", keyType.FullTypeName()));
                    }
            }

            return value;
        }

        private async Task<Int16> CreateInt16Key(string tenantId, string keyName, IEdmType type, IEdmType entityType)
        {
            return Convert.ToInt16(await GetNextFromCounters(tenantId, keyName, type, entityType));
        }

        private async Task<Int32> CreateInt32Key(string tenantId, string keyName, IEdmType type, IEdmType entityType)
        {
            return Convert.ToInt32(await GetNextFromCounters(tenantId, keyName, type, entityType));
        }

        private async Task<Int64> CreateInt64Key(string tenantId, string keyName, IEdmType type, IEdmType entityType)
        {
            return Convert.ToInt64(await GetNextFromCounters(tenantId, keyName, type, entityType));
        }

        private async Task<Decimal> CreateDecimalKey(string tenantId, string keyName, IEdmType type, IEdmType entityType)
        {
            return Convert.ToDecimal(await GetNextFromCounters(tenantId, keyName, type, entityType));
        }

        private async Task<Double> CreateDoubleKey(string tenantId, string keyName, IEdmType type, IEdmType entityType)
        {
            return Convert.ToDouble(await GetNextFromCounters(tenantId, keyName, type, entityType));
        }

        private async Task<Guid> CreateGuidKey(string tenantId, string keyName, IEdmType type, IEdmType entityType)
        {
            return Guid.NewGuid();
        }

        private async Task<Single> CreateSingleKey(string tenantId, string keyName, IEdmType type, IEdmType entityType)
        {
            return Convert.ToSingle(await GetNextFromCounters(tenantId, keyName, type, entityType));
        }

        private async Task<ulong> GetNextFromCounters(string tenantId, string keyName, IEdmType type, IEdmType entityType)
        {
            if (tenantId == null)
            {
                throw new ApplicationException("Invalid DB identifier");
            }

            using (var bucket = BucketProvider.GetBucket())
            {
                var id = string.Format("{0}:{1}:Counter:{2}", tenantId, entityType.FullTypeName(), keyName);
                var result = bucket.Increment(id);
                if (!result.Success)
                {
                    if (result.Exception != null)
                    {
                        throw result.Exception;
                    }
                    else
                    {
                        throw new ApplicationException(result.Message);
                    }
                }

                return result.Value;
            }
        }
    }
}