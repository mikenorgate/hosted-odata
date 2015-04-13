using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace OESoftware.Hosted.OData.Api.DBHelpers
{
    public class DbCommandExecutor
    {
        public async Task Execute<T>(IEnumerable<IDbCommand<T>> commands, HttpRequestMessage request)
        {
            var dbIdentifier = request.GetOwinEnvironment()["DbId"] as string;
            if (dbIdentifier == null)
            {
                throw new ApplicationException("Invalid DB identifier");
            }
            var dbConnection = DBConnectionFactory.Open(dbIdentifier);

            var tasks = new List<Task>();
            foreach (var commandGroup in commands.GroupBy(c => c.CollectionName))
            {
                var collection = dbConnection.GetCollection<T>(commandGroup.Key);
                commandGroup.ToList().ForEach(c => tasks.Add(c.Execute(collection)));
            }

            await Task.WhenAll(tasks.ToArray());
        }

        public async Task Execute<T>(IDbCommand<T> command, HttpRequestMessage request)
        {
            var dbIdentifier = request.GetOwinEnvironment()["DbId"] as string;
            if (dbIdentifier == null)
            {
                throw new ApplicationException("Invalid DB identifier");
            }
            var dbConnection = DBConnectionFactory.Open(dbIdentifier);

            var collection = dbConnection.GetCollection<T>(command.CollectionName);
            await command.Execute(collection);
        }
    }
}
