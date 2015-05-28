using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;
using System.Web.OData;
using System.Web.OData.Extensions;
using System.Web.OData.Query;
using System.Web.OData.Results;
using System.Web.OData.Routing;
using Microsoft.OData.Edm;
using OESoftware.Hosted.OData.Api.Attributes;
using OESoftware.Hosted.OData.Api.Db;
using OESoftware.Hosted.OData.Api.Db.Couchbase.Commands;
using OESoftware.Hosted.OData.Api.Extensions;

namespace OESoftware.Hosted.OData.Api.Controllers
{
    public class HandleAllODataController : ODataController
    {
        [ODataPath(EdmConstants.EntityPath)]
        public async Task<IHttpActionResult> GetItem()
        {
            var entityType = EntityTypeFromPath();

            var path = Request.ODataProperties().Path;
            var keyProperty = path.Segments[1] as KeyValuePathSegment;
            var keys = keyProperty.ParseKeyValue(entityType);

            var dbIdentifier = Request.GetOwinEnvironment()["DbId"] as string;
            var command = new GetCommand(keys, entityType);
            try
            {
                var result = await command.Execute(dbIdentifier);
                return Ok(result);
            }
            catch (DbException ex)
            {
                return FromDbException(ex);
            }
            catch (KeyNotFoundException)
            {
                return BadRequest();
            }
        }

        [ODataPath(EdmConstants.SingletonPath)]
        public async Task<IHttpActionResult> GetSingleton()
        {
            var entityType = SingletonFromPath();
            var model = Request.ODataProperties().Model;

            var dbIdentifier = Request.GetOwinEnvironment()["DbId"] as string;
            var command = new GetSingletonCommand(entityType, model);
            try
            {
                var result = await command.Execute(dbIdentifier);
                return Ok(result);
            }
            catch (DbException ex)
            {
                return FromDbException(ex);
            }
            catch (KeyNotFoundException)
            {
                return BadRequest();
            }
        }

        [ODataPath(EdmConstants.EntityPropertyPath)]
        [ODataPath(EdmConstants.EntityRawPropertyPath)]
        public async Task<IHttpActionResult> GetItemProperty()
        {
            var entityType = EntityTypeFromPath();

            var path = Request.ODataProperties().Path;
            var keyProperty = path.Segments[1] as KeyValuePathSegment;
            var keys = keyProperty.ParseKeyValue(entityType);

            var dbIdentifier = Request.GetOwinEnvironment()["DbId"] as string;
            var command = new GetCommand(keys, entityType);
            try
            {
                var result = await command.Execute(dbIdentifier);
                var value = PropertyFromEntity(path, result);

                return Ok(value);
            }
            catch (DbException ex)
            {
                return FromDbException(ex);
            }
            catch (KeyNotFoundException)
            {
                return BadRequest();
            }
        }

        [ODataPath(EdmConstants.SingletonPropertyPath)]
        [ODataPath(EdmConstants.SingletonRawPropertyPath)]
        public async Task<IHttpActionResult> GetSingletonProperty()
        {
            var entityType = SingletonFromPath();

            var path = Request.ODataProperties().Path;
            var model = Request.ODataProperties().Model;

            var dbIdentifier = Request.GetOwinEnvironment()["DbId"] as string;
            var command = new GetSingletonCommand(entityType, model);
            try
            {
                var result = await command.Execute(dbIdentifier);
                var value = PropertyFromEntity(path, result);

                return Ok(value);
            }
            catch (DbException ex)
            {
                return FromDbException(ex);
            }
            catch (KeyNotFoundException)
            {
                return BadRequest();
            }
        }

        [ODataPath(EdmConstants.EntitySetPath)]
        public async Task<IHttpActionResult> GetCollection()
        {
            var entityType = EntityTypeFromPath();

            var dbIdentifier = Request.GetOwinEnvironment()["DbId"] as string;
            var command = new GetAllCommand(entityType);
            try
            {
                var result = await command.Execute(dbIdentifier);
                return Ok(result);
            }
            catch (DbException ex)
            {
                return FromDbException(ex);
            }
            catch (KeyNotFoundException)
            {
                return BadRequest();
            }
        }

