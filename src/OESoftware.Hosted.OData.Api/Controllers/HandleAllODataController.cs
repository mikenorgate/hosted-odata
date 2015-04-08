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
using OESoftware.Hosted.OData.Api.DBHelpers;
using OESoftware.Hosted.OData.Api.Extensions;
using OESoftware.Hosted.OData.Api.Models;

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

        public async Task<IHttpActionResult> Post(EdmEntityObject entity)
        {
            ODataPath path = Request.ODataProperties().Path;
            IEdmCollectionType collectionType = (IEdmCollectionType)path.EdmType;
            IEdmEntityTypeReference entityType = collectionType.ElementType.AsEntity();
            var model = Request.ODataProperties().Model;

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            await entity.SetComputedKeys(model, Request);

            

            //_db.Products.Add(product);
            //await _db.SaveChangesAsync();
            var dbIdentifier = Request.GetOwinEnvironment()["DbId"] as string;
            if (dbIdentifier == null)
            {
                throw new ApplicationException("Invalid DB identifier");
            }

            var dbConnection = DBConnectionFactory.Open(dbIdentifier);
            var collection = dbConnection.GetCollection<BsonDocument>(collectionType.FullTypeName());

            var keyName = "";
            var edmStructuralProperty = entityType.Key().FirstOrDefault();
            if (edmStructuralProperty != null)
            {
                keyName = edmStructuralProperty.Name;
            }

            var doc = new BsonDocument();
            //Put each property into BsonDocument
            foreach (var property in entityType.DeclaredStructuralProperties())
            {
                object value;
                if (!entity.TryGetPropertyValue(property.Name, out value))
                {
                    value = property.DefaultValueString;
                }
                var name = property.Name;
                //If this is the key move it to _id 
                //TODO: Support of multiple keys
                if (name.Equals(keyName, StringComparison.InvariantCultureIgnoreCase))
                {
                    name = "_id";
                }
                doc.Add(new BsonElement(name, BsonValue.Create(value)));
            }
            //Each navigation property which doesn't have a link already in the document
            //Create an empty array to hold the ids
            //TODO: post with navigation databind
            foreach (var property in entityType.DeclaredNavigationProperties())
            {
                if (property.ReferentialConstraint == null)
                {
                    doc.Add(new BsonElement(property.Name, new BsonArray()));
                }
            }

            if (entityType.IsOpen())
            {
                foreach (var property in entity.TryGetDynamicProperties())
                {
                    doc.Add(new BsonElement(property.Key, BsonValue.Create(property.Value)));
                }
            }


            await collection.InsertOneAsync(doc);

            return Created(entity, entityType);
        }

        private ODataQueryOptions GetODataQueryOptions(IEdmType edmType, IEdmModel model, ODataPath path)
        {
            ODataQueryContext queryContext = new ODataQueryContext(model, edmType, path);
            ODataQueryOptions queryOptions = new ODataQueryOptions(queryContext, this.Request);

            return queryOptions;
        }

        protected CreatedODataResult<EdmEntityObject> Created(EdmEntityObject entity, IEdmEntityTypeReference entityType)
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