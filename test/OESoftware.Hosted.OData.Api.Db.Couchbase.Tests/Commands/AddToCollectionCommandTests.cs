using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using Couchbase.IO;
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
    public class AddToCollectionCommandTests
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
        public void Execute_CreatesNewCollectionAndAddsId_InsertsId()
        {
            var insertCalled = false;
            using (ShimsContext.Create())
            {
                var type = _model.FindDeclaredType("Test.AllTypes") as IEdmEntityType;
                var entityKey = "test-key";
                var expectedId = Helpers.CreateCollectionId("test", type);

                CouchbaseDb.Fakes.ShimCluster.ConstructorString = (i, v) => { };

                var bucket = new CouchbaseDb.Core.Fakes.StubIBucket();
                Fakes.ShimBucketProvider.GetBucket = () => bucket;
                bucket.GetDocumentAsyncOf1String((id) =>
                {
                    return Task<CouchbaseDb.IDocumentResult<JArray>>.Factory.StartNew(() =>
                    {
                        Assert.AreEqual(expectedId, id);

                        var result = new CouchbaseDb.Fakes.StubIDocumentResult<JArray>();
                        result.SuccessGet = () => false;
                        return result;
                    });
                });

                bucket.InsertAsyncOf1IDocumentOfM0<JArray>((document) =>
                {
                    return Task<CouchbaseDb.IDocumentResult<JArray>>.Factory.StartNew(() =>
                    {
                        insertCalled = true;

                        Assert.AreEqual(expectedId, document.Id);

                        Assert.AreEqual(entityKey, document.Content[0]);

                        var result = new CouchbaseDb.Fakes.StubIDocumentResult<JArray>();
                        result.SuccessGet = () => true;
                        return result;
                    });
                });

                var command = new AddToCollectionCommand(entityKey, type);
                command.Execute("test").Wait();

                if (!insertCalled)
                {
                    Assert.Fail();
                }
            }
        }

        [TestMethod]
        public void Execute_AppendsCollectionAndAddsId_AppendsId()
        {
            var replaceCalled = false;
            using (ShimsContext.Create())
            {
                var type = _model.FindDeclaredType("Test.AllTypes") as IEdmEntityType;
                var entityKey = "test-key";
                var existingArray = new JArray("id-1", "id-2");
                var expectedId = Helpers.CreateCollectionId("test", type);

                CouchbaseDb.Fakes.ShimCluster.ConstructorString = (i, v) => { };

                var bucket = new CouchbaseDb.Core.Fakes.StubIBucket();
                Fakes.ShimBucketProvider.GetBucket = () => bucket;
                bucket.GetDocumentAsyncOf1String((id) =>
                {
                    return Task<CouchbaseDb.IDocumentResult<JArray>>.Factory.StartNew(() =>
                    {
                        Assert.AreEqual(expectedId, id);

                        var result = new CouchbaseDb.Fakes.StubIDocumentResult<JArray>();
                        var document = new CouchbaseDb.Fakes.StubDocument<JArray>();
                        document.Content = existingArray;
                        document.Id = id;
                        result.SuccessGet = () => true;
                        result.ContentGet = () => document.Content;
                        result.DocumentGet = () => document;
                        return result;
                    });
                });

                bucket.ReplaceAsyncOf1IDocumentOfM0<JArray>((document) =>
                {
                    return Task<CouchbaseDb.IDocumentResult<JArray>>.Factory.StartNew(() =>
                    {
                        replaceCalled = true;

                        Assert.AreEqual(expectedId, document.Id);

                        Assert.AreEqual(3, document.Content.Count);
                        Assert.AreEqual(entityKey, document.Content[2]);

                        var result = new CouchbaseDb.Fakes.StubIDocumentResult<JArray>();
                        result.SuccessGet = () => true;
                        return result;
                    });
                });

                var command = new AddToCollectionCommand(entityKey, type);
                command.Execute("test").Wait();

                if (!replaceCalled)
                {
                    Assert.Fail();
                }
            }
        }

        [TestMethod]
        public void Execute_IdAlreadyInArrary_DoesNothing()
        {
            var replaceCalled = false;
            using (ShimsContext.Create())
            {
                var type = _model.FindDeclaredType("Test.AllTypes") as IEdmEntityType;
                var entityKey = "test-key";
                var existingArray = new JArray("id-1", "id-2", "test-key");
                var expectedId = Helpers.CreateCollectionId("test", type);

                CouchbaseDb.Fakes.ShimCluster.ConstructorString = (i, v) => { };

                var bucket = new CouchbaseDb.Core.Fakes.StubIBucket();
                Fakes.ShimBucketProvider.GetBucket = () => bucket;
                bucket.GetDocumentAsyncOf1String((id) =>
                {
                    return Task<CouchbaseDb.IDocumentResult<JArray>>.Factory.StartNew(() =>
                    {
                        Assert.AreEqual(expectedId, id);

                        var result = new CouchbaseDb.Fakes.StubIDocumentResult<JArray>();
                        var document = new CouchbaseDb.Fakes.StubDocument<JArray>();
                        document.Content = existingArray;
                        document.Id = id;
                        result.SuccessGet = () => true;
                        result.ContentGet = () => document.Content;
                        result.DocumentGet = () => document;
                        return result;
                    });
                });

                bucket.ReplaceAsyncOf1IDocumentOfM0<JArray>((document) =>
                {
                    return Task<CouchbaseDb.IDocumentResult<JArray>>.Factory.StartNew(() =>
                    {
                        replaceCalled = true;
                        var result = new CouchbaseDb.Fakes.StubIDocumentResult<JArray>();
                        result.SuccessGet = () => true;
                        return result;
                    });
                });

                var command = new AddToCollectionCommand(entityKey, type);
                command.Execute("test").Wait();

                Assert.IsFalse(replaceCalled);
            }
        }

        [TestMethod]
        public void Execute_InsertFailsDueToExistingKey_Retrys()
        {
            var insertCalled = false;
            using (ShimsContext.Create())
            {
                var type = _model.FindDeclaredType("Test.AllTypes") as IEdmEntityType;
                var entityKey = "test-key";
                var expectedId = Helpers.CreateCollectionId("test", type);
                var called = 0;

                CouchbaseDb.Fakes.ShimCluster.ConstructorString = (i, v) => { };

                var bucket = new CouchbaseDb.Core.Fakes.StubIBucket();
                Fakes.ShimBucketProvider.GetBucket = () => bucket;
                bucket.GetDocumentAsyncOf1String((id) =>
                {
                    return Task<CouchbaseDb.IDocumentResult<JArray>>.Factory.StartNew(() =>
                    {
                        Assert.AreEqual(expectedId, id);

                        var result = new CouchbaseDb.Fakes.StubIDocumentResult<JArray>();
                        result.SuccessGet = () => false;
                        return result;
                    });
                });

                bucket.InsertAsyncOf1IDocumentOfM0<JArray>((document) =>
                {
                    return Task<CouchbaseDb.IDocumentResult<JArray>>.Factory.StartNew(() =>
                    {
                        called++;
                        if (called < 2)
                        {
                            var result = new CouchbaseDb.Fakes.StubIDocumentResult<JArray>();
                            result.SuccessGet = () => false;
                            result.StatusGet = () => ResponseStatus.KeyExists;
                            return result;
                        }
                        else
                        {
                            insertCalled = true;

                            Assert.AreEqual(expectedId, document.Id);

                            Assert.AreEqual(entityKey, document.Content[0]);

                            var result = new CouchbaseDb.Fakes.StubIDocumentResult<JArray>();
                            result.SuccessGet = () => true;
                            return result;
                        }
                    });
                });

                var command = new AddToCollectionCommand(entityKey, type);
                command.Execute("test").Wait();

                if (!insertCalled)
                {
                    Assert.Fail();
                }
            }
        }

        [TestMethod]
        public void Execute_ReplaceFailsDueToExistingKey_Retrys()
        {
            var replaceCalled = false;
            using (ShimsContext.Create())
            {
                var type = _model.FindDeclaredType("Test.AllTypes") as IEdmEntityType;
                var entityKey = "test-key";
                var expectedId = Helpers.CreateCollectionId("test", type);
                var called = 0;
                var existingArray = new JArray("id-1", "id-2");

                CouchbaseDb.Fakes.ShimCluster.ConstructorString = (i, v) => { };

                var bucket = new CouchbaseDb.Core.Fakes.StubIBucket();
                Fakes.ShimBucketProvider.GetBucket = () => bucket;
                bucket.GetDocumentAsyncOf1String((id) =>
                {
                    return Task<CouchbaseDb.IDocumentResult<JArray>>.Factory.StartNew(() =>
                    {
                        Assert.AreEqual(expectedId, id);

                        var result = new CouchbaseDb.Fakes.StubIDocumentResult<JArray>();
                        var document = new CouchbaseDb.Fakes.StubDocument<JArray>();
                        document.Content = existingArray;
                        document.Id = id;
                        result.SuccessGet = () => true;
                        result.ContentGet = () => document.Content;
                        result.DocumentGet = () => document;
                        return result;
                    });
                    
                });

                bucket.ReplaceAsyncOf1IDocumentOfM0<JArray>((document) =>
                {
                    return Task<CouchbaseDb.IDocumentResult<JArray>>.Factory.StartNew(() =>
                    {
                        called++;
                        if (called < 2)
                        {
                            existingArray.Remove(existingArray[2]);
                            var result = new CouchbaseDb.Fakes.StubIDocumentResult<JArray>();
                            result.SuccessGet = () => false;
                            result.StatusGet = () => ResponseStatus.KeyExists;
                            return result;
                        }
                        else
                        {
                            replaceCalled = true;

                            Assert.AreEqual(expectedId, document.Id);

                            Assert.AreEqual(3, document.Content.Count);
                            Assert.AreEqual(entityKey, document.Content[2]);

                            var result = new CouchbaseDb.Fakes.StubIDocumentResult<JArray>();
                            result.SuccessGet = () => true;
                            return result;
                        }
                    });
                });

                var command = new AddToCollectionCommand(entityKey, type);
                command.Execute("test").Wait();

                if (!replaceCalled)
                {
                    Assert.Fail();
                }
            }
        }
    }
}
