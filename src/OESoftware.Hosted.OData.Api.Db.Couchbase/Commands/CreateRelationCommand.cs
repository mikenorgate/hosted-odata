// Copyright (C) 2015 Michael Norgate

// This software may be modified and distributed under the terms of 
// the Creative Commons Attribution Non-commercial license.  See the LICENSE file for details.

#region usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
    /// Creates a relation between two entities
    /// </summary>
    public class CreateRelationCommand
    {

        public async Task Execute(string tenantId, IDictionary<string, object> primaryEntityKeys, Type primaryType, IDictionary<string, object> secondaryEntityKeys, Type secondaryType, string navigationProperty)
        {
            var bucket = BucketProvider.GetBucket();

            var primaryId = await Helpers.CreateEntityId(tenantId, primaryEntityKeys, primaryType);
            var secondaryId = await Helpers.CreateEntityId(tenantId, secondaryEntityKeys, secondaryType);

            var result = await CommandHelpers.GetDocumentAsync(bucket, primaryType, primaryId);

            if (!result.Success)
            {
                throw ExceptionCreator.CreateDbException(result);
            }

            var primaryEntity = CommandHelpers.ReflectionGetContent<IDynamicEntity>(result);
            var primaryCas = CommandHelpers.ReflectionGetCas(result);

            result = await CommandHelpers.GetDocumentAsync(bucket, secondaryType, secondaryId);

            if (!result.Success)
            {
                throw ExceptionCreator.CreateDbException(result);
            }

            var secondaryEntity = CommandHelpers.ReflectionGetContent<IDynamicEntity>(result);
            var secondaryCas = CommandHelpers.ReflectionGetCas(result);

            var property = primaryEntity.GetType().GetRuntimeProperty(navigationProperty);

            await UpdateRelation(primaryEntity, primaryCas, property, secondaryEntity, tenantId, bucket);

            var navigationPartnerAttribute = property.GetCustomAttribute<NavigationPartnerAttribute>();

            if (navigationPartnerAttribute != null)
            {
                await UpdateRelation(secondaryEntity, secondaryCas, secondaryEntity.GetType().GetRuntimeProperty(navigationPartnerAttribute.PartnerPropertyName), primaryEntity, tenantId, bucket);
            }
        }

        private static async Task UpdateRelation(IDynamicEntity entity, ulong entityCas, PropertyInfo navigationProperty, IDynamicEntity secondaryEntity, string tenantId, IBucket bucket)
        {
            var constraints =
                navigationProperty.Attributes(typeof (ReferentialConstraintAttribute)).Cast<ReferentialConstraintAttribute>();
            var secondId = await Helpers.CreateEntityId(tenantId, secondaryEntity);
            var updated = false;
            if (!constraints.Any())
            {
                if (navigationProperty.PropertyType.IsGenericType)
                {
                    var navigationIds = entity.GetProperty(navigationProperty.Name + "Ids") as IList<string>;
                    if (!navigationIds.Contains(secondId))
                    {
                        navigationIds.Add(secondId);
                        entity.SetProperty(navigationProperty.Name + "Ids", navigationIds);
                        updated = true;
                    }
                }
                else
                {
                    var idProperty = entity.GetProperty(navigationProperty.Name + "Id") as string;
                    if (!secondId.Equals(idProperty))
                    {
                        entity.SetProperty(navigationProperty.Name + "Id", secondId);
                        updated = true;
                    }
                }

            }
            else
            {
                //Check that referal constraints are valid
                foreach (var referentialConstraintAttribute in constraints)
                {
                    var dependantValue = entity.GetProperty(referentialConstraintAttribute.DependantProperty);
                    var principalProperty = secondaryEntity.GetProperty(referentialConstraintAttribute.PrincipalProperty);
                    if (!dependantValue.Equals(principalProperty))
                    {
                        entity.SetProperty(referentialConstraintAttribute.DependantProperty, principalProperty);
                        updated = true;
                    }
                }
            }

            if (updated)
            {
                var entityId = await Helpers.CreateEntityId(tenantId, entity);
                var replaceResult =
                    await CommandHelpers.ReplaceDocumentAsync(bucket, entity.GetType(), entityId, entity, entityCas);
                if (!replaceResult.Success)
                {
                    throw ExceptionCreator.CreateDbException(replaceResult);
                }
            }
        }
    }
}