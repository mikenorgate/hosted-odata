// Copyright (C) 2015 Michael Norgate

// This software may be modified and distributed under the terms of 
// the Creative Commons Attribution Non-commercial license.  See the LICENSE file for details.

#region usings

using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.OData;
using Microsoft.OData.Edm;
using Newtonsoft.Json.Linq;

#endregion

namespace OESoftware.Hosted.OData.Api.Db.Couchbase.Commands
{
    /// <summary>
    /// Get a single element by key
    /// </summary>
    public class GetCommand : IDbCommand
    {
        private readonly IEdmEntityType _entityType;
        private readonly IDictionary<string, object> _keys;

        /// <summary>
        /// Default Constructor
        /// </summary>
        /// <param name="keys">A dictionary of the keys for the entity</param>
        /// <param name="entityType">The <see cref="IEdmEntityType"/> of the collection</param>
        public GetCommand(IDictionary<string, object> keys, IEdmEntityType entityType)
        {
            _keys = keys;
            _entityType = entityType;
        }

        Task IDbCommand.Execute(string tenantId)
        {
            return Execute(tenantId);
        }

        /// <summary>
        /// Execute this command
        /// </summary>
        /// <param name="tenantId">The id of the tenant</param>
        /// <returns><see cref="EdmEntityObject"/></returns>
        public async Task<EdmEntityObject> Execute(string tenantId)
        {
            using (var bucket = BucketProvider.GetBucket())
            {
                //Convert entity to document
                var id = await Helpers.CreateEntityId(tenantId, _keys, _entityType);

                var result = await bucket.GetAsync<JObject>(id);
                if (!result.Success)
                {
                    throw ExceptionCreator.CreateDbException(result);
                }

                var converter = new EntityObjectConverter(new ValueGenerator());
                //Convert document back to entity
                var output = converter.ToEdmEntityObject(result.Value, tenantId, _entityType);

                return output;
            }
        }
    }
}