// Copyright (C) 2015 Michael Norgate

// This software may be modified and distributed under the terms of 
// the Creative Commons Attribution Non-commercial license.  See the LICENSE file for details.

#region usings

using System.Threading.Tasks;
using System.Web.OData;
using Microsoft.OData.Edm;

#endregion

namespace OESoftware.Hosted.OData.Api.Db.Couchbase.Commands
{
    /// <summary>
    /// Insert an entity
    /// </summary>
    public class InsertCommand : IDbCommand
    {
        private readonly EdmEntityObject _entity;
        private readonly IEdmEntityType _entityType;
        private readonly IEdmModel _model;

        /// <summary>
        /// Default Constructor
        /// </summary>
        /// <param name="entity"><see cref="EdmEntityObject"/> to insert</param>
        /// <param name="entityType">The <see cref="IEdmEntityType"/> of entity</param>
        /// <param name="model">The <see cref="IEdmModel"/> containing the type</param>
        public InsertCommand(EdmEntityObject entity, IEdmEntityType entityType, IEdmModel model)
        {
            _entity = entity;
            _entityType = entityType;
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
                var converter = new EntityObjectConverter(new ValueGenerator());

                var document =
                    await converter.ToDocument(_entity, tenantId, _entityType, ConvertOptions.ComputeValues, _model);

                var id = await Helpers.CreateEntityId(tenantId, _entity, _entityType);

                var result = await bucket.InsertAsync(id, document);
                if (!result.Success)
                {
                    throw ExceptionCreator.CreateDbException(result);
                }

                var addToCollection = new AddToCollectionCommand(id, _entityType);
                await addToCollection.Execute(tenantId);

                //Convert document back to entity
                var output = converter.ToEdmEntityObject(result.Value, tenantId, _entityType);

                return output;
            }
        }
    }
}