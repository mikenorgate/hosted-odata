using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Couchbase.Fakes;
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
    public class GetSingletonCommandTests
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
        public void Execute_SingletonExists_ReturnsEntity()
        {
            using (ShimsContext.Create())
            {
                var type = _model.FindDeclaredSingleton("TestEntities.Singleton");
                var expectedId = Helpers.CreateSingletonId("test", type);
                var document1 = new JObject() { { "ItemId", 1 } };

                CouchbaseDb.Fakes.ShimCluster.ConstructorString = (i, v) => { };

                var bucket = new CouchbaseDb.Core.Fakes.StubIBucket();
                Fakes.ShimBucketProvider.GetBucket = () => bucket;
                bucket.GetDocumentAsyncOf1String((id) =>
                {
                    return Task<CouchbaseDb.IDocumentResult<JObject>>.Factory.StartNew(() =>
                    {
                        Assert.AreEqual(expectedId, id);

                        var result = new CouchbaseDb.Fakes.StubIDocumentResult<JObject>();
                        result.SuccessGet = () => true;
                        result.DocumentGet = () => new StubDocument<JObject>
                        {
                            Content = document1
                        };
                        result.ContentGet = () => document1;
                        return result;
                    });
                });

                var command = new GetSingletonCommand(type, _model);
                var entity = command.Execute("test").Result;

                
                object value;
                Assert.IsTrue(entity.TryGetPropertyValue("ItemId", out value));
                Assert.AreEqual(1, value);
            }
        }

        [TestMethod]
        public void Execute_DoesNotExists_Inserts()
        {
            using (ShimsContext.Create())
            {
                var insertCalled = false;
                var type = _model.FindDeclaredSingleton("TestEntities.Singleton");
                var expectedId = Helpers.CreateSingletonId("test", type);
                var document2 = new JObject() { { "ItemId", 0 } };

                CouchbaseDb.Fakes.ShimCluster.ConstructorString = (i, v) => { };

                Fakes.ShimEntityObjectConverter.AllInstances
                    .ToDocumentEdmEntityObjectStringIEdmEntityTypeConvertOptionsIEdmModel =
                    (converter, o, arg3, arg4, arg5, arg6) =>
                    {
                        return Task<JObject>.Factory.StartNew(() =>
                        {
                            Assert.AreEqual(ConvertOptions.ComputeValues, arg5);

                            return document2;
                        });
                    };

                var bucket = new CouchbaseDb.Core.Fakes.StubIBucket();
                Fakes.ShimBucketProvider.GetBucket = () => bucket;

                bucket.GetDocumentAsyncOf1String<JObject>((id) =>
                {
                    return Task<CouchbaseDb.IDocumentResult<JObject>>.Factory.StartNew(() =>
                    {
                        var result = new CouchbaseDb.Fakes.StubIDocumentResult<JObject>();
                        result.SuccessGet = () => false;
                        return result;
                    });
                });

                bucket.InsertAsyncOf1IDocumentOfM0<JObject>((doc) =>
                {
                    return Task<CouchbaseDb.IDocumentResult<JObject>>.Factory.StartNew(() =>
                    {
                        insertCalled = true;

                        Assert.AreEqual(expectedId, doc.Id);
                        Assert.AreEqual(document2, doc.Content);

                        var result = new CouchbaseDb.Fakes.StubIDocumentResult<JObject>();
                        result.SuccessGet = () => true;
                        return result;
                    });
                });

                var command = new GetSingletonCommand(type, _model);
                var output = command.Execute("test").Result;

                object value;
                Assert.IsTrue(output.TryGetPropertyValue("ItemId", out value));
                Assert.AreEqual(0, value);

                Assert.IsTrue(insertCalled);
            }
        }

        [TestMethod]
        public void Execute_InsertFails_ThrowsDbException()
        {
            using (ShimsContext.Create())
            {
                var insertCalled = false;
                var type = _model.FindDeclaredSingleton("TestEntities.Singleton");
                var expectedId = Helpers.CreateSingletonId("test", type);
                var document2 = new JObject() { { "ItemId", 0 } };

                CouchbaseDb.Fakes.ShimCluster.ConstructorString = (i, v) => { };

                Fakes.ShimEntityObjectConverter.AllInstances
                    .ToDocumentEdmEntityObjectStringIEdmEntityTypeConvertOptionsIEdmModel =
                    (converter, o, arg3, arg4, arg5, arg6) =>
                    {
                        return Task<JObject>.Factory.StartNew(() =>
                        {
                            Assert.AreEqual(ConvertOptions.ComputeValues, arg5);

                            return document2;
                        });
                    };

                var bucket = new CouchbaseDb.Core.Fakes.StubIBucket();
                Fakes.ShimBucketProvider.GetBucket = () => bucket;

                bucket.GetDocumentAsyncOf1String<JObject>((id) =>
                {
                    return Task<CouchbaseDb.IDocumentResult<JObject>>.Factory.StartNew(() =>
                    {
                        var result = new CouchbaseDb.Fakes.StubIDocumentResult<JObject>();
                        result.SuccessGet = () => false;
                        return result;
                    });
                });

                bucket.InsertAsyncOf1IDocumentOfM0<JObject>((doc) =>
                {
                    return Task<CouchbaseDb.IDocumentResult<JObject>>.Factory.StartNew(() =>
                    {
                        insertCalled = true;

                        Assert.AreEqual(expectedId, doc.Id);
                        Assert.AreEqual(document2, doc.Content);

                        var result = new CouchbaseDb.Fakes.StubIDocumentResult<JObject>();
                        result.SuccessGet = () => false;
                        return result;
                    });
                });

                try
                {
                    var command = new GetSingletonCommand(type, _model);
                    var output = command.Execute("test").Result;
                }
                catch (AggregateException exception)
                {
                    Assert.AreEqual(1, exception.InnerExceptions.Count);
                    var firstException = exception.InnerExceptions.First();
                    Assert.IsInstanceOfType(firstException, typeof(DbException));
                }

                Assert.IsTrue(insertCalled);
            }
        }
    }
}
