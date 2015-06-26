// Copyright (C) 2015 Michael Norgate

// This software may be modified and distributed under the terms of 
// the Creative Commons Attribution Non-commercial license.  See the LICENSE file for details.

#region usings

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.OData;
using Couchbase;
using Couchbase.Core;
using Fasterflect;
using Microsoft.OData.Edm;
using Newtonsoft.Json.Linq;
using OESoftware.Hosted.OData.Api.Core;

#endregion

namespace OESoftware.Hosted.OData.Api.Db.Couchbase.Commands
{
    /// <summary>
    /// Get a single element by key
    /// </summary>
    public class GetCommand
    {
        /// <summary>
        /// Execute this command
        /// </summary>
        /// <param name="tenantId">The id of the tenant</param>
        /// <param name="keys">Keys to of entity to get</param>
        /// <param name="entityType">Type of entity</param>
        /// <param name="castType">The type to cast to</param>
        /// <returns><see cref="IDynamicEntity"/></returns>
        public async Task<IDynamicEntity> Execute(string tenantId, IDictionary<string, object> keys, Type entityType, Type castType = null)
        {
            var bucket = BucketProvider.GetBucket();
            //Convert entity to document
            var id = await Helpers.CreateEntityId(tenantId, keys, entityType.FullName);

            var result = await CommandHelpers.GetDocumentAsync(bucket, entityType, id, castType);

            if (!result.Success)
            {
                throw ExceptionCreator.CreateDbException(result);
            }

            return CommandHelpers.ReflectionGetContent<IDynamicEntity>(result);
        }
    }
}