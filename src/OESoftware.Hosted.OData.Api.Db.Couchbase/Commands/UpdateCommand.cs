// Copyright (C) 2015 Michael Norgate

// This software may be modified and distributed under the terms of 
// the Creative Commons Attribution Non-commercial license.  See the LICENSE file for details.

#region usings

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.OData;
using Microsoft.OData.Edm;
using Newtonsoft.Json.Linq;

#endregion

namespace OESoftware.Hosted.OData.Api.Db.Couchbase.Commands
{
    /// <summary>
    /// Replace an entity value
    /// </summary>
    public class UpdateCommand : IDbCommand
    {
        private readonly EdmEntityObject _entity;
        private readonly IEdmEntityType _entityType;
        private readonly IDictionary<string, object> _keys;
        private readonly IEdmModel _model;

        /// <summary>
        /// Default Construtor
        /// </summary>
        /// <param name="keys">A dictionary of the keys for the entity</param>
        /// <param name="entity"><see cref="EdmEntityObject"/> to insert</param>
        /// <param name="entityType">The <see cref="IEdmEntityType"/> of the collection</param>
        /// <param name="model">The <see cref="IEdmModel"/> containing the type</param>
        public UpdateCommand(IDictionary<string, object> keys, EdmEntityObject entity, IEdmEntityType entityType,
            IEdmModel model)
        {
            _entity = entity;
            _entityType = entityType;
            _keys = keys;
            _model = model;
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
                var originalId = await Helpers.CreateEntityId(tenantId, _keys, _entityType);
                var id = await Helpers.CreateEntityId(tenantId, _keys, _entity, _entityType);

                var converter = new EntityObjectConverter(new ValueGenerator());

                //Get the current version
                var find = await bucket.GetDocumentAsync<JObject>(originalId);
                if (!find.Success)
                {
                    throw ExceptionCreator.CreateDbException(find);
                }

                var document =
                    await converter.ToDocument(_entity, tenantId, _entityType, ConvertOptions.CopyOnlySet, _model);
                var mergeSettings = new JsonMergeSettings
                {
                    MergeArrayHandling = MergeArrayHandling.Union
                };

                //Replace the existing values with the new ones
                find.Content.Merge(document, mergeSettings);

                //If the key hasn't changed then replace
                if (originalId.Equals(id, StringComparison.InvariantCultureIgnoreCase))
                {
                    var result = await bucket.ReplaceAsync(find.Document);
                    if (!result.Success)
                    {
                        throw ExceptionCreator.CreateDbException(result);
                    }
                }
                else
                {
                    var insertDoc = find.Document;
                    insertDoc.Id = id;

                    var result = await bucket.InsertAsync(insertDoc);
                    if (!result.Success)
                    {
                        throw ExceptionCreator.CreateDbException(result);
                    }

                    //If the key has changed remove the old and insert
                    var remove = await bucket.RemoveAsync(originalId, find.Document.Cas);
                    if (!remove.Success)
                    {
                        throw ExceptionCreator.CreateDbException(remove);
                    }

                    var addToCollectionCommand = new AddToCollectionCommand(id, _entityType);
                    await addToCollectionCommand.Execute(tenantId);

                    var removeFromCollectionCommand = new RemoveFromCollectionCommand(originalId, _entityType);
                    await removeFromCollectionCommand.Execute(tenantId);
                }

                //Convert document back to entity
                var output = converter.ToEdmEntityObject(find.Content, tenantId, _entityType);

                return output;
            }
        }
    }
}