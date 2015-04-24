using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace OESoftware.Hosted.OData.Api.DBHelpers.Commands
{
    public class DbFindCommand<T> : IDbCommand<T>
    {
        public string CollectionName { get; private set; }
        
        public FilterDefinition<T> FilterDefinition { get; private set; }

        public IAsyncCursor<T> Result { get; private set; }

        public DbFindCommand(string collectionName, FilterDefinition<T> filterDefinition)
        {
            CollectionName = collectionName;
            FilterDefinition = filterDefinition;
        }

        public async Task Execute(IMongoCollection<T> collection)
        {
            Result = await collection.FindAsync(FilterDefinition);
        }
    }
}
