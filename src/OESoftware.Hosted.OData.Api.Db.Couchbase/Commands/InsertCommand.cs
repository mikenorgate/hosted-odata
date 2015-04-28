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
    public class InsertCommand : IDbCommand
    {
        private IEdmEntityType _entityType;
        private EdmEntityObject _entity;

        public InsertCommand(EdmEntityObject entity, IEdmEntityType entityType)
        {
            _entity = entity;
            _entityType = entityType;
        }

        public async Task<EdmEntityObject> Execute(string tenantId)
        {
            using (var bucket = BucketProvider.GetBucket())
            {
                //Convert entity to document
                var id = await Helpers.CreateEntityId(tenantId, _entity, _entityType);
                var converter = new EntityObjectConverter();

                var document = await converter.ToDocument(_entity, tenantId, _entityType);

                var result = bucket.Insert(id, document);
                if (!result.Success)
                {
                    throw ExceptionCreator.CreateDbException(result);
                }

                var addToCollection = new AddToCollectionCommand(id, _entityType);
                await addToCollection.Execute(tenantId);

                var find = bucket.Get<JObject>(id);
                if (!find.Success)
                {
                    throw ExceptionCreator.CreateDbException(find);
                }
                //Convert document back to entity
                var output = await converter.ToEdmEntityObject(find.Value, tenantId, _entityType);

                return output;
            }
        }

        Task IDbCommand.Execute(string tenantId)
        {
            return Execute(tenantId);
        }
    }
}
