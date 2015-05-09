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
    public class UpdateSingletonCommand : IDbCommand
    {
        private IEdmSingleton _singleton;
        private EdmEntityObject _entity;
        private IEdmModel _model;

        public UpdateSingletonCommand(EdmEntityObject entity, IEdmSingleton singleton, IEdmModel model)
        {
            _entity = entity;
            _singleton = singleton;
            _model = model;
        }

        public async Task<EdmEntityObject> Execute(string tenantId)
        {
            using (var bucket = BucketProvider.GetBucket())
            {
                //Convert entity to document
                var id = Helpers.CreateSingletonId(tenantId, _singleton);

                var converter = new EntityObjectConverter();

                //Get the current version
                var find = bucket.GetDocument<JObject>(id);

                var document = await converter.ToDocument(_entity, tenantId, _singleton.EntityType(), false, _model);
                if (!find.Success)
                {
                    find.Document.Content = document;
                    var result = bucket.Insert(find.Document);
                    if (!result.Success)
                    {
                        throw ExceptionCreator.CreateDbException(result);
                    }
                    //Convert document back to entity
                    var output = await converter.ToEdmEntityObject(document, tenantId, _singleton.EntityType());

                    return output;
                }
                else
                {
                    var mergeSettings = new JsonMergeSettings
                    {
                        MergeArrayHandling = MergeArrayHandling.Union,
                    };

                    //Replace the existing values with the new ones
                    find.Content.Merge(document, mergeSettings);

                    var result = bucket.Replace(find.Document);
                    if (!result.Success)
                    {
                        throw ExceptionCreator.CreateDbException(result);
                    }

                    //Convert document back to entity
                    var output = await converter.ToEdmEntityObject(find.Content, tenantId, _singleton.EntityType());

                    return output;
                }
            }
        }

        Task IDbCommand.Execute(string tenantId)
        {
            return Execute(tenantId);
        }
    }
}
