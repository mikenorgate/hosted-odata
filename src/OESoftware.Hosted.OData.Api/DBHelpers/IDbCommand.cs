using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace OESoftware.Hosted.OData.Api.DBHelpers
{
    public interface IDbCommand<T>
    {
        string CollectionName { get; }

        Task Execute(IMongoCollection<T> collection);
    }
}
