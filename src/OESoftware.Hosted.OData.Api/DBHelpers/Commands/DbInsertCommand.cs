using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace OESoftware.Hosted.OData.Api.DBHelpers.Commands
{
    public class DbInsertCommand<T> : IDbCommand<T>
    {
        public string CollectionName { get; private set; }

        public T Document { get; private set; }

        public FilterDefinition<T> FilterDefinition { get; private set; }

        public T Result { get; private set; }

        public DbInsertCommand(string collectionName, T document, FilterDefinition<T> filterDefinition)
        {
            CollectionName = collectionName;
            Document = document;
            FilterDefinition = filterDefinition;
        }

        public async Task Execute(IMongoCollection<T> collection)
        {
            await collection.InsertOneAsync(Document);

            if (FilterDefinition != null)
            {
                Result =
                    (await
                        (await collection.FindAsync(FilterDefinition, new FindOptions<T>() {Limit = 1})).ToListAsync())
                        .FirstOrDefault();
            }
        }
    }
}