        [ODataPath(EdmConstants.EntitySetPath)]
        public async Task<IHttpActionResult> PostCollection(EdmEntityObject entity)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var dbIdentifier = Request.GetOwinEnvironment()["DbId"] as string;

            var entityType = EntityTypeFromPath();
            var model = Request.ODataProperties().Model;

            var command = new InsertCommand(entity, entityType, model);
            try
            {
                await command.Execute(dbIdentifier);

                return Created(entity, entityType);
            }
            catch (DbException ex)
            {
                return FromDbException(ex);
            }
            catch (KeyNotFoundException)
            {
                return BadRequest();
            }
        }

        [ODataPath(EdmConstants.EntityPath)]
        public async Task<IHttpActionResult> PatchItem(EdmEntityObject entity)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var entityType = EntityTypeFromPath();

            var path = Request.ODataProperties().Path;
            var keyProperty = path.Segments[1] as KeyValuePathSegment;
            var keys = keyProperty.ParseKeyValue(entityType);

            var dbIdentifier = Request.GetOwinEnvironment()["DbId"] as string;

            var model = Request.ODataProperties().Model;
            var command = new UpdateCommand(keys, entity, entityType, model);
            try
            {
                var result = await command.Execute(dbIdentifier);

                return Updated(result);
            }
            catch (DbException ex)
            {
                return FromDbException(ex);
            }
            catch (KeyNotFoundException)
            {
                return BadRequest();
            }
        }

        [ODataPath(EdmConstants.SingletonPath)]
        public async Task<IHttpActionResult> PatchSingleton(EdmEntityObject entity)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var entityType = SingletonFromPath();

            var dbIdentifier = Request.GetOwinEnvironment()["DbId"] as string;

            var model = Request.ODataProperties().Model;
            var command = new UpdateSingletonCommand(entity, entityType, model);
            try
            {
                var result = await command.Execute(dbIdentifier);

                return Ok(result);
            }
            catch (DbException ex)
            {
                return FromDbException(ex);
            }
            catch (KeyNotFoundException)
            {
                return BadRequest();
            }
        }

        [ODataPath(EdmConstants.EntityPath)]
        public async Task<IHttpActionResult> PutItem(EdmEntityObject entity)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var entityType = EntityTypeFromPath();

            var path = Request.ODataProperties().Path;
            var keyProperty = path.Segments[1] as KeyValuePathSegment;
            var keys = keyProperty.ParseKeyValue(entityType);

            var dbIdentifier = Request.GetOwinEnvironment()["DbId"] as string;

            var model = Request.ODataProperties().Model;
            var command = new ReplaceCommand(keys, entity, entityType, model);
            try
            {
                var result = await command.Execute(dbIdentifier);

                return Updated(result);
            }
            catch (DbException ex)
            {
                return FromDbException(ex);
            }
            catch (KeyNotFoundException)
            {
                return BadRequest();
            }
        }

        [ODataPath(EdmConstants.SingletonPath)]
        public async Task<IHttpActionResult> PutSingleton(EdmEntityObject entity)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var entityType = SingletonFromPath();

            var dbIdentifier = Request.GetOwinEnvironment()["DbId"] as string;

            var model = Request.ODataProperties().Model;
            var command = new ReplaceSingletonCommand(entity, entityType, model);
            try
            {
                var result = await command.Execute(dbIdentifier);

                return Ok(result);
            }
            catch (DbException ex)
            {
                return FromDbException(ex);
            }
            catch (KeyNotFoundException)
            {
                return BadRequest();
            }
        }

        [ODataPath(EdmConstants.EntityPropertyPath)]
        [ODataPath(EdmConstants.EntityRawPropertyPath)]
        public async Task<IHttpActionResult> PutItemProperty([FromBody]string value)
        {
            var entityType = EntityTypeFromPath();

            var path = Request.ODataProperties().Path;

            var updateEntity = new EdmEntityObject(entityType);

            EdmStructuredObject propertyEntity = updateEntity;
            IEdmStructuredType propertyType = entityType;
            foreach (var oDataPathSegment in path.Segments.Where(s=>s is PropertyAccessPathSegment))
            {
                var pathSegment = (PropertyAccessPathSegment) oDataPathSegment;
                if (
                    propertyType != null && propertyType.DeclaredProperties.Any(
                        p => p.Name.Equals(pathSegment.PropertyName, StringComparison.InvariantCultureIgnoreCase)))
                {
                    if (pathSegment.Property.Type.IsPrimitive())
                    {
                        propertyEntity.TrySetPropertyValue(pathSegment.PropertyName, value);
                    }
                    else
                    {
                        propertyType = pathSegment.Property.Type.Definition as IEdmComplexType;
                        var complex = new EdmComplexObject((IEdmComplexType)propertyType);
                        propertyEntity.TrySetPropertyValue(pathSegment.PropertyName, complex);
                        propertyEntity = complex;
                    }
                }
                else
                {
                    return NotFound();
                }
            }

            await PatchItem(updateEntity);

            return Ok(value);
        }

        [ODataPath(EdmConstants.EntityPath)]
        public async Task<IHttpActionResult> DeleteItem()
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var entityType = EntityTypeFromPath();

            var path = Request.ODataProperties().Path;
            var keyProperty = path.Segments[1] as KeyValuePathSegment;
            var keys = keyProperty.ParseKeyValue(entityType);

            var dbIdentifier = Request.GetOwinEnvironment()["DbId"] as string;

            try
            {
                var command = new DeleteCommand(keys, entityType);
                await command.Execute(dbIdentifier);

                return new StatusCodeResult(HttpStatusCode.NoContent, this);
            }
            catch (DbException ex)
            {
                return FromDbException(ex);
            }
            catch (KeyNotFoundException)
            {
                return BadRequest();
            }
        }

        private ODataQueryOptions GetODataQueryOptions(IEdmType edmType, IEdmModel model, ODataPath path)
        {
            ODataQueryContext queryContext = new ODataQueryContext(model, edmType, path);
            ODataQueryOptions queryOptions = new ODataQueryOptions(queryContext, this.Request);

            return queryOptions;
        }

        private IEdmEntityType EntityTypeFromPath()
        {
            var path = Request.ODataProperties().Path;
            var segment = path.Segments.FirstOrDefault(s => s is EntitySetPathSegment) as EntitySetPathSegment;
            IEdmEntityType entityType = null;
            if (segment != null)
            {
                var type = segment.GetEdmType(null);
                if (type is IEdmCollectionType)
                {
                    var collectionType = (IEdmCollectionType) type;
                    entityType = (IEdmEntityType) collectionType.ElementType.Definition;
                }
                else
                {
                    entityType = (IEdmEntityType) type;
                }
            }
            return entityType;
        }

        private IEdmSingleton SingletonFromPath()
        {
            var path = Request.ODataProperties().Path;
            var segment = path.Segments.FirstOrDefault(s => s is SingletonPathSegment) as SingletonPathSegment;
            return segment.Singleton;
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

        private IHttpActionResult FromDbException(DbException exception)
        {
            switch (exception.Error)
            {
                case DbError.AuthenticationError:
                    return new StatusCodeResult(HttpStatusCode.Unauthorized, this);
                case DbError.InvalidData:
                    return BadRequest();
                case DbError.KeyExists:
                    return Conflict();
                case DbError.KeyNotFound:
                    return NotFound();
                default:
                    return InternalServerError();
            }
        }

        private static dynamic PropertyFromEntity(ODataPath path, EdmEntityObject result)
        {
            var propertySegements =
                path.Segments.Where(s => s is PropertyAccessPathSegment).Cast<PropertyAccessPathSegment>().ToList();
            IEdmStructuredObject propertyValue = result;
            dynamic value = null;
            foreach (var propertySegement in propertySegements)
            {
                if (propertyValue.TryGetPropertyValue(propertySegement.PropertyName, out value) && value is IEdmStructuredObject)
                {
                    propertyValue = (IEdmStructuredObject)value;
                }
            }
            return value;
        }
    }
}