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

        private readonly T _document;

        private readonly FilterDefinition<T> _filterDefinition;

        public T Result { get; private set; }

        public DbInsertCommand(string collectionName, T document, FilterDefinition<T> filterDefinition)
        {
            CollectionName = collectionName;
            _document = document;
            _filterDefinition = filterDefinition;
        }

        public async Task Execute(IMongoCollection<T> collection)
        {
            await collection.InsertOneAsync(_document);

            Result =
                (await (await collection.FindAsync(_filterDefinition, new FindOptions<T>() {Limit = 1})).ToListAsync())
                    .FirstOrDefault();
        }
    }
}
