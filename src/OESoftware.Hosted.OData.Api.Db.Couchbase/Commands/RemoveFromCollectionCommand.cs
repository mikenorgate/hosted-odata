using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.OData;
using Couchbase;
using Couchbase.IO;
using Microsoft.OData.Edm;
using Newtonsoft.Json.Linq;

namespace OESoftware.Hosted.OData.Api.Db.Couchbase.Commands
{
    public class RemoveFromCollectionCommand : IDbCommand
    {
        private IEdmEntityType _entityType;
        private string _key;

        public RemoveFromCollectionCommand(string key, IEdmEntityType entityType)
        {
            _key = key;
            _entityType = entityType;
        }

        public async Task Execute(string tenantId)
        {
            using (var provider = new BucketProvider())
            {
                var bucket = provider.GetBucket();
                var id = Helpers.CreateCollectionId(tenantId, _entityType);
                var existing = bucket.GetDocument<JArray>(id);
                if (existing.Success)
                {
                    var match = existing.Content.FirstOrDefault(j => j.Value<string>().Equals(_key));
                    if (match == null)
                    {
                        //Id is not in collection, nothing to do;
                        return;
                    }
                    existing.Document.Content.Remove(match);
                    var updateResult = bucket.Replace(existing.Document);
                    if (!updateResult.Success)
                    {
                        //If the update failed then another thread got there first
                        //Try again
                        await Execute(tenantId);
                    }
                }
            }
        }
    }
}
