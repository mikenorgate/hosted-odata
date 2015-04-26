using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Couchbase;
using Couchbase.Core;

namespace OESoftware.Hosted.OData.Api.Db.Couchbase
{
    public class BucketProvider : IDisposable
    {

        private Cluster _cluster;
        private IBucket _bucket;

        public IBucket GetBucket()
        {
            Dispose();

            _cluster = new Cluster("couchbaseClients/couchbase");
            _bucket = _cluster.OpenBucket();

            return _bucket;
        }

        public void Dispose()
        {
            _bucket?.Dispose();

            _cluster?.Dispose();
        }
    }
}
