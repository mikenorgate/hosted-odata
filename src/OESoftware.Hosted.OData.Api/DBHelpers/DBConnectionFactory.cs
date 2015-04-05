using MongoDB.Driver;

namespace OESoftware.Hosted.OData.Api.DBHelpers
{
    public static class DBConnectionFactory
    {
        public static IMongoDatabase Open(string databaseName)
        {
            var connectionString = "mongodb://localhost"; //TODO: Get this from connectionstrings
            var client = new MongoClient(connectionString);
            return client.GetDatabase(databaseName);
        }
    }
}