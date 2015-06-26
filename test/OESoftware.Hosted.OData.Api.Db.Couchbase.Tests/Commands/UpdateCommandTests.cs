using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
using OESoftware.Hosted.OData.Api.Core;
using OESoftware.Hosted.OData.Api.Db.Couchbase.Commands;
using CouchbaseDb = Couchbase;

namespace OESoftware.Hosted.OData.Api.Db.Couchbase.Tests.Commands
{

    [TestClass]
    public class UpdateCommandTests
    {
        [TestMethod]
        public void Execute_KeyNotChanged_ReplacesDbEntry()
        {
            using (ShimsContext.Create())
            {
                var type = typeof(TestEntity);
                var keys = new Dictionary<string, object> { { "Int32", 1 } };
                var expectedId = Helpers.CreateEntityId("test", keys, type.FullName).Result;
                var document1 = new TestEntity() { Int32 = 1, Prop1 = "value1", Prop2 = "value2" };
                var delta = new Delta<TestEntity>();
                delta.TrySetPropertyValue("Prop1", "value2");

                CouchbaseDb.Fakes.ShimCluster.ConstructorString = (i, v) => { };

                var bucket = new TestBucket();
                Fakes.ShimBucketProvider.GetBucket = () => bucket;

                bucket.Items.Add(expectedId, document1);
                bucket.Cas.Add(expectedId, 1);

                var command = new UpdateCommand();
                var output = (TestEntity)command.Execute("test", keys, type, delta, false).Result;


                var content = (TestEntity)bucket.Items[expectedId];
                Assert.AreEqual(1, content.Int32);
                Assert.AreEqual("value2", content.Prop1);
                Assert.AreEqual("value2", content.Prop2);

                Assert.AreEqual(output, content);
            }
        }

        [TestMethod]
        public void Execute_KeyNotChangedPut_ReplacesDbEntry()
        {
            using (ShimsContext.Create())
            {
                var type = typeof(TestEntity);
                var keys = new Dictionary<string, object> { { "Int32", 1 } };
                var expectedId = Helpers.CreateEntityId("test", keys, type.FullName).Result;
                var document1 = new TestEntity() { Int32 = 1, Prop1 = "value1", Prop2 = "value2" };
                var delta = new Delta<TestEntity>();
                delta.TrySetPropertyValue("Prop1", "value2");
                delta.TrySetPropertyValue("Int32", 1);

                CouchbaseDb.Fakes.ShimCluster.ConstructorString = (i, v) => { };

                var bucket = new TestBucket();
                Fakes.ShimBucketProvider.GetBucket = () => bucket;

                bucket.Items.Add(expectedId, document1);
                bucket.Cas.Add(expectedId, 1);

                var command = new UpdateCommand();
                var output = (TestEntity)command.Execute("test", keys, type, delta, true).Result;

                var content = (TestEntity)bucket.Items[expectedId];
                Assert.AreEqual(1, content.Int32);
                Assert.AreEqual("value2", content.Prop1);
                Assert.AreEqual(null, content.Prop2);

                Assert.AreEqual(output, content);
            }
        }

