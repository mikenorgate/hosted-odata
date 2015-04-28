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
    public class ReplaceCommand : IDbCommand
    {
        private IEdmEntityType _entityType;
        private EdmEntityObject _entity;
        private IDictionary<string, object> _keys;

        public ReplaceCommand(IDictionary<string, object> keys, EdmEntityObject entity, IEdmEntityType entityType)
        {
            _entity = entity;
            _entityType = entityType;
            _keys = keys;
        }

        public async Task<EdmEntityObject> Execute(string tenantId)
        {
            using (var bucket = BucketProvider.GetBucket())
            {
                //Convert entity to document
                var originalId = await Helpers.CreateEntityId(tenantId, _keys, _entityType);
                var id = await Helpers.CreateEntityId(tenantId, _keys, _entity, _entityType);

                var converter = new EntityObjectConverter();

                //Get the current version
                var find = bucket.GetDocument<JObject>(originalId);
                if (!find.Success)
                {
                    throw ExceptionCreator.CreateDbException(find);
                }

                var document = await converter.ToDocument(_entity, tenantId, _entityType);

                var replaceDocument = new Document<JObject>()
                {
                    Id = find.Document.Id,
                    Cas = find.Document.Cas,
                    Content = document
                };
                
                //If the key hasn't changed then replace
                if (originalId.Equals(id, StringComparison.InvariantCultureIgnoreCase))
                {
                    var result = bucket.Replace(replaceDocument);
                    if (!result.Success)
                    {
                        throw ExceptionCreator.CreateDbException(result);
                    }
                }
                else
                {
                    //If the key has changed remove the old and insert
                    var remove = bucket.Remove(originalId);
                    if (!remove.Success)
                    {
                        throw ExceptionCreator.CreateDbException(remove);
                    }

                    replaceDocument.Id = id;

                    var result = bucket.Insert(replaceDocument);
                    if (!result.Success)
                    {
                        throw ExceptionCreator.CreateDbException(result);
                    }
                }

                //Convert document back to entity
                var output = await converter.ToEdmEntityObject(find.Content, tenantId, _entityType);

                return output;
            }
        }

        Task IDbCommand.Execute(string tenantId)
        {
            return Execute(tenantId);
        }
    }
}
