using System;
using System.Threading.Tasks;
using Microsoft.OData.Edm;
using OESoftware.Hosted.OData.Api.Core;

namespace OESoftware.Hosted.OData.Api.Db.Couchbase
{
    public class KeyGenerator : IKeyGenerator
    {
        public async Task<object> CreateKey(string tenantId, string keyName, IEdmType type)
        {
            object value = null;
            switch (type.FullTypeName())
            {
                case EdmConstants.EdmInt16TypeName:
                    {
                        value = await CreateInt16Key(tenantId, keyName, type);
                        break;
                    }
                case EdmConstants.EdmInt32TypeName:
                    {
                        value = await CreateInt32Key(tenantId, keyName, type);
                        break;
                    }
                case EdmConstants.EdmInt64TypeName:
                    {
                        value = await CreateInt64Key(tenantId, keyName, type);
                        break;
                    }
                case EdmConstants.EdmDecimalTypeName:
                    {
                        value = await CreateDecimalKey(tenantId, keyName, type);
                        break;
                    }
                case EdmConstants.EdmDoubleTypeName:
                    {
                        value = await CreateDoubleKey(tenantId, keyName, type);
                        break;
                    }
                case EdmConstants.EdmGuidTypeName:
                    {
                        value = await CreateGuidKey(tenantId, keyName, type);
                        break;
                    }
                case EdmConstants.EdmSingleTypeName:
                    {
                        value = await CreateSingleKey(tenantId, keyName, type);
                        break;
                    }
                default:
                    {
                        throw new ApplicationException(string.Format("Unable to compute value of type {0}", type.FullTypeName()));
                    }
            }

            return value;
        }

        private async Task<Int16> CreateInt16Key(string tenantId, string keyName, IEdmType type)
        {
            return Convert.ToInt16(await GetNextFromCounters(tenantId, keyName, type));
        }

        private async Task<Int32> CreateInt32Key(string tenantId, string keyName, IEdmType type)
        {
            return Convert.ToInt32(await GetNextFromCounters(tenantId, keyName, type));
        }

        private async Task<Int64> CreateInt64Key(string tenantId, string keyName, IEdmType type)
        {
            return Convert.ToInt64(await GetNextFromCounters(tenantId, keyName, type));
        }

        private async Task<Decimal> CreateDecimalKey(string tenantId, string keyName, IEdmType type)
        {
            return Convert.ToDecimal(await GetNextFromCounters(tenantId, keyName, type));
        }

        private async Task<Double> CreateDoubleKey(string tenantId, string keyName, IEdmType type)
        {
            return Convert.ToDouble(await GetNextFromCounters(tenantId, keyName, type));
        }

        private async Task<Guid> CreateGuidKey(string tenantId, string keyName, IEdmType type)
        {
            return Guid.NewGuid();
        }

        private async Task<Single> CreateSingleKey(string tenantId, string keyName, IEdmType type)
        {
            return Convert.ToSingle(await GetNextFromCounters(tenantId, keyName, type));
        }

        private async Task<ulong> GetNextFromCounters(string tenantId, string keyName, IEdmType type)
        {
            if (tenantId == null)
            {
                throw new ApplicationException("Invalid DB identifier");
            }

            using (var bucket = BucketProvider.GetBucket())
            {
                var id = string.Format("{0}:{1}:Counter:{2}", tenantId, type.FullTypeName(), keyName);
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