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
    public class GetCommand : IDbCommand
    {
        private IEdmEntityType _entityType;
        private IDictionary<string, object> _keys;

        public GetCommand(IDictionary<string, object> keys, IEdmEntityType entityType)
        {
            _keys = keys;
            _entityType = entityType;
        }

        public async Task<EdmEntityObject> Execute(string tenantId)
        {
            using (var bucket = BucketProvider.GetBucket())
            {
                //Convert entity to document
                var id = await Helpers.CreateEntityId(tenantId, _keys, _entityType);

                var result = bucket.Get<JObject>(id);
                if (!result.Success)
                {
                    throw ExceptionCreator.CreateDbException(result);
                }

                var converter = new EntityObjectConverter(new KeyGenerator());
                //Convert document back to entity
                var output = converter.ToEdmEntityObject(result.Value, tenantId, _entityType);

                return output;
            }
        }

        

        Task IDbCommand.Execute(string tenantId)
        {
            return Execute(tenantId);
        }
    }
}
