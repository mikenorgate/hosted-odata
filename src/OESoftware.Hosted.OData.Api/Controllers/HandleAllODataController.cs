using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;
using System.Web.OData;
using System.Web.OData.Builder;
using System.Web.OData.Extensions;
using System.Web.OData.Query;
using System.Web.OData.Results;
using System.Web.OData.Routing;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Library;
using Microsoft.OData.Edm.Validation;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using OESoftware.Hosted.OData.Api.DBHelpers;
using OESoftware.Hosted.OData.Api.Extensions;
using OESoftware.Hosted.OData.Api.Models;
using OESoftware.Hosted.OData.Api.Attributes;
using OESoftware.Hosted.OData.Api.DBHelpers.Commands;

namespace OESoftware.Hosted.OData.Api.Controllers
{
    public class HandleAllODataController : ODataController
    {
        public async Task<IHttpActionResult> Get()
        {
            // Get entity set's EDM type: A collection type.
            ODataPath path = Request.ODataProperties().Path;
            if (path.EdmType is IEdmCollectionType)
            {
                IEdmCollectionType collectionType = (IEdmCollectionType)path.EdmType;
                IEdmEntityTypeReference entityType = collectionType.ElementType.AsEntity();
                var queryOptions = GetODataQueryOptions(entityType.Definition, Request.ODataProperties().Model, path);

                // Create an untyped collection with the EDM collection type.
                EdmEntityObjectCollection collection =
                    new EdmEntityObjectCollection(new EdmCollectionTypeReference(collectionType));

                // Add untyped objects to collection.
                Request.ODataProperties().TotalCount = 1;

                var item = new EdmEntityObject(entityType);
                item.TrySetPropertyValue("CategoryID", 123);
                item.TrySetPropertyValue("CategoryName", "Name");
                item.TrySetPropertyValue("Description", "Description");

                collection.Add(item);


                return Ok(collection);
            }
            else
            {
                return Ok(new EdmEntityObject(path.EdmType as IEdmEntityType));
            }
        }

        [ODataPath(EdmConstants.EntitySetPath)]
        public async Task<IHttpActionResult> PostCollection(EdmEntityObject entity)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var entityType = EntityTypeFromPath();
            var command = await CreateInsertCommand(entity, entityType);

            var executor = new DbCommandExecutor();

            await executor.Execute(command, Request);

            entity = new EdmEntityObject(entityType);
            foreach (var element in command.Result.Elements)
            {
                var name = element.Name;
                if (name.Equals("_id"))
                {
                    name = entityType.DeclaredKey.First().Name;
                }
                entity.TrySetPropertyValue(name, BsonTypeMapper.MapToDotNetValue(element.Value));
            }

            return Created(entity, entityType);
        }

        [ODataPath(EdmConstants.EntityNavigationPath)]
        public async Task<IHttpActionResult> PostNavigation(EdmEntityObject entity)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var entityType = EntityTypeFromPath();
            var insertCommand = await CreateInsertCommand(entity, entityType);

            var executor = new DbCommandExecutor();

            var path = Request.ODataProperties().Path;
            var updates = CreateNavigationLink((path.Segments.Last() as NavigationPathSegment).NavigationProperty, entity, Request.Properties["BaseEntity"] as EdmEntityObject);

            var commands = new List<IDbCommand<BsonDocument>>();
            commands.Add(insertCommand);
            commands.AddRange(updates);

            await executor.Execute(commands, Request);

            entity = new EdmEntityObject(entityType);
            foreach (var element in insertCommand.Result.Elements)
            {
                var name = element.Name;
                if (name.Equals("_id"))
                {
                    name = entityType.DeclaredKey.First().Name;
                }
                entity.TrySetPropertyValue(name, BsonTypeMapper.MapToDotNetValue(element.Value));
            }

