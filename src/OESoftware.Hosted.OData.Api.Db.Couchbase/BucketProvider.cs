using Couchbase;
using Couchbase.Core;

namespace OESoftware.Hosted.OData.Api.Db.Couchbase
{
    /// <summary>
    /// Get a bucket from the Couchbase cluster
    /// </summary>
    public static class BucketProvider
    {
        private static readonly Cluster Cluster = new Cluster("couchbaseClients/couchbase");
        
        /// <summary>
        /// Get the default bucket
        /// </summary>
        /// <returns><see cref="IBucket"/></returns>
        public static IBucket GetBucket()
        {
            return Cluster.OpenBucket();
        }
    }
}
