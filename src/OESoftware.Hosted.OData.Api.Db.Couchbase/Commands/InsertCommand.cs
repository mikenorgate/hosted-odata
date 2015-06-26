// Copyright (C) 2015 Michael Norgate

// This software may be modified and distributed under the terms of 
// the Creative Commons Attribution Non-commercial license.  See the LICENSE file for details.

#region usings

using System.Threading.Tasks;
using System.Web.OData;
using Microsoft.OData.Edm;
using OESoftware.Hosted.OData.Api.Core;

#endregion

namespace OESoftware.Hosted.OData.Api.Db.Couchbase.Commands
{
    /// <summary>
    /// Insert an entity
    /// </summary>
    public class InsertCommand
    {
        private readonly IValueGenerator _valueGenerator;

        /// <summary>
        /// Default Constructor
        /// </summary>
        public InsertCommand(IValueGenerator valueGenerator)
        {
            _valueGenerator = valueGenerator;
        }

        /// <summary>
        /// Execute this command
        /// </summary>
        /// <param name="entity"><see cref="IDynamicEntity"/></param>
        /// <param name="tenantId">The id of the tenant</param>
        /// <returns><see cref="EdmEntityObject"/></returns>
        public async Task<IDynamicEntity> Execute(string tenantId, IDynamicEntity entity)
        {
            await _valueGenerator.ComputeValues(tenantId, entity);
            var id = await Helpers.CreateEntityId(tenantId, entity);

            var bucket = BucketProvider.GetBucket();
            var result = await bucket.InsertAsync(id, entity);
            if (!result.Success)
            {
                throw ExceptionCreator.CreateDbException(result);
            }

            var addToCollection = new AddToCollectionCommand();
            await addToCollection.Execute(tenantId, id, entity.GetType());

            return entity;
        }
    }
}