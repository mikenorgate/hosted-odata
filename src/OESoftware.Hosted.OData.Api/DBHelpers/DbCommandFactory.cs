using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.OData;
using Microsoft.OData.Edm;
using MongoDB.Bson;
using MongoDB.Driver;
using OESoftware.Hosted.OData.Api.DBHelpers.Commands;

namespace OESoftware.Hosted.OData.Api.DBHelpers
{
    public class DbCommandFactory
    {
        private IKeyGenerator _keyGenerator;
        private string _dbIdentifier;

        public DbCommandFactory(string dbIdentifier)
        {
            _dbIdentifier = dbIdentifier;
            _keyGenerator = new KeyGenerator();
        }

        public DbCommandFactory(string dbIdentifier, IKeyGenerator keyGenerator)
        {
            _dbIdentifier = dbIdentifier;
            _keyGenerator = keyGenerator;
        }

        public async Task<IEnumerable<IDbCommand<BsonDocument>>> CreateInsertCommand(EdmEntityObject entity, IEdmEntityType entityType, IEdmModel model)
        {
            Contract.Requires(entity != null);
            Contract.Requires(entityType != null);
            Contract.Requires(model != null);
            
            //Generate auto keys for entity
            var keyGenTask = CreateIdDocument(entity, entityType, model);


            //Copy each property into BsonDocument
            //Do not copy navigation properties that have referal constraint
            //If they do have a referal constraint do not copy the linked property
            //Properties can still be computed
            //If type is open copy any extra properties
            //else throw exception if there is any extra properties
            var insertDoc = await CreateBsonDocument(entity, entityType, model);
            //Set missing values to their defaults

            //Set single nav properties to null
            //Set multiple nav properties to empty array
            //If nav property has nested entity, create this entity
            //Create relationships

            var id = await keyGenTask;
            if (id != null)
            {
                insertDoc.Add("_id", id);
            }

            return new List<IDbCommand<BsonDocument>>
                {
                    new DbInsertCommand<BsonDocument>(string.Format("Collection({0})", entityType.FullTypeName()),
                        insertDoc,
                        null)
                };
        }

        public async Task<IEnumerable<IDbCommand<BsonDocument>>> CreateUpdateCommand(IDictionary<string, object> ids, EdmEntityObject delta, IEdmEntityType entityType, IEdmModel model, bool overwrite)
        {
            Contract.Requires(ids != null);
            Contract.Requires(delta != null);
            Contract.Requires(entityType != null);
            Contract.Requires(model != null);

            var result = new List<IDbCommand<BsonDocument>>();
            var filter = CreateFilterDefinition(ids);

            //Copy each property into BsonDocument ignoring keys
            //Do not copy navigation properties that have referal constraint
            //If they do have a referal constraint do not copy the linked property
            //If type is open copy any extra properties
            //If overwrite remove any extra properties
            //else throw exception if there is any extra properties
            if (!overwrite)
            {
                var update = CreateUpdateDefinitionBuilder(delta, entityType, model);
                result.Add(new DbUpdateCommand<BsonDocument>(string.Format("Collection({0})", entityType.FullTypeName()), filter, update));
            }
            else
            //If overwrite set any properties not specified to their default values
            {
                var insertDoc = await CreateBsonDocument(delta, entityType, model);
                //Set missing values to their defaults
                result.Add(new DbUpdateCommand<BsonDocument>(string.Format("Collection({0})", entityType.FullTypeName()), filter, new BsonDocumentUpdateDefinition<BsonDocument>(insertDoc)));
            }


            return result;
        }

        public async Task<IEnumerable<IDbCommand<BsonDocument>>> CreateDeleteCommand(IDictionary<string, object> ids, IEdmEntityType entityType)
        {
            //Create delete by id
            Contract.Requires(ids != null);
            Contract.Requires(entityType != null);

            var result = new List<IDbCommand<BsonDocument>>();
            var filter = CreateFilterDefinition(ids);

            result.Add(new DbDeleteCommand<BsonDocument>(string.Format("Collection({0})", entityType.FullTypeName()), filter));

            //Delete any with cascade delete

            return result;
        }

        public async Task<IEnumerable<IDbCommand<BsonDocument>>> CreateGetCommand(IDictionary<string, object> ids, IEdmEntityType entityType)
        {
            //Create delete by id
            Contract.Requires(ids != null);
            Contract.Requires(entityType != null);

            var result = new List<IDbCommand<BsonDocument>>();
            var filter = CreateFilterDefinition(ids);

            result.Add(new DbFindCommand<BsonDocument>(string.Format("Collection({0})", entityType.FullTypeName()), filter));

            //Delete any with cascade delete

            return result;
        }

        private async Task<BsonDocument> CreateIdDocument(EdmEntityObject entity, IEdmEntityType entityType, IEdmModel model)
        {
            if (entityType.DeclaredKey == null)
            {
                return null;
            }
            var idDoc = new BsonDocument();
            var tasks = new List<Task>();
            foreach (var keyProperty in entityType.DeclaredKey)
            {
                var property = keyProperty;
                if (property.VocabularyAnnotations(model).Any(v => v.Term.FullName() ==
                                                                   Microsoft.OData.Edm.Vocabularies.V1
                                                                       .CoreVocabularyConstants.Computed))
                {
                    tasks.Add(_keyGenerator.CreateKey(_dbIdentifier,
                        string.Format("{0}.{1}", entityType.FullTypeName(), keyProperty.Name),
                        keyProperty.Type.Definition).ContinueWith(
                            (task) =>
                            {
                                idDoc.Add(property.Name, BsonValue.Create(task.Result));
                            }));
                }
                else
                {
                    if (!entity.GetChangedPropertyNames().Contains(property.Name))
                    {
                        throw new ValidationException(string.Format("Key {0} must be provided", property.Name));
                    }
                    object value;
                    entity.TryGetPropertyValue(property.Name, out value);
                    idDoc.Add(property.Name, BsonValue.Create(value));
                }
            }

            await Task.WhenAll(tasks);
            return idDoc;
        }

