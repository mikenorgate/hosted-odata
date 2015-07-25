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
    /// Get the entity/entities that are related
    /// </summary>
    public class GetRelationCommand
    {

        public async Task<IEnumerable<IDynamicEntity>> Execute(string tenantId, IDictionary<string, object> primaryEntityKeys, Type primaryType, string navigationProperty)
        {
            var bucket = BucketProvider.GetBucket();

            var primaryId = await Helpers.CreateEntityId(tenantId, primaryEntityKeys, primaryType);

            var result = await CommandHelpers.GetDocumentAsync(bucket, primaryType, primaryId);

            if (!result.Success)
            {
                throw ExceptionCreator.CreateDbException(result);
            }

            var primaryEntity = CommandHelpers.ReflectionGetContent<IDynamicEntity>(result);

            var property = primaryEntity.GetType().GetRuntimeProperty(navigationProperty);

            var constraints =
                property.Attributes(typeof(ReferentialConstraintAttribute)).Cast<ReferentialConstraintAttribute>();

            var relatedIds = new List<string>();
            Type secondaryType = property.PropertyType;
            if (property.PropertyType.IsGenericType)
            {
                secondaryType = property.PropertyType.GenericTypeArguments[0];
            }

            if (!constraints.Any())
            {
                if (property.PropertyType.IsGenericType)
                {
                    var navigationIds = primaryEntity.GetProperty(property.Name + "Ids") as IList<string>;
                    relatedIds.AddRange(navigationIds);
                }
                else
                {
                    var idProperty = primaryEntity.GetProperty(property.Name + "Id") as string;
                    relatedIds.Add(idProperty);
                }

            }
            else
            {
                var keys = new Dictionary<string, object>();
                foreach (var referentialConstraintAttribute in constraints)
                {
                    keys.Add(referentialConstraintAttribute.DependantProperty, primaryEntity.GetProperty(referentialConstraintAttribute.DependantProperty));
                }
                relatedIds.Add(await Helpers.CreateEntityId(tenantId, keys, secondaryType));
            }

            return CommandHelpers.GetAll(bucket, relatedIds, secondaryType);
        }
    }
}