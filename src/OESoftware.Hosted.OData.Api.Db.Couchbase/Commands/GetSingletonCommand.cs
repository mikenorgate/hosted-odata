using System;
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
using Newtonsoft.Json.Linq;

namespace OESoftware.Hosted.OData.Api.Db.Couchbase.Commands
{
    public class GetSingletonCommand : IDbCommand
    {
        private IEdmSingleton _singleton;

        public GetSingletonCommand(IEdmSingleton singleton)
        {
            _singleton = singleton;
        }

        public async Task<EdmEntityObject> Execute(string tenantId)
        {
            using (var bucket = BucketProvider.GetBucket())
            {
                //Convert entity to document
                var id = Helpers.CreateSingletonId(tenantId, _singleton);

                var result = bucket.Get<JObject>(id);
                if (!result.Success)
                {
                    throw ExceptionCreator.CreateDbException(result);
                }

                var converter = new EntityObjectConverter(new KeyGenerator());
                //Convert document back to entity
                var output = converter.ToEdmEntityObject(result.Value, tenantId, _singleton.EntityType());

                return output;
            }
        }

        

        Task IDbCommand.Execute(string tenantId)
        {
            return Execute(tenantId);
        }
    }
}
