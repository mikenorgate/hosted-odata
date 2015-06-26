// Copyright (C) 2015 Michael Norgate
// 
// This software may be modified and distributed under the terms of 
// the Creative Commons Attribution Non-commercial license.  See the LICENSE file for details.

#region usings

using System;
using System.Linq;
using System.Threading.Tasks;
using Couchbase;
using Couchbase.IO;
using Microsoft.OData.Edm;
using Newtonsoft.Json.Linq;

#endregion

namespace OESoftware.Hosted.OData.Api.Db.Couchbase.Commands
{
    /// <summary>
    /// Add an entity key to a collection
    /// </summary>
    public class AddToCollectionCommand
    {
        /// <summary>
        /// Execute this command
        /// </summary>
        /// <param name="tenantId">The id of the tenant</param>
        /// <param name="key">The key of the item to add to the collection</param>
        /// <param name="entityType">The type of the entity</param>
        /// <returns>void</returns>
        public async Task Execute(string tenantId, string key, Type entityType)
        {
            var bucket = BucketProvider.GetBucket();
            var id = Helpers.CreateCollectionId(tenantId, entityType.FullName);
            var existing = await bucket.GetDocumentAsync<string[]>(id);
            if (!existing.Success)
            {
                var insertReuslt = await bucket.InsertAsync(id, new string[] { key });
                if (!insertReuslt.Success && insertReuslt.Status == ResponseStatus.KeyExists)
                {
                    //If the insert failed with key exists then another thread got there first
                    //Try again
                    await Execute(tenantId, key, entityType);
                }
            }
            else
            {
                var match = existing.Content.FirstOrDefault(j => j.Equals(key));
                if (match != null)
                {
                    //Id is already in collection, nothing to do;
                    return;
                }

                var updated = existing.Document.Content;
                Array.Resize(ref updated, updated.Length + 1);
                updated[updated.Length - 1] = key;

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