using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.OData;
using Couchbase;
using Couchbase.Core;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Library;
using Newtonsoft.Json.Linq;

namespace OESoftware.Hosted.OData.Api.Db.Couchbase.Commands
{
    public class GetAllCommand : IDbCommand
    {
        private IEdmEntityType _entityType;

        public GetAllCommand(IEdmEntityType entityType)
        {
            _entityType = entityType;
        }

        public async Task<EdmEntityObjectCollection> Execute(string tenantId)
        {
            using (var bucket = BucketProvider.GetBucket())
            {
                //Convert entity to document
                var id = Helpers.CreateCollectionId(tenantId, _entityType);

                var result = bucket.Get<JArray>(id);
                if (!result.Success)
                {
                    throw ExceptionCreator.CreateDbException(result);
                }

                var all = bucket.Get<JObject>(result.Value.Values<string>().ToList());

                var converter = new EntityObjectConverter();

                var output = new IEdmEntityObject[all.Values.Count];
                var tasks = new Task[all.Values.Count];
                for (var i = 0; i < all.Values.Count; i++)
                {
                    var i1 = i;
                    tasks[i1] =
                        converter.ToEdmEntityObject(all.Values.ElementAt(i1).Value, tenantId, _entityType)
                            .ContinueWith((task) => { output[i1] = task.Result; });
                }

                await Task.WhenAll(tasks);


                return new EdmEntityObjectCollection(new EdmCollectionTypeReference(new EdmCollectionType(new EdmEntityTypeReference(_entityType, false))), output.ToList());
            }
        }



        Task IDbCommand.Execute(string tenantId)
        {
            return Execute(tenantId);
        }
    }
}
