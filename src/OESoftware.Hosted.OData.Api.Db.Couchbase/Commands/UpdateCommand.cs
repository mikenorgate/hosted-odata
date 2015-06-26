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
    /// Replace an entity value
    /// </summary>
    public class UpdateCommand
    {
        /// <summary>
        /// Execute this command
        /// </summary>
        /// <param name="tenantId">The id of the tenant</param>
        /// <param name="keys">Entity keys</param>
        /// <param name="entityType">Entity type</param>
        /// <param name="delta">Entity delta</param>
        /// <param name="isPut">True if update should act as a put</param>
        /// <returns><see cref="IDynamicEntity"/></returns>
        public async Task<IDynamicEntity> Execute(string tenantId, IDictionary<string, object> keys, Type entityType, Delta delta, bool isPut)
        {
            var bucket = BucketProvider.GetBucket();
            var originalId = await Helpers.CreateEntityId(tenantId, keys, entityType.FullName);

            var find = await CommandHelpers.GetDocumentAsync(bucket, entityType, originalId);
            if (!find.Success)
            {
                throw ExceptionCreator.CreateDbException(find);
            }

            var original = CommandHelpers.ReflectionGetContent<IDynamicEntity>(find);

            delta.CallMethod(isPut ? "Put" : "Patch", original);

            var id = await Helpers.CreateEntityId(tenantId, original);
            var cas = CommandHelpers.ReflectionGetCas(find);

            //If the key hasn't changed then replace
            if (originalId.Equals(id, StringComparison.InvariantCultureIgnoreCase))
            {
                var result = await CommandHelpers.ReplaceDocumentAsync(bucket, entityType, id, original, cas);
                if (!result.Success)
                {
                    throw ExceptionCreator.CreateDbException(result);
                }
            }
            else
            {
                var result = await CommandHelpers.InsertDocumentAsync(bucket, entityType, id, original);
                if (!result.Success)
                {
                    throw ExceptionCreator.CreateDbException(result);
                }

                //If the key has changed remove the old and insert
                var remove = await CommandHelpers.RemoveDocumentAsync(bucket, originalId, cas);
                if (!remove.Success)
                {
                    await CommandHelpers.RemoveDocumentAsync(bucket, id, result.Cas);
                    throw ExceptionCreator.CreateDbException(remove);
                }

                var addToCollectionCommand = new AddToCollectionCommand();
                await addToCollectionCommand.Execute(tenantId, id, entityType);

                var removeFromCollectionCommand = new RemoveFromCollectionCommand();
                await removeFromCollectionCommand.Execute(tenantId, originalId, entityType);
            }

            return original;
        }
    }
}