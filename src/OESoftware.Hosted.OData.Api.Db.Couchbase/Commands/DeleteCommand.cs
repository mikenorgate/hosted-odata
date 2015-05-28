// Copyright (C) 2015 Michael Norgate

// This software may be modified and distributed under the terms of 
// the Creative Commons Attribution Non-commercial license.  See the LICENSE file for details.

#region usings

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.OData.Edm;

#endregion

namespace OESoftware.Hosted.OData.Api.Db.Couchbase.Commands
{
    /// <summary>
    /// Delete an entity by key
    /// </summary>
    public class DeleteCommand : IDbCommand
    {
        private readonly IEdmEntityType _entityType;
        private readonly IDictionary<string, object> _keys;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="keys">A dictionary of the keys for the entity</param>
        /// <param name="entityType">The <see cref="IEdmEntityType"/> of the collection</param>
        public DeleteCommand(IDictionary<string, object> keys, IEdmEntityType entityType)
        {
            _keys = keys;
            _entityType = entityType;
        }

        /// <summary>
        /// Execute this command
        /// </summary>
        /// <param name="tenantId">The id of the tenant</param>
        /// <returns>void</returns>
        public async Task Execute(string tenantId)
        {
            using (var bucket = BucketProvider.GetBucket())
            {
                var id = await Helpers.CreateEntityId(tenantId, _keys, _entityType);

                var result = await bucket.RemoveAsync(id);
                if (!result.Success)
                {
                    throw ExceptionCreator.CreateDbException(result);
                }

                var removeFromCollection = new RemoveFromCollectionCommand(id, _entityType);
                await removeFromCollection.Execute(tenantId);
            }
        }
    }
}