        private async Task<BsonDocument> CreateBsonDocument(EdmEntityObject entity, IEdmEntityType entityType, IEdmModel model)
        {
            var tasks = new List<Task>();
            var result = new BsonDocument();
            var properties =
                entityType.DeclaredProperties.Where(
                    p =>
                        (entityType.DeclaredKey == null || !entityType.DeclaredKey.Any(
                            k => k.Name.Equals(p.Name, StringComparison.InvariantCultureIgnoreCase))) &&
                        (entityType.NavigationProperties() == null || !entityType.NavigationProperties()
                            .Any(
                                n =>
                                    n.ReferentialConstraint.PropertyPairs.Any(
                                        r =>
                                            r.DependentProperty.Name.Equals(p.Name,
                                                StringComparison.InvariantCultureIgnoreCase))))).ToList();
            foreach (var edmProperty in properties)
            {
                var property = edmProperty;
                if (property.VocabularyAnnotations(model).Any(v => v.Term.FullName() ==
                                                                   Microsoft.OData.Edm.Vocabularies.V1
                                                                       .CoreVocabularyConstants.Computed))
                {
                    tasks.Add(_keyGenerator.CreateKey(_dbIdentifier,
                        string.Format("{0}.{1}", entityType.FullTypeName(), property.Name),
                        property.Type.Definition).ContinueWith(
                            (task) => { result.Add(property.Name, BsonValue.Create(task.Result)); }));
                }
                else
                {
                    if (entity.GetChangedPropertyNames().Contains(property.Name))
                    {
                        object value;
                        entity.TryGetPropertyValue(property.Name, out value);
                        result.Add(property.Name, BsonValue.Create(value));
                    }
                }
            }

            var dynamicProperties =
                entity.GetChangedPropertyNames()
                    .Where(
                        e =>
                            !entityType.DeclaredProperties.Any(
                                d => d.Name.Equals(e, StringComparison.InvariantCultureIgnoreCase))).ToList();
            if (dynamicProperties.Any())
            {
                if (!entityType.IsOpen)
                {
                    throw new ValidationException("Dynamic properties not supported on this type");
                }

                foreach (var dynamicMemberName in dynamicProperties)
                {
                    object value;
                    entity.TryGetPropertyValue(dynamicMemberName, out value);
                    result.Add(dynamicMemberName, BsonValue.Create(value));
                }
            }

            await Task.WhenAll(tasks);
            return result;
        }

        private UpdateDefinition<BsonDocument> CreateUpdateDefinitionBuilder(EdmEntityObject entity,
            IEdmEntityType entityType, IEdmModel model)
        {
            UpdateDefinition<BsonDocument> update = null;
            var properties =
                entityType.DeclaredProperties.Where(
                    p =>
                        (entityType.DeclaredKey == null || !entityType.DeclaredKey.Any(
                            k => k.Name.Equals(p.Name, StringComparison.InvariantCultureIgnoreCase))) &&
                        (entityType.NavigationProperties() == null || !entityType.NavigationProperties()
                            .Any(
                                n =>
                                    n.ReferentialConstraint.PropertyPairs.Any(
                                        r =>
                                            r.DependentProperty.Name.Equals(p.Name,
                                                StringComparison.InvariantCultureIgnoreCase))))).ToList();
            foreach (var edmProperty in properties)
            {
                var property = edmProperty;
                if (!property.VocabularyAnnotations(model).Any(v => v.Term.FullName() ==
                                                                   Microsoft.OData.Edm.Vocabularies.V1
                                                                       .CoreVocabularyConstants.Computed))
                {
                    if (entity.GetChangedPropertyNames().Contains(property.Name))
                    {
                        object value;
                        entity.TryGetPropertyValue(property.Name, out value);
                        if (update == null)
                        {
                            update = Builders<BsonDocument>.Update.Set(property.Name, BsonValue.Create(value));
                        }
                        else
                        {
                            update = update.Set(property.Name, BsonValue.Create(value));
                        }
                    }
                }
            }

            var dynamicProperties =
                entity.GetChangedPropertyNames()
                    .Where(
                        e =>
                            !entityType.DeclaredProperties.Any(
                                d => d.Name.Equals(e, StringComparison.InvariantCultureIgnoreCase))).ToList();
            if (dynamicProperties.Any())
            {
                if (!entityType.IsOpen)
                {
                    throw new ValidationException("Dynamic properties not supported on this type");
                }

                foreach (var dynamicMemberName in dynamicProperties)
                {
                    object value;
                    entity.TryGetPropertyValue(dynamicMemberName, out value);
                    update = update.Set(dynamicMemberName, BsonValue.Create(value));
                }
            }

            return update;
        }

        private FilterDefinition<BsonDocument> CreateFilterDefinition(IDictionary<string, object> ids)
        {
            var filters = ids.Select(id => Builders<BsonDocument>.Filter.Eq(string.Format("_id.{0}", id.Key), BsonValue.Create(id.Value))).ToList();

            return Builders<BsonDocument>.Filter.And(filters);
        }
    }
}
