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
    public class AddToCollectionCommand : IDbCommand
    {
        private readonly IEdmEntityType _entityType;
        private readonly string _key;

        public AddToCollectionCommand(string key, IEdmEntityType entityType)
        {
            _key = key;
            _entityType = entityType;
        }

        public async Task Execute(string tenantId)
        {
            using (var bucket = BucketProvider.GetBucket())
            {
                var id = Helpers.CreateCollectionId(tenantId, _entityType);
                var existing = bucket.GetDocument<JArray>(id);
                if (!existing.Success)
                {
                    var insertReuslt = bucket.Insert(new Document<JArray>
                    {
                        Id = id,
                        Content = new JArray(_key)
                    });
                    if (!insertReuslt.Success && insertReuslt.Status == ResponseStatus.KeyExists)
                    {
                        //If the insert failed with key exists then another thread got there first
                        //Try again
                        await Execute(tenantId);
                    }
                }
                else
                {
                    var match = existing.Content.FirstOrDefault(j => j.Value<string>().Equals(_key));
                    if (match != null)
                    {
                        //Id is already in collection, nothing to do;
                        return;
                    }
                    existing.Document.Content.Add(_key);
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
