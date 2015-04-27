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
    public class DeleteCommand : IDbCommand
    {
        private IEdmEntityType _entityType;
        private IDictionary<string, object> _keys;

        public DeleteCommand(IDictionary<string, object> keys, IEdmEntityType entityType)
        {
            _keys = keys;
            _entityType = entityType;
        }

        public async Task Execute(string tenantId)
        {
            using (var bucket = BucketProvider.GetBucket())
            {
                //Convert entity to document
                var id = CreateId(tenantId);

                var result = bucket.Remove(id);
                if (!result.Success)
                {
                    throw ExceptionCreator.CreateDbException(result);
                }

                var removeFromCollection = new RemoveFromCollectionCommand(id, _entityType);
                await removeFromCollection.Execute(tenantId);
            }
        }

        private string CreateId(string tenantId)
        {
            var values = new List<string>();
            foreach (var property in _entityType.DeclaredKey.OrderBy(k=>k.Name))
            {
                if (!_keys.ContainsKey(property.Name))
                {
                    throw new KeyNotFoundException(string.Format("No value for key {0} was found", property.Name));
                }

                values.Add(_keys[property.Name].ToString());
            }

           return string.Format("{0}:{1}:{2}", tenantId, _entityType.FullTypeName(), string.Join("_", values));
        }

        Task IDbCommand.Execute(string tenantId)
        {
            return Execute(tenantId);
        }
    }
}
