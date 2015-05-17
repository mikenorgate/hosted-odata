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
    public class ReplaceSingletonCommand : IDbCommand
    {
        private IEdmSingleton _singleton;
        private EdmEntityObject _entity;
        private IEdmModel _model;

        public ReplaceSingletonCommand(EdmEntityObject entity, IEdmSingleton singleton, IEdmModel model)
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

                var converter = new EntityObjectConverter(new KeyGenerator());

                //Get the current version
                var find = bucket.GetDocument<JObject>(id);

                //TODO: Convert Options
                var document = await converter.ToDocument(_entity, tenantId, _singleton.EntityType(), ConvertOptions.None, _model);

                var replaceDocument = new Document<JObject>()
                {
                    Id = id,
                    Content = document
                };

                if (find.Success)
                {
                    replaceDocument.Cas = find.Document.Cas;
                    var result = bucket.Replace(replaceDocument);
                    if (!result.Success)
                    {
                        throw ExceptionCreator.CreateDbException(result);
                    }

                    //Convert document back to entity
                    var output = converter.ToEdmEntityObject(document, tenantId, _singleton.EntityType());

                    return output;
                }
                else
                {
                    var result = bucket.Insert(replaceDocument);
                    if (!result.Success)
                    {
                        throw ExceptionCreator.CreateDbException(result);
                    }
                    //Convert document back to entity
                    var output = converter.ToEdmEntityObject(document, tenantId, _singleton.EntityType());

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