        [TestMethod]
        public void Execute_ReplaceFails_ThrowsDbException()
        {
            using (ShimsContext.Create())
            {
                var type = typeof(TestEntity);
                var keys = new Dictionary<string, object> { { "Int32", 1 } };
                var expectedId = Helpers.CreateEntityId("test", keys, type.FullName).Result;
                var document1 = new TestEntity() { Int32 = 1, Prop1 = "value1", Prop2 = "value2" };
                var delta = new Delta<TestEntity>();
                delta.TrySetPropertyValue("Prop1", "value2");

                CouchbaseDb.Fakes.ShimCluster.ConstructorString = (i, v) => { };

                var bucket = new TestBucket();
                Fakes.ShimBucketProvider.GetBucket = () => bucket;

                bucket.Items.Add(expectedId, document1);
                bucket.Cas.Add(expectedId, 1);

                bucket.ReplaceBefore = (s, o, arg3) => false;

                try
                {
                    var command = new UpdateCommand();
                    var output = command.Execute("test", keys, type, delta, false).Result;
                    Assert.Fail();
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
                var type = typeof(TestEntity);
                var keys = new Dictionary<string, object> { { "Int32", 1 } };
                var delta = new Delta<TestEntity>();
                delta.TrySetPropertyValue("Int32", 2);

                CouchbaseDb.Fakes.ShimCluster.ConstructorString = (i, v) => { };

                var bucket = new TestBucket();
                Fakes.ShimBucketProvider.GetBucket = () => bucket;

                try
                {
                    var command = new UpdateCommand();
                    var output = command.Execute("test", keys, type, delta, false).Result;
                    Assert.Fail();
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
                var type = typeof(TestEntity);
                var keys = new Dictionary<string, object> { { "Int32", 1 } };
                var keys2 = new Dictionary<string, object> { { "Int32", 2 } };
                var expectedId = Helpers.CreateEntityId("test", keys, type.FullName).Result;
                var expectedId2 = Helpers.CreateEntityId("test", keys2, type.FullName).Result;
                var document1 = new TestEntity() { Int32 = 1, Prop1 = "value1", Prop2 = "value2" };
                var delta = new Delta<TestEntity>();
                delta.TrySetPropertyValue("Prop1", "value2");
                delta.TrySetPropertyValue("Int32", 2);

                CouchbaseDb.Fakes.ShimCluster.ConstructorString = (i, v) => { };

                var bucket = new TestBucket();
                Fakes.ShimBucketProvider.GetBucket = () => bucket;

                bucket.Items.Add(expectedId, document1);
                bucket.Cas.Add(expectedId, 1);

                var command = new UpdateCommand();
                var output = (TestEntity)command.Execute("test", keys, type, delta, false).Result;

                var content = (TestEntity)bucket.Items[expectedId2];
                Assert.AreEqual(2, content.Int32);
                Assert.AreEqual("value2", content.Prop1);
                Assert.AreEqual("value2", content.Prop2);

                Assert.AreEqual(output, content);

                Assert.IsFalse(bucket.Items.ContainsKey(expectedId));
            }
        }

        [TestMethod]
        public void Execute_KeyChangedRemoveFails_ThrowsDbException()
        {
            using (ShimsContext.Create())
            {
                var type = typeof(TestEntity);
                var keys = new Dictionary<string, object> { { "Int32", 1 } };
                var keys2 = new Dictionary<string, object> { { "Int32", 2 } };
                var expectedId = Helpers.CreateEntityId("test", keys, type.FullName).Result;
                var expectedId2 = Helpers.CreateEntityId("test", keys2, type.FullName).Result;
                var document1 = new TestEntity() { Int32 = 1, Prop1 = "value1", Prop2 = "value2" };
                var delta = new Delta<TestEntity>();
                delta.TrySetPropertyValue("Prop1", "value2");
                delta.TrySetPropertyValue("Int32", 2);

                CouchbaseDb.Fakes.ShimCluster.ConstructorString = (i, v) => { };

                var bucket = new TestBucket();
                Fakes.ShimBucketProvider.GetBucket = () => bucket;

                bucket.Items.Add(expectedId, document1);
                bucket.Cas.Add(expectedId, 1);

                bucket.RemoveBefore = (s, arg2) =>
                {
                    return s != expectedId;
                };

                try
                {
                    var command = new UpdateCommand();
                    var output = command.Execute("test", keys, type, delta, false).Result;
                    Assert.Fail();
                }
                catch (AggregateException exception)
                {
                    Assert.AreEqual(1, exception.InnerExceptions.Count);
                    var firstException = exception.InnerExceptions.First();
                    Assert.IsInstanceOfType(firstException, typeof(DbException));
                }

                Assert.IsTrue(bucket.Items.ContainsKey(expectedId));
                Assert.IsFalse(bucket.Items.ContainsKey(expectedId2));
            }
        }

        [TestMethod]
        public void Execute_KeyChangedInsertFails_ThrowsDbException()
        {
            using (ShimsContext.Create())
            {
                var type = typeof(TestEntity);
                var keys = new Dictionary<string, object> { { "Int32", 1 } };
                var keys2 = new Dictionary<string, object> { { "Int32", 2 } };
                var expectedId = Helpers.CreateEntityId("test", keys, type.FullName).Result;
                var expectedId2 = Helpers.CreateEntityId("test", keys2, type.FullName).Result;
                var document1 = new TestEntity() { Int32 = 1, Prop1 = "value1", Prop2 = "value2" };
                var delta = new Delta<TestEntity>();
                delta.TrySetPropertyValue("Prop1", "value2");
                delta.TrySetPropertyValue("Int32", 2);

                CouchbaseDb.Fakes.ShimCluster.ConstructorString = (i, v) => { };

                var bucket = new TestBucket();
                Fakes.ShimBucketProvider.GetBucket = () => bucket;

                bucket.Items.Add(expectedId, document1);
                bucket.Cas.Add(expectedId, 1);

                bucket.InsertBefore = (s, o) => false;

                try
                {
                    var command = new UpdateCommand();
                    var output = command.Execute("test", keys, type, delta, false).Result;
                    Assert.Fail();
                }
                catch (AggregateException exception)
                {
                    Assert.AreEqual(1, exception.InnerExceptions.Count);
                    var firstException = exception.InnerExceptions.First();
                    Assert.IsInstanceOfType(firstException, typeof(DbException));
                }

                Assert.IsTrue(bucket.Items.ContainsKey(expectedId));
                Assert.IsFalse(bucket.Items.ContainsKey(expectedId2));
            }
        }
    }
}
