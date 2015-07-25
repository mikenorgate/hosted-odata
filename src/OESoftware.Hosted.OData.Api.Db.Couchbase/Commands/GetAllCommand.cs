// Copyright (C) 2015 Michael Norgate

// This software may be modified and distributed under the terms of 
// the Creative Commons Attribution Non-commercial license.  See the LICENSE file for details.

#region usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.OData;
using Couchbase;
using Fasterflect;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Library;
using Newtonsoft.Json.Linq;
using OESoftware.Hosted.OData.Api.Core;

#endregion

namespace OESoftware.Hosted.OData.Api.Db.Couchbase.Commands
{
    /// <summary>
    /// Get all entities of a collection
    /// </summary>
    public class GetAllCommand
    {
        /// <summary>
        /// Execute this command
        /// </summary>
        /// <param name="tenantId">The id of the tenant</param>
        /// <param name="entityType">The type of the entity in the collection</param>
        /// <param name="castType">The <see cref="IEdmEntityType"/> to cast to</param>
        /// <returns><see cref="EdmEntityObjectCollection"/></returns>
        public async Task<IEnumerable<IDynamicEntity>> Execute(string tenantId, Type entityType, Type castType = null)
        {
            var bucket = BucketProvider.GetBucket();
            var id = Helpers.CreateCollectionId(tenantId, entityType.FullName);

            var result = await bucket.GetDocumentAsync<string[]>(id);
            if (!result.Success)
            {
                return new List<IDynamicEntity>();
            }

            return CommandHelpers.GetAll(bucket, result.Content, entityType, castType);
        }
    }
}