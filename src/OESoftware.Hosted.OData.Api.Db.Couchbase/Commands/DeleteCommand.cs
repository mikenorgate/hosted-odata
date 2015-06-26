// Copyright (C) 2015 Michael Norgate

// This software may be modified and distributed under the terms of 
// the Creative Commons Attribution Non-commercial license.  See the LICENSE file for details.

#region usings

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.OData.Edm;

#endregion

namespace OESoftware.Hosted.OData.Api.Db.Couchbase.Commands
{
    /// <summary>
    /// Delete an entity by key
    /// </summary>
    public class DeleteCommand
    {
        /// <summary>
        /// Execute this command
        /// </summary>
        /// <param name="tenantId">The id of the tenant</param>
        /// <param name="keys">A dictionary of the keys for the entity</param>
        /// <param name="entityType">The type of the collection</param>
        /// <returns>void</returns>
        public async Task Execute(string tenantId, IDictionary<string, object> keys, Type entityType)
        {
            var bucket = BucketProvider.GetBucket();
            var id = await Helpers.CreateEntityId(tenantId, keys, entityType.FullName);

            var result = await bucket.RemoveAsync(id);
            if (!result.Success)
            {
                throw ExceptionCreator.CreateDbException(result);
            }

            var removeFromCollection = new RemoveFromCollectionCommand();
            await removeFromCollection.Execute(tenantId, id, entityType);
        }
    }
}