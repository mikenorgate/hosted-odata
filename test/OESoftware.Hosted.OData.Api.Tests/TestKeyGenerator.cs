using System;
using System.Threading.Tasks;
using Microsoft.OData.Edm;
using OESoftware.Hosted.OData.Api.Db;
using OESoftware.Hosted.OData.Api.Models;

namespace OESoftware.Hosted.OData.Api.Tests
{
    public class TestKeyGenerator : IKeyGenerator
    {
        public async Task<object> CreateKey(string dbIdentifier, string keyName, IEdmType type, IEdmType entityType)
        {
            return await Task<object>.Run(async () =>
            {
                await Task.Delay(100); //Simulate log db call
                object value = null;
                switch (type.FullTypeName())
                {
                    case EdmConstants.EdmInt16TypeName:
                        {
                            value = await CreateInt16Key(dbIdentifier, keyName);
                            break;
                        }
                    case EdmConstants.EdmInt32TypeName:
                        {
                            value = await CreateInt32Key(dbIdentifier, keyName);
                            break;
                        }
                    case EdmConstants.EdmInt64TypeName:
                        {
                            value = await CreateInt64Key(dbIdentifier, keyName);
                            break;
                        }
                    case EdmConstants.EdmDecimalTypeName:
                        {
                            value = await CreateDecimalKey(dbIdentifier, keyName);
                            break;
                        }
                    case EdmConstants.EdmDoubleTypeName:
                        {
                            value = await CreateDoubleKey(dbIdentifier, keyName);
                            break;
                        }
                    case EdmConstants.EdmGuidTypeName:
                        {
                            value = await CreateGuidKey(dbIdentifier, keyName);
                            break;
                        }
                    case EdmConstants.EdmSingleTypeName:
                        {
                            value = await CreateSingleKey(dbIdentifier, keyName);
                            break;
                        }
                    default:
                        {
                            throw new ApplicationException(string.Format("Unable to compute value of type {0}", type.FullTypeName()));
                        }
                }

                return value;
            });
        }

        private async Task<Int16> CreateInt16Key(string dbIdentifier, string keyName)
        {
            return (Int16)1;
        }

        private async Task<Int32> CreateInt32Key(string dbIdentifier, string keyName)
        {
            return (Int32)1;
        }

        private async Task<Int64> CreateInt64Key(string dbIdentifier, string keyName)
        {
            return (Int64)1;
        }

        private async Task<Decimal> CreateDecimalKey(string dbIdentifier, string keyName)
        {
            return (Decimal)1.0;
        }

        private async Task<Double> CreateDoubleKey(string dbIdentifier, string keyName)
        {
            return (Double)1.0;
        }

        private async Task<Guid> CreateGuidKey(string dbIdentifier, string keyName)
        {
            return Guid.NewGuid();
        }

        private async Task<Single> CreateSingleKey(string dbIdentifier, string keyName)
        {
            return (Single)1.0;
        }
    }
}