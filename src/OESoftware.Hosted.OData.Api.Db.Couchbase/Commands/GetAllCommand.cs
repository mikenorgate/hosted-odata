// Copyright (C) 2015 Michael Norgate

// This software may be modified and distributed under the terms of 
// the Creative Commons Attribution Non-commercial license.  See the LICENSE file for details.

#region usings

using System.Linq;
using System.Threading.Tasks;
using System.Web.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Library;
using Newtonsoft.Json.Linq;

#endregion

namespace OESoftware.Hosted.OData.Api.Db.Couchbase.Commands
{
    /// <summary>
    /// Get all entities of a collection
    /// </summary>
    public class GetAllCommand : IDbCommand
    {
        private readonly IEdmEntityType _entityType;

        /// <summary>
        /// Default Constructor
        /// </summary>
        /// <param name="entityType">The <see cref="IEdmEntityType"/> of the collection</param>
        public GetAllCommand(IEdmEntityType entityType)
        {
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
        /// <param name="castType">The <see cref="IEdmEntityType"/> to cast to</param>
        /// <returns><see cref="EdmEntityObjectCollection"/></returns>
        public async Task<EdmEntityObjectCollection> Execute(string tenantId, IEdmEntityType castType = null)
        {
            using (var bucket = BucketProvider.GetBucket())
            {
                var id = Helpers.CreateCollectionId(tenantId, _entityType);

                var result = await bucket.GetAsync<JArray>(id);
                if (!result.Success)
                {
                    throw ExceptionCreator.CreateDbException(result);
                }

                var all = bucket.Get<JObject>(result.Value.Values<string>().ToList());

                var converter = new EntityObjectConverter(new ValueGenerator());
                var output =
                    all.Values.Where(e => e.Success)
                        .Select(e => converter.ToEdmEntityObject(e.Value, tenantId, castType ?? _entityType) as IEdmEntityObject);

                return
                    new EdmEntityObjectCollection(
                        new EdmCollectionTypeReference(
                            new EdmCollectionType(new EdmEntityTypeReference(_entityType, false))), output.ToList());
            }
        }
    }
}