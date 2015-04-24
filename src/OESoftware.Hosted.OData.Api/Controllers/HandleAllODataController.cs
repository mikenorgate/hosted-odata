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
        [ODataPath(EdmConstants.EntityPath)]
        public async Task<IHttpActionResult> GetItem()
        {
            var entityType = EntityTypeFromPath();

            var path = Request.ODataProperties().Path;
            var keyProperty = path.Segments[1] as KeyValuePathSegment;
            var keys = keyProperty.ParseKeyValue(entityType);

            var executor = new DbCommandExecutor();
            var dbIdentifier = Request.GetOwinEnvironment()["DbId"] as string;
            var commandFactory = new DbCommandFactory(dbIdentifier);


            var commands = await commandFactory.CreateGetCommand(keys, entityType);

            await executor.Execute(commands, Request);

            var command = commands.First() as DbFindCommand<BsonDocument>;
            var results = await command.Result.ToListAsync();
            if (results.Count != 1)
            {
                return NotFound();
            }

            return Ok(results.First());
        }

        [ODataPath(EdmConstants.EntitySetPath)]
        public async Task<IHttpActionResult> PostCollection(EdmEntityObject entity)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var executor = new DbCommandExecutor();
            var dbIdentifier = Request.GetOwinEnvironment()["DbId"] as string;
            var commandFactory = new DbCommandFactory(dbIdentifier);

            var entityType = EntityTypeFromPath();
            var commands = await commandFactory.CreateInsertCommand(entity, entityType,
                Request.ODataProperties().Model);

            await executor.Execute(commands, Request);

            return Created(entity, entityType);
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

            var executor = new DbCommandExecutor();
            var dbIdentifier = Request.GetOwinEnvironment()["DbId"] as string;
            var commandFactory = new DbCommandFactory(dbIdentifier);


            var commands = await commandFactory.CreateUpdateCommand(keys, entity, entityType,
                Request.ODataProperties().Model, false);

            await executor.Execute(commands, Request);

            return Updated(entity);
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

            var executor = new DbCommandExecutor();
            var dbIdentifier = Request.GetOwinEnvironment()["DbId"] as string;
            var commandFactory = new DbCommandFactory(dbIdentifier);


            var commands = await commandFactory.CreateUpdateCommand(keys, entity, entityType,
                Request.ODataProperties().Model, false);

            await executor.Execute(commands, Request);

            return Updated(entity);
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

            var executor = new DbCommandExecutor();
            var dbIdentifier = Request.GetOwinEnvironment()["DbId"] as string;
            var commandFactory = new DbCommandFactory(dbIdentifier);


            var commands = await commandFactory.CreateDeleteCommand(keys, entityType);

            await executor.Execute(commands, Request);

            return new StatusCodeResult(HttpStatusCode.NoContent, this);
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
    }
}