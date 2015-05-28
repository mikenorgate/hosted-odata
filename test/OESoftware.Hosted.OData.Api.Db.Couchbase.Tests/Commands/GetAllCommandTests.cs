using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Csdl;
using Microsoft.OData.Edm.Validation;
using Microsoft.QualityTools.Testing.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using OESoftware.Hosted.OData.Api.Db.Couchbase.Commands;
using CouchbaseDb = Couchbase;

namespace OESoftware.Hosted.OData.Api.Db.Couchbase.Tests.Commands
{
    [TestClass]
    public class GetAllCommandTests
    {
        IEdmModel _model;

        [TestInitialize]
        public void TestInitialize()
        {
            using (var stringReader = new FileStream("TestDataModel.xml", FileMode.Open))
            {
                using (var xmlReader = XmlReader.Create(stringReader))
                {
                    IEnumerable<EdmError> errors;
                    EdmxReader.TryParse(xmlReader, out _model, out errors);
                }
            }
        }

        [TestMethod]
        public void Execute_CollectionExists_ReturnsCollection()
        {
            using (ShimsContext.Create())
            {
                var type = _model.FindDeclaredType("Test.SimpleWithKey") as IEdmEntityType;
                var collectionKeys = new JArray(1, 2);
                var collectionId = Helpers.CreateCollectionId("test", type);
                var document1 = new JObject() { { "Int32", 1 }, { "Prop1", "value1" }, { "Prop2", "value2" } };
                var document2 = new JObject() { { "Int32", 2 }, { "Prop1", "value1" }, { "Prop2", "value2" } };

                CouchbaseDb.Fakes.ShimCluster.ConstructorString = (i, v) => { };

                var bucket = new CouchbaseDb.Core.Fakes.StubIBucket();
                Fakes.ShimBucketProvider.GetBucket = () => bucket;
                bucket.GetAsyncOf1String((id) =>
                {
                    return Task<CouchbaseDb.IOperationResult<JArray>>.Factory.StartNew(() =>
                    {
                        Assert.AreEqual(collectionId, id);

                        var result = new CouchbaseDb.Fakes.StubIOperationResult<JArray>();
                        result.SuccessGet = () => true;
                        result.ValueGet = () => collectionKeys;
                        return result;
                    });
                });

                bucket.GetOf1IListOfString((ids) =>
                {
                    var result = new Dictionary<string, CouchbaseDb.IOperationResult<JObject>>();
                    var key1 = Helpers.CreateEntityId("test", new Dictionary<string, object>() { { "Int32", 1 } }, type).Result;
                    var operation1 = new CouchbaseDb.Fakes.StubIOperationResult<JObject>();
                    operation1.SuccessGet = () => true;
                    operation1.ValueGet = () => document1;
                    result.Add(key1, operation1);

                    var key2 = Helpers.CreateEntityId("test", new Dictionary<string, object>() { { "Int32", 2 } }, type).Result;
                    var operation2 = new CouchbaseDb.Fakes.StubIOperationResult<JObject>();
                    operation2.SuccessGet = () => true;
                    operation2.ValueGet = () => document2;
                    result.Add(key2, operation2);
                    return result;
                });

                var command = new GetAllCommand(type);
                var collection = command.Execute("test").Result;

                Assert.AreEqual(2, collection.Count);
                var item1 = collection[0];
                object value;
                Assert.IsTrue(item1.TryGetPropertyValue("Int32", out value));
                Assert.AreEqual(1, value);

                var item2 = collection[1];
                Assert.IsTrue(item2.TryGetPropertyValue("Int32", out value));
                Assert.AreEqual(2, value);
            }
        }

        [TestMethod]
        public void Execute_CollectionDoesNotExists_ThrowsDbException()
        {
            using (ShimsContext.Create())
            {
                var type = _model.FindDeclaredType("Test.SimpleWithKey") as IEdmEntityType;
                var collectionId = Helpers.CreateCollectionId("test", type);

                CouchbaseDb.Fakes.ShimCluster.ConstructorString = (i, v) => { };

                var bucket = new CouchbaseDb.Core.Fakes.StubIBucket();
                Fakes.ShimBucketProvider.GetBucket = () => bucket;
                bucket.GetAsyncOf1String((id) =>
                {
                    return Task<CouchbaseDb.IOperationResult<JArray>>.Factory.StartNew(() =>
                    {
                        Assert.AreEqual(collectionId, id);

                        var result = new CouchbaseDb.Fakes.StubIOperationResult<JArray>();
                        result.SuccessGet = () => false;
                        return result;
                    });
                });

                try
                {
                    var command = new GetAllCommand(type);
                    var collection = command.Execute("test").Result;
                }
                catch (AggregateException exception)
                {
                    Assert.AreEqual(1, exception.InnerExceptions.Count);
                    var firstException = exception.InnerExceptions.First();
                    Assert.IsInstanceOfType(firstException, typeof(DbException));
                }
            }
        }

        [TestMethod]
        public void Execute_CollectionExistsOneMemberOfCollectionDoesNotExists_ReturnsCollectionIgnoringMissingElement()
        {
            using (ShimsContext.Create())
            {
                var type = _model.FindDeclaredType("Test.SimpleWithKey") as IEdmEntityType;
                var collectionKeys = new JArray(1, 2);
                var collectionId = Helpers.CreateCollectionId("test", type);
                var document1 = new JObject() { { "Int32", 1 }, { "Prop1", "value1" }, { "Prop2", "value2" } };

                CouchbaseDb.Fakes.ShimCluster.ConstructorString = (i, v) => { };

                var bucket = new CouchbaseDb.Core.Fakes.StubIBucket();
                Fakes.ShimBucketProvider.GetBucket = () => bucket;
                bucket.GetAsyncOf1String((id) =>
                {
                    return Task<CouchbaseDb.IOperationResult<JArray>>.Factory.StartNew(() =>
                    {
                        Assert.AreEqual(collectionId, id);

                        var result = new CouchbaseDb.Fakes.StubIOperationResult<JArray>();
                        result.SuccessGet = () => true;
                        result.ValueGet = () => collectionKeys;
                        return result;
                    });
                });

                bucket.GetOf1IListOfString((ids) =>
                {
                    var result = new Dictionary<string, CouchbaseDb.IOperationResult<JObject>>();
                    var key1 = Helpers.CreateEntityId("test", new Dictionary<string, object>() { { "Int32", 1 } }, type).Result;
                    var operation1 = new CouchbaseDb.Fakes.StubIOperationResult<JObject>();
                    operation1.SuccessGet = () => true;
                    operation1.ValueGet = () => document1;
                    result.Add(key1, operation1);

                    var key2 = Helpers.CreateEntityId("test", new Dictionary<string, object>() { { "Int32", 2 } }, type).Result;
                    var operation2 = new CouchbaseDb.Fakes.StubIOperationResult<JObject>();
                    operation2.SuccessGet = () => false;
                    result.Add(key2, operation2);
                    return result;
                });

                var command = new GetAllCommand(type);
                var collection = command.Execute("test").Result;

                Assert.AreEqual(1, collection.Count);
                var item1 = collection[0];
                object value;
                Assert.IsTrue(item1.TryGetPropertyValue("Int32", out value));
                Assert.AreEqual(1, value);
            }
        }
    }
}
