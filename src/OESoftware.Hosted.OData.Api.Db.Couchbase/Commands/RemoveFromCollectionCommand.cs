// Copyright (C) 2015 Michael Norgate
// 
// This software may be modified and distributed under the terms of 
// the Creative Commons Attribution Non-commercial license.  See the LICENSE file for details.

#region usings

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.OData.Edm;
using Newtonsoft.Json.Linq;

#endregion

namespace OESoftware.Hosted.OData.Api.Db.Couchbase.Commands
{
    /// <summary>
    /// Removes an id from a collection
    /// </summary>
    public class RemoveFromCollectionCommand
    {
        /// <summary>
        /// Execute this command
        /// </summary>
        /// <param name="tenantId">The id of the tenant</param>
        /// <param name="key">The key of the item to remove from the collection</param>
        /// <param name="entityType">The type of the entity</param>
        /// <returns>void</returns>
        public async Task Execute(string tenantId, string key, Type entityType)
        {
            var bucket = BucketProvider.GetBucket();
            var id = Helpers.CreateCollectionId(tenantId, entityType.FullName);
            var existing = await bucket.GetDocumentAsync<string[]>(id);
            if (existing.Success)
            {
                var updated = existing.Document.Content.Where(w => w != key).ToArray();

                if (updated.Length == existing.Content.Length)
                {
                    return;
                }

                var updateResult = await bucket.ReplaceAsync(id, updated, existing.Document.Cas);
                if (!updateResult.Success)
                {
                    //If the update failed then another thread got there first
                    //Try again
                    await Execute(tenantId, key, entityType);
                }
            }
        }
    }
}