// Copyright (C) 2015 Michael Norgate

// This software may be modified and distributed under the terms of 
// the Creative Commons Attribution Non-commercial license.  See the LICENSE file for details.

#region usings

using System;
using System.Threading.Tasks;
using System.Web.OData;
using Fasterflect;
using Microsoft.OData.Edm;
using Newtonsoft.Json.Linq;
using OESoftware.Hosted.OData.Api.Core;

#endregion

namespace OESoftware.Hosted.OData.Api.Db.Couchbase.Commands
{
    /// <summary>
    /// Get a singleton
    /// </summary>
    public class GetSingletonCommand
    {
        private readonly IValueGenerator _valueGenerator;

        /// <summary>
        /// Default Constructor
        /// </summary>
        public GetSingletonCommand(IValueGenerator valueGenerator)
        {
            _valueGenerator = valueGenerator;
        }

        /// <summary>
        /// Execute this command
        /// </summary>
        /// <param name="tenantId">The id of the tenant</param>
        /// <param name="singletonType">The type of the singleton</param>
        /// <returns><see cref="IDynamicEntity"/></returns>
        public async Task<IDynamicEntity> Execute(string tenantId, Type singletonType)
        {
            var bucket = BucketProvider.GetBucket();
            //Convert entity to document
            var id = Helpers.CreateSingletonId(tenantId, singletonType.FullName);

            var result = await CommandHelpers.GetDocumentAsync(bucket, singletonType, id);
            if (!result.Success)
            {
                return
                    await CommandHelpers.InsertSingletonAsync(bucket, singletonType, id, tenantId, _valueGenerator);
            }

            return CommandHelpers.ReflectionGetContent<IDynamicEntity>(result);
        }
    }
}