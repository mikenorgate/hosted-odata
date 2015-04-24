using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Web.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using OESoftware.Hosted.OData.Api.DBHelpers;
using OESoftware.Hosted.OData.Api.DBHelpers.Commands;
using OESoftware.Hosted.OData.Api.Models;

namespace OESoftware.Hosted.OData.Api.Tests
{
    [TestClass]
    public class DbCommandFactoryTests
    {
        [TestMethod]
        public void CreateInsertCommand_SetsComputedKeysForInsert()
        {
            var modelProvider = new ModelProvider();
            IEnumerable<EdmError> errors;
            var model = modelProvider.FromXml(File.ReadAllText("TestDataModel.xml"), out errors);

            var type = model.FindDeclaredType("NorthwindModel.EntityWithOnlyComputedKeys") as IEdmEntityType;

            var factory = new DbCommandFactory("", new TestKeyGenerator());

            var commands = factory.CreateInsertCommand(new EdmEntityObject(type), type, model).Result;
            var command = commands.First() as DbInsertCommand<BsonDocument>;

            Assert.IsNotNull(command);

            var id = command.Document["_id"];
            Assert.IsTrue(id.IsBsonDocument);
            var idDoc = id as BsonDocument;
            Assert.IsNotNull(idDoc["ItemId"]);
            Assert.IsNotNull(idDoc["ItemGuid"]);
        }

        [TestMethod]
        [ExpectedException(typeof(AggregateException))]
        public void CreateInsertCommand_ThrowsIfNoComputedKeyIsNotSet()
        {
            var modelProvider = new ModelProvider();
            IEnumerable<EdmError> errors;
            var model = modelProvider.FromXml(File.ReadAllText("TestDataModel.xml"), out errors);

            var type = model.FindDeclaredType("NorthwindModel.EntityWithMixedKeys") as IEdmEntityType;

            var factory = new DbCommandFactory("", new TestKeyGenerator());

            factory.CreateInsertCommand(new EdmEntityObject(type), type, model).Wait();
        }

        [TestMethod]
        public void CreateInsertCommand_SetsAllKeysForInsert()
        {
            var modelProvider = new ModelProvider();
            IEnumerable<EdmError> errors;
            var model = modelProvider.FromXml(File.ReadAllText("TestDataModel.xml"), out errors);

            var type = model.FindDeclaredType("NorthwindModel.EntityWithMixedKeys") as IEdmEntityType;

            var factory = new DbCommandFactory("", new TestKeyGenerator());

            var obj = new EdmEntityObject(type);
            obj.TrySetPropertyValue("NonComputed", Guid.NewGuid());
            var commands = factory.CreateInsertCommand(obj, type, model).Result;
            var command = commands.First() as DbInsertCommand<BsonDocument>;

            Assert.IsNotNull(command);

            var id = command.Document["_id"];
            Assert.IsTrue(id.IsBsonDocument);
            var idDoc = id as BsonDocument;
            Assert.IsNotNull(idDoc["ItemId"]);
            Assert.IsNotNull(idDoc["ItemGuid"]);
            Assert.IsNotNull(idDoc["NonComputed"]);
        }

        [TestMethod]
        public void CreateInsertCommand_SetsAllPropertiesForInsert()
        {
            var modelProvider = new ModelProvider();
            IEnumerable<EdmError> errors;
            var model = modelProvider.FromXml(File.ReadAllText("TestDataModel.xml"), out errors);

            var type = model.FindDeclaredType("NorthwindModel.EntityMixedProperties") as IEdmEntityType;

            var factory = new DbCommandFactory("", new TestKeyGenerator());

            var obj = new EdmEntityObject(type);
            obj.TrySetPropertyValue("NonComputed", Guid.NewGuid());
            var commands = factory.CreateInsertCommand(obj, type, model).Result;
            var command = commands.First() as DbInsertCommand<BsonDocument>;

            Assert.IsNotNull(command);
            
            Assert.IsNotNull(command.Document["ItemId"]);
            Assert.IsNotNull(command.Document["ItemGuid"]);
            Assert.IsNotNull(command.Document["NonComputed"]);
        }

        [TestMethod]
        public void CreateInsertCommand_SetsDynamicPropertiesOnOpenType()
        {
            var modelProvider = new ModelProvider();
            IEnumerable<EdmError> errors;
            var model = modelProvider.FromXml(File.ReadAllText("TestDataModel.xml"), out errors);

            var type = model.FindDeclaredType("NorthwindModel.OpenEntity") as IEdmEntityType;

            var factory = new DbCommandFactory("", new TestKeyGenerator());

            var obj = new EdmEntityObject(type);
            obj.TrySetPropertyValue("Prop1", Guid.NewGuid());
            obj.TrySetPropertyValue("Prop2", Guid.NewGuid());
            var commands = factory.CreateInsertCommand(obj, type, model).Result;
            var command = commands.First() as DbInsertCommand<BsonDocument>;

            Assert.IsNotNull(command);

            Assert.IsNotNull(command.Document["Prop1"]);
            Assert.IsNotNull(command.Document["Prop2"]);
        }

