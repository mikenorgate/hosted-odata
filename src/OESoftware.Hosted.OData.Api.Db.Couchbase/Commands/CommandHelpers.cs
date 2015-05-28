// Copyright (C) 2015 Michael Norgate

// This software may be modified and distributed under the terms of 
// the Creative Commons Attribution Non-commercial license.  See the LICENSE file for details.

#region usings

using System.Threading.Tasks;
using System.Web.OData;
using Couchbase;
using Couchbase.Core;
using Microsoft.OData.Edm;
using Newtonsoft.Json.Linq;

#endregion

namespace OESoftware.Hosted.OData.Api.Db.Couchbase.Commands
{
    /// <summary>
    /// Helpers for commands
    /// </summary>
    internal static class CommandHelpers
    {
        /// <summary>
        /// Insert a singleton
        /// </summary>
        /// <param name="converter"><see cref="EntityObjectConverter"/></param>
        /// <param name="entity"><see cref="EdmEntityObject"/></param>
        /// <param name="tenantId">The id of the tenant</param>
        /// <param name="singleton"><see cref="IEdmSingleton"/></param>
        /// <param name="model"><see cref="IEdmModel"/></param>
        /// <param name="id">ID of the singleton</param>
        /// <param name="bucket"><see cref="IBucket"/></param>
        /// <returns><see cref="EdmEntityObject"/></returns>
        internal static async Task<EdmEntityObject> InsertSingletonAsync(EntityObjectConverter converter,
            EdmEntityObject entity, string tenantId, IEdmSingleton singleton, IEdmModel model, string id, IBucket bucket)
        {
            var document =
                await
                    converter.ToDocument(entity, tenantId, singleton.EntityType(), ConvertOptions.ComputeValues, model);
            var insertDoc = new Document<JObject>
            {
                Id = id,
                Content = document
            };
            var result = await bucket.InsertAsync(insertDoc);
            if (!result.Success)
            {
                throw ExceptionCreator.CreateDbException(result);
            }
            //Convert document back to entity
            var output = converter.ToEdmEntityObject(document, tenantId, singleton.EntityType());

            return output;
        }
    }
}