            return Created(entity, entityType);
        }

        private IEnumerable<IDbCommand<BsonDocument>> CreateNavigationLink(IEdmNavigationProperty navigationProperty, EdmEntityObject obj1, EdmEntityObject obj2)
        {
            //Find the principal and get its key
            IEdmNavigationProperty principalNav, dependantNav = null;
            if (navigationProperty.IsPrincipal() || navigationProperty.Partner == null)
            {
                principalNav = navigationProperty;
            }
            else
            {
                principalNav = navigationProperty.Partner;
            }

            if (principalNav.Partner != null)
            {
                dependantNav = principalNav.Partner;
            }

            EdmEntityObject principalObject, dependantObject;
            if (obj1.GetEdmType()
                .FullName()
                .Equals(principalNav.DeclaringType.FullTypeName(), StringComparison.InvariantCultureIgnoreCase))
            {
                principalObject = obj1;
                dependantObject = obj2;
            }
            else
            {
                principalObject = obj2;
                dependantObject = obj1;
            }

            object principalKey = null, dependantKey = null;
            if (dependantNav != null)
            {
                principalObject.TryGetPropertyValue(dependantNav.ToEntityType().Key().First().Name,
                    out principalKey);
            }

            //Get the key of the dependant
            dependantObject.TryGetPropertyValue(principalNav.ToEntityType().Key().First().Name,
                out dependantKey);

            var results = new List<IDbCommand<BsonDocument>>();

            results.Add(GetUpdate(principalNav, principalObject, dependantKey));
            if (principalKey != null)
            {
                results.Add(GetUpdate(dependantNav, dependantObject, principalKey));
            }

            return results;
        }

        private static IDbCommand<BsonDocument> GetUpdate(IEdmNavigationProperty navigationProperty, EdmEntityObject navObject, object key)
        {
            object navKey = null;
            var navKeyName = navigationProperty.DeclaringEntityType().DeclaredKey.First().Name;
            navObject.TryGetPropertyValue(navKeyName, out navKey);
            var filter = Builders<BsonDocument>.Filter.Eq("_id", navKey);

            var updateBuilder = Builders<BsonDocument>.Update;
            UpdateDefinition<BsonDocument> update = null;
            //Add the dependant key to the principal
            switch (ExtensionMethods.TargetMultiplicity(navigationProperty))
            {
                case EdmMultiplicity.Many:
                    update = updateBuilder.AddToSet(navigationProperty.Name, key);
                    break;
                case EdmMultiplicity.One:
                case EdmMultiplicity.ZeroOrOne:
                case EdmMultiplicity.Unknown:
                    var propertyToSet = navigationProperty.Name;
                    if (navigationProperty.ReferentialConstraint != null)
                    {
                        var constraint = Enumerable.FirstOrDefault<EdmReferentialConstraintPropertyPair>(navigationProperty.ReferentialConstraint.PropertyPairs);
                        if (constraint != null)
                        {
                            propertyToSet = constraint.DependentProperty.Name;
                        }
                    }
                    update = updateBuilder.Set(propertyToSet, key);
                    break;
            }

            return new DbUpdateCommand<BsonDocument>(string.Format("Collection({0})", navigationProperty.DeclaringType.FullTypeName()), filter, update);
        }

        private ODataQueryOptions GetODataQueryOptions(IEdmType edmType, IEdmModel model, ODataPath path)
        {
            ODataQueryContext queryContext = new ODataQueryContext(model, edmType, path);
            ODataQueryOptions queryOptions = new ODataQueryOptions(queryContext, this.Request);

            return queryOptions;
        }

        private async Task<DbInsertCommand<BsonDocument>> CreateInsertCommand(EdmEntityObject entity, IEdmEntityType entityType)
        {
            var model = Request.ODataProperties().Model;
            var keysTask = entity.SetComputedKeys(model, Request);

            await keysTask;

            object key;
            entity.TryGetPropertyValue(entityType.DeclaredKey.First().Name, out key);
            var filter = Builders<BsonDocument>.Filter.Eq("_id", key);

            return new DbInsertCommand<BsonDocument>(string.Format("Collection({0})", entityType.FullTypeName()), entity.ToInsertDocument(entityType), filter);
        }

        private IEdmEntityType EntityTypeFromPath()
        {
            var path = Request.ODataProperties().Path;
            IEdmEntityType entityType = null;
            if (path.EdmType is IEdmCollectionType)
            {
                var collectionType = (IEdmCollectionType)path.EdmType;
                entityType = (IEdmEntityType)collectionType.ElementType.Definition;
            }
            else
            {
                entityType = (IEdmEntityType)path.EdmType;
            }
            return entityType;
        }

        protected CreatedODataResult<EdmEntityObject> Created(EdmEntityObject entity, IEdmEntityType entityType)
        {
            var path = Request.ODataProperties().Path;
            var currentSegments = path.Segments.ToList();

            var edmStructuralProperty = entityType.Key().FirstOrDefault();
            if (edmStructuralProperty != null)
            {
                var key = edmStructuralProperty.Name;
                object value;
                if (entity.TryGetPropertyValue(key, out value))
                {
                    currentSegments.Add(new KeyValuePathSegment(value.ToString()));
                }
            }


            var url = Url.CreateODataLink(currentSegments);

            return new CreatedODataResult<EdmEntityObject>(entity,
                Request.GetRequestContext().Configuration.Services.GetContentNegotiator(), Request,
                Request.GetRequestContext().Configuration.Formatters, new Uri(url));
        }
    }
}