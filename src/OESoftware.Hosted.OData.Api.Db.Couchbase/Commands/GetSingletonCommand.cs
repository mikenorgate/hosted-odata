// Copyright (C) 2015 Michael Norgate

// This software may be modified and distributed under the terms of 
// the Creative Commons Attribution Non-commercial license.  See the LICENSE file for details.

#region usings

using System.Threading.Tasks;
using System.Web.OData;
using Microsoft.OData.Edm;
using Newtonsoft.Json.Linq;

#endregion

namespace OESoftware.Hosted.OData.Api.Db.Couchbase.Commands
{
    /// <summary>
    /// Get a singleton
    /// </summary>
    public class GetSingletonCommand : IDbCommand
    {
        private readonly IEdmSingleton _singleton;
        private readonly IEdmModel _model;

        /// <summary>
        /// Default Constructor
        /// </summary>
        /// <param name="singleton"><see cref="IEdmSingleton"/></param>
        /// <param name="model">The <see cref="IEdmModel"/> containing the type</param>
        public GetSingletonCommand(IEdmSingleton singleton, IEdmModel model)
        {
            _singleton = singleton;
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
                var id = Helpers.CreateSingletonId(tenantId, _singleton);
                var converter = new EntityObjectConverter(new ValueGenerator());
                var result = await bucket.GetDocumentAsync<JObject>(id);
                if (!result.Success)
                {
                    return await CommandHelpers.InsertSingletonAsync(converter, new EdmEntityObject(_singleton.EntityType()), tenantId, _singleton, _model, id, bucket);
                }
                else
                {

                    //Convert document back to entity
                    var output = converter.ToEdmEntityObject(result.Content, tenantId, _singleton.EntityType());

                    return output;
                }
            }
        }
    }
}