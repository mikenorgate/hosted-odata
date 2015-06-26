// Copyright (C) 2015 Michael Norgate

// This software may be modified and distributed under the terms of 
// the Creative Commons Attribution Non-commercial license.  See the LICENSE file for details.

#region usings

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.OData;
using Microsoft.OData.Edm;
using Newtonsoft.Json.Linq;

#endregion

namespace OESoftware.Hosted.OData.Api.Db.Couchbase.Commands
{
    /// <summary>
    /// Creates a relation between two entities
    /// </summary>
    public class CreateRelationCommand : IDbCommand
    {
        private readonly IDictionary<string, object> _primaryEntityKeys;
        private readonly IDictionary<string, object> _secondaryEntityKeys;
        private readonly IEdmNavigationProperty _navigationProperty;
        private readonly IEdmModel _model;

        /// <summary>
        /// Default Construtor
        /// </summary>
        /// <param name="keys">A dictionary of the keys for the entity</param>
        /// <param name="entity"><see cref="EdmEntityObject"/> to insert</param>
        /// <param name="entityType">The <see cref="IEdmEntityType"/> of the collection</param>
        /// <param name="model">The <see cref="IEdmModel"/> containing the type</param>
        public CreateRelationCommand(IDictionary<string, object> primaryEntityKeys, IDictionary<string, object> secondaryEntityKeys, IEdmNavigationProperty navigationProperty,
            IEdmModel model)
        {
            _primaryEntityKeys = primaryEntityKeys;
            _secondaryEntityKeys = secondaryEntityKeys;
            _navigationProperty = navigationProperty;
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
        public async Task Execute(string tenantId)
        {
            var bucket = BucketProvider.GetBucket();
            var primaryId = await Helpers.CreateEntityId(tenantId, _primaryEntityKeys, _navigationProperty.DeclaringEntityType());
            var secondaryId = await Helpers.CreateEntityId(tenantId, _secondaryEntityKeys, _navigationProperty.ToEntityType());

            //Get the current version
            var primaryDocument = await bucket.GetDocumentAsync<JObject>(primaryId);
            if (!primaryDocument.Success)
            {
                throw ExceptionCreator.CreateDbException(primaryDocument);
            }

            var secondaryDocument = await bucket.GetDocumentAsync<JObject>(secondaryId);
            if (!secondaryDocument.Success)
            {
                throw ExceptionCreator.CreateDbException(secondaryDocument);
            }

            if (_navigationProperty.TargetMultiplicity() == EdmMultiplicity.Many)
            {
                var property = primaryDocument.Document.Content.Property(_navigationProperty.Name);
                JArray array;
                if (property == null)
                {
                    array = new JArray();
                    primaryDocument.Document.Content.Add(_navigationProperty.Name, array);
                }
                else
                {
                    array = property.Value<JArray>();
                }

                if (!array.Contains(secondaryId))
                {
                    array.Add(secondaryId);
                }
            }
            else
            {
                primaryDocument.Document.Content.Add(_navigationProperty.Name, JValue.CreateString(secondaryId));
            }

            var replaceResult = await bucket.ReplaceAsync(primaryDocument.Document);
            if (!replaceResult.Success)
            {
                throw ExceptionCreator.CreateDbException(replaceResult);
            }
        }
    }
}