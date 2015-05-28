using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.OData;
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
    public class ReplaceCommandTests
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
        public void Execute_KeyNotChanged_ReplacesDbEntry()
        {
            using (ShimsContext.Create())
            {
                var replaceCalled = false;
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
                            Assert.AreEqual(ConvertOptions.None, arg5);

                            return document1;
                        });
                    };

                var bucket = new CouchbaseDb.Core.Fakes.StubIBucket();
                Fakes.ShimBucketProvider.GetBucket = () => bucket;

                bucket.GetDocumentAsyncOf1String<JObject>((id) =>
                {
                    return Task<CouchbaseDb.IDocumentResult<JObject>>.Factory.StartNew(() =>
                    {
                        var result = new CouchbaseDb.Fakes.StubIDocumentResult<JObject>();
                        result.SuccessGet = () => true;
                        result.DocumentGet = () => new StubDocument<JObject>()
                        {
                            Content = document1,
                            Id = id
                        };
                        return result;
                    });
                });

                bucket.ReplaceAsyncOf1IDocumentOfM0<JObject>((doc) =>
                {
                    return Task<CouchbaseDb.IDocumentResult<JObject>>.Factory.StartNew(() =>
                    {
                        replaceCalled = true;

                        Assert.AreEqual(expectedId, doc.Id);
                        Assert.AreEqual(document1, doc.Content);

                        var result = new CouchbaseDb.Fakes.StubIDocumentResult<JObject>();
                        result.SuccessGet = () => true;
                        result.DocumentGet = () => new StubDocument<JObject>()
                        {
                            Content = document1
                        };
                        return result;
                    });
                });

                var command = new ReplaceCommand(keys, entity, type, _model);
                var output = command.Execute("test").Result;

                object value;
                Assert.IsTrue(output.TryGetPropertyValue("Int32", out value));
                Assert.AreEqual(1, value);

                Assert.IsTrue(replaceCalled);
            }
        }

        [TestMethod]
        public void Execute_ReplaceFails_ThrowsDbException()
        {
            using (ShimsContext.Create())
            {
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
                            Assert.AreEqual(ConvertOptions.None, arg5);

                            return document1;
                        });
                    };

                var bucket = new CouchbaseDb.Core.Fakes.StubIBucket();
                Fakes.ShimBucketProvider.GetBucket = () => bucket;

                bucket.GetDocumentAsyncOf1String<JObject>((id) =>
                {
                    return Task<CouchbaseDb.IDocumentResult<JObject>>.Factory.StartNew(() =>
                    {
                        var result = new CouchbaseDb.Fakes.StubIDocumentResult<JObject>();
                        result.SuccessGet = () => true;
                        result.DocumentGet = () => new StubDocument<JObject>()
                        {
                            Content = document1,
                            Id = id
                        };
                        return result;
                    });
                });

                bucket.ReplaceAsyncOf1IDocumentOfM0<JObject>((doc) =>
                {
                    return Task<CouchbaseDb.IDocumentResult<JObject>>.Factory.StartNew(() =>
                    {
                        Assert.AreEqual(expectedId, doc.Id);
                        Assert.AreEqual(document1, doc.Content);

                        var result = new CouchbaseDb.Fakes.StubIDocumentResult<JObject>();
                        result.SuccessGet = () => false;
                        return result;
                    });
                });

                try
                {
                    var command = new ReplaceCommand(keys, entity, type, _model);
                    var output = command.Execute("test").Result;
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
        public void Execute_GetFails_ThrowsDbException()
        {
            using (ShimsContext.Create())
            {
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
                            Assert.AreEqual(ConvertOptions.None, arg5);

                            return document1;
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

                try
                {
                    var command = new ReplaceCommand(keys, entity, type, _model);
                    var output = command.Execute("test").Result;
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
        public void Execute_KeyChanged_RemovesAndInserts()
        {
            using (ShimsContext.Create())
            {
                var removeCalled = false;
                var insertCalled = false;
                var addToCollectionCalled = false;
                var removeFromCollectionCalled = false;
                var type = _model.FindDeclaredType("Test.SimpleWithKey") as IEdmEntityType;
                var keys = new Dictionary<string, object> { { "Int32", 1 } };
                var keys2 = new Dictionary<string, object> { { "Int32", 2 } };
                var expectedId = Helpers.CreateEntityId("test", keys, type).Result;
                var expectedId2 = Helpers.CreateEntityId("test", keys2, type).Result;
                var document1 = new JObject() { { "Int32", 1 }, { "Prop1", "value1" }, { "Prop2", "value2" } };
                var document2 = new JObject() { { "Int32", 2 }, { "Prop1", "value1" }, { "Prop2", "value2" } };
                var entity = new EdmEntityObject(type);
                entity.TrySetPropertyValue("Int32", 2);

                CouchbaseDb.Fakes.ShimCluster.ConstructorString = (i, v) => { };

                Fakes.ShimEntityObjectConverter.AllInstances
                    .ToDocumentEdmEntityObjectStringIEdmEntityTypeConvertOptionsIEdmModel =
                    (converter, o, arg3, arg4, arg5, arg6) =>
                    {
                        return Task<JObject>.Factory.StartNew(() =>
                        {
                            Assert.AreEqual(ConvertOptions.None, arg5);

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
                        result.SuccessGet = () => true;
                        result.DocumentGet = () => new StubDocument<JObject>()
                        {
                            Content = document1,
                            Id = id
                        };
                        return result;
                    });
                });

                bucket.RemoveAsyncString = (id) =>
                {
                    return Task<CouchbaseDb.IOperationResult>.Factory.StartNew(() =>
                    {
                        removeCalled = true;

                        Assert.AreEqual(expectedId, id);

                        var result = new CouchbaseDb.Fakes.StubIOperationResult();
                        result.SuccessGet = () => true;
                        return result;
                    });
                };

                bucket.InsertAsyncOf1IDocumentOfM0<JObject>((doc) =>
                {
                    return Task<CouchbaseDb.IDocumentResult<JObject>>.Factory.StartNew(() =>
                    {
                        insertCalled = true;

                        Assert.AreEqual(expectedId2, doc.Id);
                        Assert.AreEqual(document2, doc.Content);

                        var result = new CouchbaseDb.Fakes.StubIDocumentResult<JObject>();
                        result.SuccessGet = () => true;
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

                Couchbase.Commands.Fakes.ShimRemoveFromCollectionCommand.AllInstances.ExecuteString =
                    (collectionCommand, id) =>
                    {
                        return Task.Factory.StartNew(() =>
                        {
                            removeFromCollectionCalled = true;
                        });
                    };

                var command = new ReplaceCommand(keys, entity, type, _model);
                var output = command.Execute("test").Result;

                object value;
                Assert.IsTrue(output.TryGetPropertyValue("Int32", out value));
                Assert.AreEqual(2, value);

                Assert.IsTrue(removeCalled);
                Assert.IsTrue(insertCalled);
                Assert.IsTrue(addToCollectionCalled);
                Assert.IsTrue(removeFromCollectionCalled);
            }
        }

        [TestMethod]
        public void Execute_KeyChangedRemoveFails_ThrowsDbException()
        {
            using (ShimsContext.Create())
            {
                var removeCalled = false;
                var insertCalled = false;
                var addToCollectionCalled = false;
                var removeFromCollectionCalled = false;
                var type = _model.FindDeclaredType("Test.SimpleWithKey") as IEdmEntityType;
                var keys = new Dictionary<string, object> { { "Int32", 1 } };
                var keys2 = new Dictionary<string, object> { { "Int32", 2 } };
                var expectedId = Helpers.CreateEntityId("test", keys, type).Result;
                var expectedId2 = Helpers.CreateEntityId("test", keys2, type).Result;
                var document1 = new JObject() { { "Int32", 1 }, { "Prop1", "value1" }, { "Prop2", "value2" } };
                var document2 = new JObject() { { "Int32", 2 }, { "Prop1", "value1" }, { "Prop2", "value2" } };
                var entity = new EdmEntityObject(type);
                entity.TrySetPropertyValue("Int32", 2);

                CouchbaseDb.Fakes.ShimCluster.ConstructorString = (i, v) => { };

                Fakes.ShimEntityObjectConverter.AllInstances
                    .ToDocumentEdmEntityObjectStringIEdmEntityTypeConvertOptionsIEdmModel =
                    (converter, o, arg3, arg4, arg5, arg6) =>
                    {
                        return Task<JObject>.Factory.StartNew(() =>
                        {
                            Assert.AreEqual(ConvertOptions.None, arg5);

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
                        result.SuccessGet = () => true;
                        result.DocumentGet = () => new StubDocument<JObject>()
                        {
                            Content = document1,
                            Id = id
                        };
                        return result;
                    });
                });

                bucket.RemoveAsyncString = (id) =>
                {
                    return Task<CouchbaseDb.IOperationResult>.Factory.StartNew(() =>
                    {
                        removeCalled = true;

                        Assert.AreEqual(expectedId, id);

                        var result = new CouchbaseDb.Fakes.StubIOperationResult();
                        result.SuccessGet = () => false;
                        return result;
                    });
                };

                bucket.InsertAsyncOf1IDocumentOfM0<JObject>((doc) =>
                {
                    return Task<CouchbaseDb.IDocumentResult<JObject>>.Factory.StartNew(() =>
                    {
                        insertCalled = true;
                        var result = new CouchbaseDb.Fakes.StubIDocumentResult<JObject>();
                        result.SuccessGet = () => true;
                        return result;
                    });
                });

                Couchbase.Commands.Fakes.ShimAddToCollectionCommand.AllInstances.ExecuteString =
                    (collectionCommand, id) =>
                    {
                        return Task.Factory.StartNew(() =>
                        {
                            Assert.AreEqual(expectedId2, id);
                            addToCollectionCalled = true;
                        });
                    };

                Couchbase.Commands.Fakes.ShimRemoveFromCollectionCommand.AllInstances.ExecuteString =
                    (collectionCommand, id) =>
                    {
                        return Task.Factory.StartNew(() =>
                        {
                            Assert.AreEqual(expectedId, id);
                            removeFromCollectionCalled = true;
                        });
                    };

                try
                {
                    var command = new ReplaceCommand(keys, entity, type, _model);
                    var output = command.Execute("test").Result;
                }
                catch (AggregateException exception)
                {
                    Assert.AreEqual(1, exception.InnerExceptions.Count);
                    var firstException = exception.InnerExceptions.First();
                    Assert.IsInstanceOfType(firstException, typeof(DbException));
                }

                Assert.IsTrue(removeCalled);
                Assert.IsTrue(insertCalled);
                Assert.IsFalse(addToCollectionCalled);
                Assert.IsFalse(removeFromCollectionCalled);
            }
        }

        [TestMethod]
        public void Execute_KeyChangedInsertFails_ThrowsDbException()
        {
            using (ShimsContext.Create())
            {
                var removeCalled = false;
                var insertCalled = false;
                var addToCollectionCalled = false;
                var removeFromCollectionCalled = false;
                var type = _model.FindDeclaredType("Test.SimpleWithKey") as IEdmEntityType;
                var keys = new Dictionary<string, object> { { "Int32", 1 } };
                var keys2 = new Dictionary<string, object> { { "Int32", 2 } };
                var expectedId = Helpers.CreateEntityId("test", keys, type).Result;
                var expectedId2 = Helpers.CreateEntityId("test", keys2, type).Result;
                var document1 = new JObject() { { "Int32", 1 }, { "Prop1", "value1" }, { "Prop2", "value2" } };
                var document2 = new JObject() { { "Int32", 2 }, { "Prop1", "value1" }, { "Prop2", "value2" } };
                var entity = new EdmEntityObject(type);
                entity.TrySetPropertyValue("Int32", 2);

                CouchbaseDb.Fakes.ShimCluster.ConstructorString = (i, v) => { };

                Fakes.ShimEntityObjectConverter.AllInstances
                    .ToDocumentEdmEntityObjectStringIEdmEntityTypeConvertOptionsIEdmModel =
                    (converter, o, arg3, arg4, arg5, arg6) =>
                    {
                        return Task<JObject>.Factory.StartNew(() =>
                        {
                            Assert.AreEqual(ConvertOptions.None, arg5);

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
                        result.SuccessGet = () => true;
                        result.DocumentGet = () => new StubDocument<JObject>()
                        {
                            Content = document1,
                            Id = id
                        };
                        return result;
                    });
                });

                bucket.RemoveAsyncString = (id) =>
                {
                    return Task<CouchbaseDb.IOperationResult>.Factory.StartNew(() =>
                    {
                        removeCalled = true;

                        Assert.AreEqual(expectedId, id);

                        var result = new CouchbaseDb.Fakes.StubIOperationResult();
                        result.SuccessGet = () => true;
                        return result;
                    });
                };

                bucket.InsertAsyncOf1IDocumentOfM0<JObject>((doc) =>
                {
                    return Task<CouchbaseDb.IDocumentResult<JObject>>.Factory.StartNew(() =>
                    {
                        insertCalled = true;
                        var result = new CouchbaseDb.Fakes.StubIDocumentResult<JObject>();
                        result.SuccessGet = () => false;
                        return result;
                    });
                });

                Couchbase.Commands.Fakes.ShimAddToCollectionCommand.AllInstances.ExecuteString =
                    (collectionCommand, id) =>
                    {
                        return Task.Factory.StartNew(() =>
                        {
                            Assert.AreEqual(expectedId2, id);
                            addToCollectionCalled = true;
                        });
                    };

                Couchbase.Commands.Fakes.ShimRemoveFromCollectionCommand.AllInstances.ExecuteString =
                    (collectionCommand, id) =>
                    {
                        return Task.Factory.StartNew(() =>
                        {
                            Assert.AreEqual(expectedId, id);
                            removeFromCollectionCalled = true;
                        });
                    };

                try
                {
                    var command = new ReplaceCommand(keys, entity, type, _model);
                    var output = command.Execute("test").Result;
                }
                catch (AggregateException exception)
                {
                    Assert.AreEqual(1, exception.InnerExceptions.Count);
                    var firstException = exception.InnerExceptions.First();
                    Assert.IsInstanceOfType(firstException, typeof(DbException));
                }

                Assert.IsFalse(removeCalled);
                Assert.IsTrue(insertCalled);
                Assert.IsFalse(addToCollectionCalled);
                Assert.IsFalse(removeFromCollectionCalled);
            }
        }
    }
}
