using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.OData;
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
    public class InsertCommandTests
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
        public void Execute_InsertsEntity_ReturnEntity()
        {
            using (ShimsContext.Create())
            {
                var insertCalled = false;
                var addToCollectionCalled = false;
                var type = _model.FindDeclaredType("Test.SimpleWithKey") as IEdmEntityType;
                var keys = new Dictionary<string, object> { { "Int32", 1 } };
                var expectedId = Helpers.CreateEntityId("test", keys, type).Result;
                var document1 = new JObject() { { "Int32", 1 }, { "Prop1", "value1" }, { "Prop2", "value2" } };
                var entity = new EdmEntityObject(type);
                entity.TrySetPropertyValue("Int32", 1);

                CouchbaseDb.Fakes.ShimCluster.ConstructorString = (i, v) => { };

                Fakes.ShimEntityObjectConverter.AllInstances
                    .ToDocumentEdmEntityObjectStringIEdmEntityTypeConvertOptionsIEdmModel =
                    (converter, o, arg3, arg4, arg5, arg6) =>
                    {
                        return Task<JObject>.Factory.StartNew(() =>
                        {
                            Assert.AreEqual(ConvertOptions.ComputeValues, arg5);

                            return document1;
                        });
                    };

                var bucket = new CouchbaseDb.Core.Fakes.StubIBucket();
                Fakes.ShimBucketProvider.GetBucket = () => bucket;

                bucket.InsertAsyncOf1StringM0<JObject>((id, doc) =>
                {
                    return Task<CouchbaseDb.IOperationResult<JObject>>.Factory.StartNew(() =>
                    {
                        insertCalled = true;

                        Assert.AreEqual(expectedId, id);
                        Assert.AreEqual(document1, doc);

                        var result = new CouchbaseDb.Fakes.StubIOperationResult<JObject>();
                        result.SuccessGet = () => true;
                        result.ValueGet = () => null;
                        return result;
                    });
                });

                Couchbase.Commands.Fakes.ShimAddToCollectionCommand.AllInstances.ExecuteString =
                    (collectionCommand, id) =>
                    {
                        return Task.Factory.StartNew(() =>
                        {
                            addToCollectionCalled = true;
                        });
                    };

                var command = new InsertCommand(entity, type, _model);
                var output = command.Execute("test").Result;

                object value;
                Assert.IsTrue(output.TryGetPropertyValue("Int32", out value));
                Assert.AreEqual(1, value);

                Assert.IsTrue(insertCalled);
                Assert.IsTrue(addToCollectionCalled);
            }
        }

        [TestMethod]
        public void Execute_InsertsFails_ThrowsDbException()
        {
            using (ShimsContext.Create())
            {
                var addToCollectionCalled = false;
                var type = _model.FindDeclaredType("Test.SimpleWithKey") as IEdmEntityType;
                var document1 = new JObject() { { "Int32", 1 }, { "Prop1", "value1" }, { "Prop2", "value2" } };
                var entity = new EdmEntityObject(type);
                entity.TrySetPropertyValue("Int32", 1);

                CouchbaseDb.Fakes.ShimCluster.ConstructorString = (i, v) => { };

                Fakes.ShimEntityObjectConverter.AllInstances
                    .ToDocumentEdmEntityObjectStringIEdmEntityTypeConvertOptionsIEdmModel =
                    (converter, o, arg3, arg4, arg5, arg6) =>
                    {
                        return Task<JObject>.Factory.StartNew(() => document1);
                    };

                var bucket = new CouchbaseDb.Core.Fakes.StubIBucket();
                Fakes.ShimBucketProvider.GetBucket = () => bucket;

                bucket.InsertAsyncOf1StringM0<JObject>((id, doc) =>
                {
                    return Task<CouchbaseDb.IOperationResult<JObject>>.Factory.StartNew(() =>
                    {

                        var result = new CouchbaseDb.Fakes.StubIOperationResult<JObject>();
                        result.SuccessGet = () => false;
                        return result;
                    });
                });

                Couchbase.Commands.Fakes.ShimAddToCollectionCommand.AllInstances.ExecuteString =
                    (collectionCommand, id) =>
                    {
                        return Task.Factory.StartNew(() =>
                        {
                            addToCollectionCalled = true;
                        });
                    };

                try
                {
                    var command = new InsertCommand(entity, type, _model);
                    var output = command.Execute("test").Result;
                }
                catch (AggregateException exception)
                {
                    Assert.AreEqual(1, exception.InnerExceptions.Count);
                    var firstException = exception.InnerExceptions.First();
                    Assert.IsInstanceOfType(firstException, typeof(DbException));
                }

                Assert.IsFalse(addToCollectionCalled);
            }
        }
    }
}
