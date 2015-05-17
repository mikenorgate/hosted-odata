using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.OData;
using Couchbase;
using Couchbase.Configuration.Client.Providers;
using Couchbase.Core;
using Microsoft.OData.Edm;
using Newtonsoft.Json.Linq;

namespace OESoftware.Hosted.OData.Api.Db.Couchbase.Commands
{
    public class UpdateCommand : IDbCommand
    {
        private IEdmEntityType _entityType;
        private EdmEntityObject _entity;
        private IDictionary<string, object> _keys;
        private IEdmModel _model;

        public UpdateCommand(IDictionary<string, object> keys, EdmEntityObject entity, IEdmEntityType entityType, IEdmModel model)
        {
            _entity = entity;
            _entityType = entityType;
            _keys = keys;
            _model = model;
        }

        public async Task<EdmEntityObject> Execute(string tenantId)
        {
            using (var bucket = BucketProvider.GetBucket())
            {
                //Convert entity to document
                var originalId = await Helpers.CreateEntityId(tenantId, _keys, _entityType);
                var id = await Helpers.CreateEntityId(tenantId, _keys, _entity, _entityType);

                var converter = new EntityObjectConverter(new KeyGenerator());

                //Get the current version
                var find = bucket.GetDocument<JObject>(originalId);
                if (!find.Success)
                {
                    throw ExceptionCreator.CreateDbException(find);
                }

                //TODO: Convert Options
                var document = await converter.ToDocument(_entity, tenantId, _entityType, ConvertOptions.None, _model);
                var mergeSettings = new JsonMergeSettings
                {
                    MergeArrayHandling = MergeArrayHandling.Union,
                };

                //Replace the existing values with the new ones
                find.Content.Merge(document, mergeSettings);
                
                //If the key hasn't changed then replace
                if (originalId.Equals(id, StringComparison.InvariantCultureIgnoreCase))
                {
                    var result = bucket.Replace(find.Document);
                    if (!result.Success)
                    {
                        throw ExceptionCreator.CreateDbException(result);
                    }
                }
                else
                {
                    //If the key has changed remove the old and insert
                    var remove = bucket.Remove(find.Document.Id, find.Document.Cas);
                    if (!remove.Success)
                    {
                        throw ExceptionCreator.CreateDbException(remove);
                    }

                    find.Document.Id = id;

                    var result = bucket.Insert(find.Document);
                    if (!result.Success)
                    {
                        throw ExceptionCreator.CreateDbException(result);
                    }
                }

                //Convert document back to entity
                var output = converter.ToEdmEntityObject(find.Content, tenantId, _entityType);

                return output;
            }
        }

        Task IDbCommand.Execute(string tenantId)
        {
            return Execute(tenantId);
        }
    }
}
