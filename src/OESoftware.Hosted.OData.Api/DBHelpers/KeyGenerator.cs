using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Microsoft.OData.Edm;
using MongoDB.Bson;
using MongoDB.Driver;
using OESoftware.Hosted.OData.Api.Models;

namespace OESoftware.Hosted.OData.Api.DBHelpers
{
    public class KeyGenerator : IKeyGenerator
    {
        public async Task<object> CreateKey(string dbIdentifier, string keyName, IEdmType type)
        {
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
        }

        private async Task<Int16> CreateInt16Key(string dbIdentifier, string keyName)
        {
            return (Int16)(await GetNextFromCounters(dbIdentifier, keyName, (Int16) 1));
        }

        private async Task<Int32> CreateInt32Key(string dbIdentifier, string keyName)
        {
            return (Int32)(await GetNextFromCounters(dbIdentifier, keyName, (Int32)1));
        }

        private async Task<Int64> CreateInt64Key(string dbIdentifier, string keyName)
        {
            return (Int64)(await GetNextFromCounters(dbIdentifier, keyName, (Int64)1));
        }

        private async Task<Decimal> CreateDecimalKey(string dbIdentifier, string keyName)
        {
            return (Decimal)(await GetNextFromCounters(dbIdentifier, keyName, (Decimal)1.0));
        }

        private async Task<Double> CreateDoubleKey(string dbIdentifier, string keyName)
        {
            return (Double)(await GetNextFromCounters(dbIdentifier, keyName, (Double)1.0));
        }

        private async Task<Guid> CreateGuidKey(string dbIdentifier, string keyName)
        {
            return Guid.NewGuid();
        }

        private async Task<Single> CreateSingleKey(string dbIdentifier, string keyName)
        {
            return (Single)(await GetNextFromCounters(dbIdentifier, keyName, (Single)1.0));
        }

        private async Task<object> GetNextFromCounters(string dbIdentifier, string keyName, object increment)
        {
            if (dbIdentifier == null)
            {
                throw new ApplicationException("Invalid DB identifier");
            }

            var dbConnection = DBConnectionFactory.Open(dbIdentifier);
            var collection = dbConnection.GetCollection<IdCounter>("_counters");

            var filter = new ExpressionFilterDefinition<IdCounter>(f => f.Name == keyName);
            var update = Builders<IdCounter>.Update
                .Inc(f=>f.Counter, increment);

            var counter = await collection.FindOneAndUpdateAsync(filter, update, new FindOneAndUpdateOptions<IdCounter>() {IsUpsert = true, ReturnDocument = ReturnDocument.After});

            return counter.Counter;
        }
    }
}