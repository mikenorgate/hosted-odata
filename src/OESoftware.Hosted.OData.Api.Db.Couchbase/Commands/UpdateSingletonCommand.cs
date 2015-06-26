// Copyright (C) 2015 Michael Norgate

// This software may be modified and distributed under the terms of 
// the Creative Commons Attribution Non-commercial license.  See the LICENSE file for details.

#region usings

using System;
using System.Threading.Tasks;
using System.Web.OData;
using Couchbase;
using Fasterflect;
using Microsoft.OData.Edm;
using Newtonsoft.Json.Linq;
using OESoftware.Hosted.OData.Api.Core;

#endregion

namespace OESoftware.Hosted.OData.Api.Db.Couchbase.Commands
{
    /// <summary>
    /// Updates an singleton value
    /// </summary>
    public class UpdateSingletonCommand
    {
        private readonly IValueGenerator _valueGenerator;

        /// <summary>
        /// Default Constructor
        /// </summary>
        public UpdateSingletonCommand(IValueGenerator valueGenerator)
        {
            _valueGenerator = valueGenerator;
        }


        /// <summary>
        /// Execute this command
        /// </summary>
        /// <param name="tenantId">The id of the tenant</param>
        /// <param name="singletonType">The type of the singleton</param>
        /// <param name="delta">Entity delta</param>
        /// <param name="isPut">True if update should act as a put</param>
        /// <returns><see cref="IDynamicEntity"/></returns>
        public async Task<IDynamicEntity> Execute(string tenantId, Type singletonType, Delta delta, bool isPut)
        {
            var bucket = BucketProvider.GetBucket();
            //Convert entity to document
            var id = Helpers.CreateSingletonId(tenantId, singletonType.FullName);

            //Get the current version
            var find = await CommandHelpers.GetDocumentAsync(bucket, singletonType, id);

            if (!find.Success)
            {
                return
                    await CommandHelpers.InsertSingletonAsync(bucket, singletonType, id, tenantId, _valueGenerator, delta, isPut);
            }

            var original = CommandHelpers.ReflectionGetContent<IDynamicEntity>(find);

            delta.CallMethod(isPut ? "Put" : "Patch", original);

            var cas = CommandHelpers.ReflectionGetCas(find);
            var result = await CommandHelpers.ReplaceDocumentAsync(bucket, singletonType, id, original, cas);
            if (!result.Success)
            {
                throw ExceptionCreator.CreateDbException(result);
            }

            return original;
        }
    }
}