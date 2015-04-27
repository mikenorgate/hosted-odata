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
using OESoftware.Hosted.OData.Api.Db;
using OESoftware.Hosted.OData.Api.DBHelpers.Commands;

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
            var command = new Db.Couchbase.Commands.GetCommand(keys, entityType);
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
            var commandFactory = new DbCommandFactory(dbIdentifier);

            var entityType = EntityTypeFromPath();
            var model = Request.ODataProperties().Model;
            var keyGen = new Db.Couchbase.KeyGenerator();
            var tasks = (from key in entityType.DeclaredKey.Where(k => k.VocabularyAnnotations(model).Any(v => v.Term.FullName() == Microsoft.OData.Edm.Vocabularies.V1.CoreVocabularyConstants.Computed))
                         let key1 = key
                         select keyGen.CreateKey(dbIdentifier, key.Name, key.Type.Definition).ContinueWith((task) => { entity.TrySetPropertyValue(key1.Name, task.Result); })).ToList();

            await Task.WhenAll(tasks);

            var command = new Db.Couchbase.Commands.InsertCommand(entity, entityType);
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

            var command = new Db.Couchbase.Commands.UpdateCommand(keys, entity, entityType);
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

            var command = new Db.Couchbase.Commands.ReplaceCommand(keys, entity, entityType);
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
                var command = new Db.Couchbase.Commands.DeleteCommand(keys, entityType);
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
    }
}