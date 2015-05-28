// Copyright (C) 2015 Michael Norgate
// 
// This software may be modified and distributed under the terms of 
// the Creative Commons Attribution Non-commercial license.  See the LICENSE file for details.

#region usings

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
    public class AddToCollectionCommand : IDbCommand
    {
        private readonly IEdmEntityType _entityType;
        private readonly string _key;

        /// <summary>
        /// Default Constructor
        /// </summary>
        /// <param name="key">The key to add to the collection</param>
        /// <param name="entityType">The <see cref="IEdmEntityType"/> of the collection</param>
        public AddToCollectionCommand(string key, IEdmEntityType entityType)
        {
            _key = key;
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
                var id = Helpers.CreateCollectionId(tenantId, _entityType);
                var existing = await bucket.GetDocumentAsync<JArray>(id);
                if (!existing.Success)
                {
                    var insertReuslt = await bucket.InsertAsync(new Document<JArray>
                    {
                        Id = id,
                        Content = new JArray(_key)
                    });
                    if (!insertReuslt.Success && insertReuslt.Status == ResponseStatus.KeyExists)
                    {
                        //If the insert failed with key exists then another thread got there first
                        //Try again
                        await Execute(tenantId);
                    }
                }
                else
                {
                    var match = existing.Content.FirstOrDefault(j => j.Value<string>().Equals(_key));
                    if (match != null)
                    {
                        //Id is already in collection, nothing to do;
                        return;
                    }
                    existing.Document.Content.Add(_key);
                    var updateResult = await bucket.ReplaceAsync(existing.Document);
                    if (!updateResult.Success)
                    {
                        //If the update failed then another thread got there first
                        //Try again
                        await Execute(tenantId);
                    }
                }
            }
        }
    }
}