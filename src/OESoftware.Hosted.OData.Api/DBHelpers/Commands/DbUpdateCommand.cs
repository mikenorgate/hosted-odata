using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace OESoftware.Hosted.OData.Api.DBHelpers.Commands
{
    public class DbUpdateCommand<T> : IDbCommand<T>
    {
        public string CollectionName { get; private set; }

        private readonly UpdateDefinition<T> _updateBuilder;

        private readonly FilterDefinition<T> _filterDefinition;

        public DbUpdateCommand(string collectionName, FilterDefinition<T> filterDefinition, UpdateDefinition<T> updateBuilder)
        {
            CollectionName = collectionName;
            _updateBuilder = updateBuilder;
            _filterDefinition = filterDefinition;
        }

        public async Task Execute(IMongoCollection<T> collection)
        {
            await collection.UpdateOneAsync(_filterDefinition, _updateBuilder);
        }
    }
}