        [TestMethod]
        public void CreateUpdateCommand_SetsAllKeysInFilter()
        {
            var modelProvider = new ModelProvider();
            IEnumerable<EdmError> errors;
            var model = modelProvider.FromXml(File.ReadAllText("TestDataModel.xml"), out errors);

            var type = model.FindDeclaredType("NorthwindModel.EntityWithMixedKeys") as IEdmEntityType;

            var factory = new DbCommandFactory("", new TestKeyGenerator());

            var obj = new EdmEntityObject(type);
            var keys = new Dictionary<string, object>();
            keys.Add("ItemId", 1);
            keys.Add("ItemGuid", Guid.NewGuid());
            keys.Add("NonComputed", Guid.NewGuid());
            var commands = factory.CreateUpdateCommand(keys, obj, type, model, false).Result;
            var command = commands.First() as DbUpdateCommand<BsonDocument>;

            Assert.IsNotNull(command);

            var documentSerializer = BsonSerializer.SerializerRegistry.GetSerializer<BsonDocument>();
            var filter = command.FilterDefinition.Render(documentSerializer, BsonSerializer.SerializerRegistry);

            Assert.IsNotNull(filter["_id.ItemId"]);
            Assert.IsNotNull(filter["_id.ItemGuid"]);
            Assert.IsNotNull(filter["_id.NonComputed"]);
        }

        [TestMethod]
        public void CreateUpdateCommand_UpdatesOneField()
        {
            var modelProvider = new ModelProvider();
            IEnumerable<EdmError> errors;
            var model = modelProvider.FromXml(File.ReadAllText("TestDataModel.xml"), out errors);

            var type = model.FindDeclaredType("NorthwindModel.EntityWithOneKey") as IEdmEntityType;

            var factory = new DbCommandFactory("", new TestKeyGenerator());

            var obj = new EdmEntityObject(type);
            obj.TrySetPropertyValue("NonComputed", Guid.NewGuid());
            var keys = new Dictionary<string, object>();
            keys.Add("ItemId", 1);
            var commands = factory.CreateUpdateCommand(keys, obj, type, model, false).Result;
            var command = commands.First() as DbUpdateCommand<BsonDocument>;

            Assert.IsNotNull(command);

            var documentSerializer = BsonSerializer.SerializerRegistry.GetSerializer<BsonDocument>();
            var update = command.UpdateDefinition.Render(documentSerializer, BsonSerializer.SerializerRegistry);

            Assert.AreEqual(1, update.ElementCount);

            var set = update["$set"] as BsonDocument;

            Assert.IsNotNull(set);

            Assert.AreEqual(1, set.ElementCount);

            Assert.IsNotNull(set["NonComputed"]);
        }

        [TestMethod]
        public void CreateUpdateCommand_OverwriteDoesNotUseSet()
        {
            var modelProvider = new ModelProvider();
            IEnumerable<EdmError> errors;
            var model = modelProvider.FromXml(File.ReadAllText("TestDataModel.xml"), out errors);

            var type = model.FindDeclaredType("NorthwindModel.EntityWithOneKey") as IEdmEntityType;

            var factory = new DbCommandFactory("", new TestKeyGenerator());

            var obj = new EdmEntityObject(type);
            obj.TrySetPropertyValue("NonComputed", Guid.NewGuid());
            var keys = new Dictionary<string, object>();
            keys.Add("ItemId", 1);
            var commands = factory.CreateUpdateCommand(keys, obj, type, model, true).Result;
            var command = commands.First() as DbUpdateCommand<BsonDocument>;

            Assert.IsNotNull(command);

            var documentSerializer = BsonSerializer.SerializerRegistry.GetSerializer<BsonDocument>();
            var update = command.UpdateDefinition.Render(documentSerializer, BsonSerializer.SerializerRegistry);

            Assert.AreEqual(1, update.ElementCount);

            Assert.IsNotNull(update["NonComputed"]);
        }

        [TestMethod]
        public void CreateDeleteCommand_SetsAllKeysInFilter()
        {
            var modelProvider = new ModelProvider();
            IEnumerable<EdmError> errors;
            var model = modelProvider.FromXml(File.ReadAllText("TestDataModel.xml"), out errors);

            var type = model.FindDeclaredType("NorthwindModel.EntityWithMixedKeys") as IEdmEntityType;

            var factory = new DbCommandFactory("", new TestKeyGenerator());

            var keys = new Dictionary<string, object>();
            keys.Add("ItemId", 1);
            keys.Add("ItemGuid", Guid.NewGuid());
            keys.Add("NonComputed", Guid.NewGuid());
            var commands = factory.CreateDeleteCommand(keys, type).Result;
            var command = commands.First() as DbDeleteCommand<BsonDocument>;

            Assert.IsNotNull(command);

            var documentSerializer = BsonSerializer.SerializerRegistry.GetSerializer<BsonDocument>();
            var filter = command.FilterDefinition.Render(documentSerializer, BsonSerializer.SerializerRegistry);

            Assert.IsNotNull(filter["_id.ItemId"]);
            Assert.IsNotNull(filter["_id.ItemGuid"]);
            Assert.IsNotNull(filter["_id.NonComputed"]);
        }
    }
}

