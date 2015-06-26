using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;
using System.Web.OData;
using System.Web.OData.Extensions;
using System.Web.OData.Formatter;
using System.Web.OData.Query;
using System.Web.OData.Results;
using System.Web.OData.Routing;
using Fasterflect;
using Microsoft.OData.Edm;
using OESoftware.Hosted.OData.Api.Attributes;
using OESoftware.Hosted.OData.Api.Core;
using OESoftware.Hosted.OData.Api.Db;
using OESoftware.Hosted.OData.Api.Db.Couchbase;
using OESoftware.Hosted.OData.Api.Db.Couchbase.Commands;
using OESoftware.Hosted.OData.Api.Extensions;
using OESoftware.Hosted.OData.Api.TypeMapping;

namespace OESoftware.Hosted.OData.Api.Controllers
{
    [DynamicEntityMapping]
    public class HandleAllODataController : ODataController
    {
        [ODataPath(EdmConstants.EntityPath)]
        public async Task<IHttpActionResult> GetItem()
        {
            var entityType = EntityTypeFromPath();

            var model = Request.ODataProperties().Model;
            var path = Request.ODataProperties().Path;
            var keyProperty = path.Segments[1] as KeyValuePathSegment;
            var keys = keyProperty.ParseKeyValue(entityType);

            var dbIdentifier = Request.GetOwinEnvironment()["DbId"] as string;
            var command = new GetCommand();
            try
            {
                var result = await command.Execute(dbIdentifier, keys, EdmLibHelpers.GetClrType(path.EdmType, model));
                return DynamicOk(result);
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

        [ODataPath(EdmConstants.EntityPathWithCast)]
        public async Task<IHttpActionResult> GetItemWithCast()
        {
            var model = Request.ODataProperties().Model;
            var entityType = EdmLibHelpers.GetClrType(EntityTypeFromPath(), model);
            var castType = EdmLibHelpers.GetClrType(CastTypeFromPath(), model);
            if (!entityType.Inherits(castType))
            {
                return BadRequest(string.Format("Enity type {0} does not inherit from {1}", entityType.FullName, castType.FullName));
            }

            var path = Request.ODataProperties().Path;
            var keyProperty = path.Segments[1] as KeyValuePathSegment;
            var keys = keyProperty.ParseKeyValue(EntityTypeFromPath());

            var dbIdentifier = Request.GetOwinEnvironment()["DbId"] as string;
            var command = new GetCommand();
            try
            {
                var result = await command.Execute(dbIdentifier, keys, entityType, castType);
                return DynamicOk(result);
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
            var command = new GetSingletonCommand(new ValueGenerator());
            try
            {
                var result = await command.Execute(dbIdentifier, EdmLibHelpers.GetClrType(entityType.Type, model));
                return DynamicOk(result);
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
            var model = Request.ODataProperties().Model;

            var path = Request.ODataProperties().Path;
            var keyProperty = path.Segments[1] as KeyValuePathSegment;
            var keys = keyProperty.ParseKeyValue(entityType);

            var dbIdentifier = Request.GetOwinEnvironment()["DbId"] as string;
            var command = new GetCommand();
            try
            {
                var result = await command.Execute(dbIdentifier, keys, EdmLibHelpers.GetClrType(entityType, model));
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

        [ODataPath(EdmConstants.EntityPropertyPathWithCast)]
        [ODataPath(EdmConstants.EntityRawPropertyPathWithCast)]
        public async Task<IHttpActionResult> GetItemPropertyWithCast()
        {
            var model = Request.ODataProperties().Model;
            var entityType = EdmLibHelpers.GetClrType(EntityTypeFromPath(), model);
            var castType = EdmLibHelpers.GetClrType(CastTypeFromPath(), model);
            if (!entityType.Inherits(castType))
            {
                return BadRequest(string.Format("Enity type {0} does not inherit from {1}", entityType.FullName, castType.FullName));
            }

            var path = Request.ODataProperties().Path;
            var keyProperty = path.Segments[1] as KeyValuePathSegment;
            var keys = keyProperty.ParseKeyValue(EntityTypeFromPath());

            var dbIdentifier = Request.GetOwinEnvironment()["DbId"] as string;
            var command = new GetCommand();
            try
            {
                var result = await command.Execute(dbIdentifier, keys, entityType, castType);
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
            var command = new GetSingletonCommand(new ValueGenerator());
            try
            {
                var result = await command.Execute(dbIdentifier, EdmLibHelpers.GetClrType(entityType.Type, model));
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
            var model = Request.ODataProperties().Model;

            var dbIdentifier = Request.GetOwinEnvironment()["DbId"] as string;
            var command = new GetAllCommand();
            try
            {
                var entityType = EdmLibHelpers.GetClrType(TypeFromPath(), model);
                var result = await command.Execute(dbIdentifier, entityType);
                return DynamicOk(result, entityType);
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

        [ODataPath(EdmConstants.EntitySetPathWithCast)]
        public async Task<IHttpActionResult> GetCollectionWithCast()
        {
            var model = Request.ODataProperties().Model;
            var entityType = EdmLibHelpers.GetClrType(EntityTypeFromPath(), model);
            var castType = EdmLibHelpers.GetClrType(CastTypeFromPath(), model);
            if (!entityType.Inherits(castType))
            {
                return BadRequest(string.Format("Enity type {0} does not inherit from {1}", entityType.FullName, castType.FullName));
            }

            var dbIdentifier = Request.GetOwinEnvironment()["DbId"] as string;
            var command = new GetAllCommand();
            try
            {
                var result = await command.Execute(dbIdentifier, entityType, castType);
                return DynamicOk(result, castType);
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
        public async Task<IHttpActionResult> PostCollection(IDynamicEntity entity)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var dbIdentifier = Request.GetOwinEnvironment()["DbId"] as string;

            var command = new InsertCommand(new ValueGenerator());
            try
            {
                var output = await command.Execute(dbIdentifier, entity);

                return DynamicCreated(output);
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
        public async Task<IHttpActionResult> PatchItem(Delta entity)
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
            var command = new UpdateCommand();
            try
            {
                var result = await command.Execute(dbIdentifier, keys, EdmLibHelpers.GetClrType(entityType, model), entity, false);

                return DynamicUpdated(result);
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
        public async Task<IHttpActionResult> PatchSingleton(Delta entity)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var entityType = SingletonFromPath();

            var dbIdentifier = Request.GetOwinEnvironment()["DbId"] as string;

            var model = Request.ODataProperties().Model;
            var command = new UpdateSingletonCommand(new ValueGenerator());
            try
            {
                var result = await command.Execute(dbIdentifier, EdmLibHelpers.GetClrType(entityType.Type, model), entity, false);

                return DynamicOk(result);
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
        public async Task<IHttpActionResult> PutItem(Delta entity)
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
            var command = new UpdateCommand();
            try
            {
                var result = await command.Execute(dbIdentifier, keys, EdmLibHelpers.GetClrType(entityType, model), entity, true);

                return DynamicUpdated(result);
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
            var command = new UpdateSingletonCommand(new ValueGenerator());
            try
            {
                var result = await command.Execute(dbIdentifier, EdmLibHelpers.GetClrType(entityType.Type, model), entity, false);

                return DynamicOk(result);
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
        public IHttpActionResult PutItemProperty()
        {
            //var entityType = EntityTypeFromPath();
            //var path = Request.ODataProperties().Path;

            //var updateEntity = new EdmEntityObject(entityType);

            //EdmStructuredObject propertyEntity = updateEntity;
            //IEdmStructuredType propertyType = entityType;
            //foreach (var oDataPathSegment in path.Segments.Where(s=>s is PropertyAccessPathSegment))
            //{
            //    var pathSegment = (PropertyAccessPathSegment) oDataPathSegment;
            //    if (
            //        propertyType != null && propertyType.DeclaredProperties.Any(
            //            p => p.Name.Equals(pathSegment.PropertyName, StringComparison.InvariantCultureIgnoreCase)))
            //    {
            //        if (pathSegment.Property.Type.IsPrimitive())
            //        {
            //            propertyEntity.TrySetPropertyValue(pathSegment.PropertyName, value);
            //        }
            //        else
            //        {
            //            propertyType = pathSegment.Property.Type.Definition as IEdmComplexType;
            //            var complex = new EdmComplexObject((IEdmComplexType)propertyType);
            //            propertyEntity.TrySetPropertyValue(pathSegment.PropertyName, complex);
            //            propertyEntity = complex;
            //        }
            //    }
            //    else
            //    {
            //        return NotFound();
            //    }
            //}

            //await PatchItem(updateEntity);

            //return Ok(value);

            return new StatusCodeResult(HttpStatusCode.MethodNotAllowed, this);
        }

        [ODataPath(EdmConstants.EntityPropertyPath)]
        [ODataPath(EdmConstants.EntityRawPropertyPath)]
        public IHttpActionResult PatchItemProperty()
        {
            return new StatusCodeResult(HttpStatusCode.MethodNotAllowed, this);
        }

        [ODataPath(EdmConstants.EntityPropertyPath)]
        [ODataPath(EdmConstants.EntityRawPropertyPath)]
        public IHttpActionResult DeletetemProperty()
        {
            return new StatusCodeResult(HttpStatusCode.MethodNotAllowed, this);
        }


        [ODataPath(EdmConstants.EntityPath)]
        public async Task<IHttpActionResult> DeleteItem()
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var entityType = EntityTypeFromPath();
            var model = Request.ODataProperties().Model;

            var path = Request.ODataProperties().Path;
            var keyProperty = path.Segments[1] as KeyValuePathSegment;
            var keys = keyProperty.ParseKeyValue(entityType);

            var dbIdentifier = Request.GetOwinEnvironment()["DbId"] as string;

            try
            {
                var command = new DeleteCommand();
                await command.Execute(dbIdentifier, keys, EdmLibHelpers.GetClrType(entityType, model));

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
                    var collectionType = (IEdmCollectionType)type;
                    entityType = (IEdmEntityType)collectionType.ElementType.Definition;
                }
                else
                {
                    entityType = (IEdmEntityType)type;
                }
            }
            return entityType;
        }

        private IEdmType TypeFromPath()
        {
            var path = Request.ODataProperties().Path;
            IEdmType entityType = null;
            var type = path.EdmType;
            if (type is IEdmCollectionType)
            {
                var collectionType = (IEdmCollectionType)type;
                entityType = collectionType.ElementType.Definition;
            }
            else
            {
                entityType = type;
            }
            return entityType;
        }

        private IEdmSingleton SingletonFromPath()
        {
            var path = Request.ODataProperties().Path;
            var segment = path.Segments.FirstOrDefault(s => s is SingletonPathSegment) as SingletonPathSegment;
            return segment.Singleton;
        }

        private IEdmEntityType CastTypeFromPath()
        {
            var path = Request.ODataProperties().Path;
            var segment = path.Segments.FirstOrDefault(s => s is CastPathSegment) as CastPathSegment;
            return segment.CastType;
        }


        private IHttpActionResult DynamicCreated(IDynamicEntity entity)
        {
            var result = this.CallMethod(new Type[] { entity.GetType() }, "Created", entity);
            return (IHttpActionResult)result;
        }

        private IHttpActionResult DynamicOk(IDynamicEntity entity)
        {
            var result = this.CallMethod(new Type[] { entity.GetType() }, "Ok", entity);
            return (IHttpActionResult)result;
        }

        private IHttpActionResult DynamicOk(IEnumerable<IDynamicEntity> entity, Type entityType)
        {
            var enumberableType = typeof(List<>);
            var resultType = enumberableType.MakeGenericType(entityType);

            var outputList = resultType.CreateInstance();
            foreach (var dynamicEntity in entity)
            {
                outputList.CallMethod("Add", dynamicEntity);
            }

            var result = this.CallMethod(new Type[] { resultType }, "Ok", outputList);
            return (IHttpActionResult)result;
        }

        private IHttpActionResult DynamicUpdated(IDynamicEntity entity)
        {
            var result = this.CallMethod(new Type[] { entity.GetType() }, "Updated", entity);
            return (IHttpActionResult)result;
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

        private static dynamic PropertyFromEntity(ODataPath path, IDynamicEntity result)
        {
            var propertySegements =
                path.Segments.Where(s => s is PropertyAccessPathSegment).Cast<PropertyAccessPathSegment>().ToList();
            object propertyValue = result;
            object value = null;
            foreach (var propertySegement in propertySegements)
            {
                value = propertyValue.TryGetPropertyValue(propertySegement.PropertyName);
                if (!value.GetType().IsPrimitive)
                {
                    propertyValue = value;
                }
            }
            return value;
        }
    }
}