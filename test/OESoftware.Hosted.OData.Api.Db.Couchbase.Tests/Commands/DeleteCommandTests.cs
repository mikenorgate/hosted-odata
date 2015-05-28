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
    public class DeleteCommandTests
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
        public void Execute_EntityExists_RemovesItemAndRemovesFromCollection()
        {
            var deleteCalled = false;
            using (ShimsContext.Create())
            {
                var type = _model.FindDeclaredType("Test.SimpleWithKey") as IEdmEntityType;
                var keys = new Dictionary<string, object> {{ "Int32", 123}};
                var expectedId = Helpers.CreateEntityId("test", keys, type).Result;

                CouchbaseDb.Fakes.ShimCluster.ConstructorString = (i, v) => { };

                var bucket = new CouchbaseDb.Core.Fakes.StubIBucket();
                Fakes.ShimBucketProvider.GetBucket = () => bucket;

                bucket.RemoveAsyncString = (document) =>
                {
                    return Task<CouchbaseDb.IOperationResult>.Factory.StartNew(() =>
                    {
                        deleteCalled = true;

                        Assert.AreEqual(expectedId, document);
                        
                        var result = new CouchbaseDb.Fakes.StubIOperationResult();
                        result.SuccessGet = () => true;
                        return result;
                    });
                };

                var removeFromCollectionCalled = false;

                Couchbase.Commands.Fakes.ShimRemoveFromCollectionCommand.AllInstances.ExecuteString = (instance, tenantId) =>
                {
                    return Task.Factory.StartNew(() =>
                    {
                        removeFromCollectionCalled = true;
                        Assert.AreEqual("test", tenantId);
                    });
                };

                var command = new DeleteCommand(keys, type);
                command.Execute("test").Wait();

                Assert.IsTrue(deleteCalled);
                Assert.IsTrue(removeFromCollectionCalled);
            }
        }

        [TestMethod]
        public void Execute_EntityDoesNotExists_ThrowsDbException()
        {
            var deleteCalled = false;
            using (ShimsContext.Create())
            {
                var type = _model.FindDeclaredType("Test.SimpleWithKey") as IEdmEntityType;
                var keys = new Dictionary<string, object> { { "Int32", 123 } };
                var expectedId = Helpers.CreateEntityId("test", keys, type).Result;

                CouchbaseDb.Fakes.ShimCluster.ConstructorString = (i, v) => { };

                var bucket = new CouchbaseDb.Core.Fakes.StubIBucket();
                Fakes.ShimBucketProvider.GetBucket = () => bucket;

                bucket.RemoveAsyncString = (document) =>
                {
                    return Task<CouchbaseDb.IOperationResult>.Factory.StartNew(() =>
                    {
                        deleteCalled = true;

                        Assert.AreEqual(expectedId, document);

                        var result = new CouchbaseDb.Fakes.StubIOperationResult();
                        result.SuccessGet = () => false;
                        return result;
                    });
                };

                var removeFromCollectionCalled = false;

                Couchbase.Commands.Fakes.ShimRemoveFromCollectionCommand.AllInstances.ExecuteString = (instance, tenantId) =>
                {
                    return Task.Factory.StartNew(() =>
                    {
                        removeFromCollectionCalled = true;
                        Assert.AreEqual("test", tenantId);
                    });
                };

                try
                {
                    var command = new DeleteCommand(keys, type);
                    command.Execute("test").Wait();
                    Assert.Fail();
                }
                catch (AggregateException exception)
                {
                    Assert.AreEqual(1, exception.InnerExceptions.Count);
                    var firstException = exception.InnerExceptions.First();
                    Assert.IsInstanceOfType(firstException, typeof(DbException));
                }

                Assert.IsTrue(deleteCalled);
                Assert.IsFalse(removeFromCollectionCalled);
            }
        }
    }
}
