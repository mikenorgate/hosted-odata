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
using Microsoft.OData.Edm.Library;
using Microsoft.OData.Edm.Validation;
using Microsoft.QualityTools.Testing.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using OESoftware.Hosted.OData.Api.Core;
using OESoftware.Hosted.OData.Api.Db.Couchbase.Commands;
using OESoftware.Hosted.OData.Api.Tests.Core;
using CouchbaseDb = Couchbase;

namespace OESoftware.Hosted.OData.Api.Db.Couchbase.Tests.Commands
{

    [TestClass]
    public class UpdateSingletonCommandTests
    {
        [TestMethod]
        public void Execute_AlreadyExists_ReplacesDbEntry()
        {
            using (ShimsContext.Create())
            {
                var type = typeof(TestEntity);
                var expectedId = Helpers.CreateSingletonId("test", type.FullName);
                var document1 = new TestEntity() { Int32 = 1, Prop1 = "value1", Prop2 = "value2" };
                var delta = new Delta<TestEntity>();
                delta.TrySetPropertyValue("Prop1", "value2");

                CouchbaseDb.Fakes.ShimCluster.ConstructorString = (i, v) => { };

                var bucket = new TestBucket();
                bucket.Items.Add(expectedId, document1);
                bucket.Cas.Add(expectedId, 1L);
                Fakes.ShimBucketProvider.GetBucket = () => bucket;

                var command = new UpdateSingletonCommand(new TestValueGenerator());
                var output = command.Execute("test", type, delta, false).Result;

                var content = (TestEntity)bucket.Items[expectedId];
                Assert.AreEqual(1, content.Int32);
                Assert.AreEqual("value2", content.Prop1);
                Assert.AreEqual("value2", content.Prop2);

                Assert.AreEqual(content, output);
            }
        }

        [TestMethod]
        public void Execute_AlreadyExistsPut_ReplacesDbEntry()
        {
            using (ShimsContext.Create())
            {
                var type = typeof(TestEntity);
                var expectedId = Helpers.CreateSingletonId("test", type.FullName);
                var document1 = new TestEntity() { Int32 = 1, Prop1 = "value1", Prop2 = "value2" };
                var delta = new Delta<TestEntity>();
                delta.TrySetPropertyValue("Prop1", "value2");

                CouchbaseDb.Fakes.ShimCluster.ConstructorString = (i, v) => { };

                var bucket = new TestBucket();
                bucket.Items.Add(expectedId, document1);
                bucket.Cas.Add(expectedId, 1L);
                Fakes.ShimBucketProvider.GetBucket = () => bucket;

                var command = new UpdateSingletonCommand(new TestValueGenerator());
                var output = command.Execute("test", type, delta, true).Result;

                var content = (TestEntity)bucket.Items[expectedId];
                Assert.AreEqual(0, content.Int32);
                Assert.AreEqual("value2", content.Prop1);
                Assert.AreEqual(null, content.Prop2);

                Assert.AreEqual(content, output);
            }
        }

        [TestMethod]
        public void Execute_ReplaceFails_ThrowsDbException()
        {
            using (ShimsContext.Create())
            {
                var type = typeof(TestEntity);
                var expectedId = Helpers.CreateSingletonId("test", type.FullName);
                var document1 = new TestEntity() { Int32 = 1, Prop1 = "value1", Prop2 = "value2" };
                var delta = new Delta<TestEntity>();
                delta.TrySetPropertyValue("Prop1", "value2");

                CouchbaseDb.Fakes.ShimCluster.ConstructorString = (i, v) => { };

                var bucket = new TestBucket();
                bucket.Items.Add(expectedId, document1);
                bucket.Cas.Add(expectedId, 1L);
                Fakes.ShimBucketProvider.GetBucket = () => bucket;

                bucket.ReplaceBefore = (s, o, arg3) =>
                {
                    return false;
                };

                try
                {
                    var command = new UpdateSingletonCommand(new TestValueGenerator());
                    var output = command.Execute("test", type, delta, false).Result;
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
        public void Execute_DoesNotExists_Inserts()
        {
            using (ShimsContext.Create())
            {
                var insertCalled = false;
                var type = typeof(TestEntity);
                var expectedId = Helpers.CreateSingletonId("test", type.FullName);
                var delta = new Delta<TestEntity>();
                delta.TrySetPropertyValue("Prop1", "value2");

                CouchbaseDb.Fakes.ShimCluster.ConstructorString = (i, v) => { };

                var bucket = new TestBucket();
                Fakes.ShimBucketProvider.GetBucket = () => bucket;

                var command = new UpdateSingletonCommand(new TestValueGenerator());
                var output = (TestEntity)command.Execute("test", type, delta, false).Result;

                var inserted = bucket.Items[expectedId];
                Assert.AreEqual(inserted, output);

                Assert.AreEqual(0, output.Int32);
                Assert.AreEqual("value2", output.Prop1);
                Assert.AreEqual(null, output.Prop2);

            }
        }

        [TestMethod]
        public void Execute_InsertFails_ThrowsDbException()
        {
            using (ShimsContext.Create())
            {
                var type = typeof(TestEntity);
                var delta = new Delta<TestEntity>();
                delta.TrySetPropertyValue("Prop1", "value2");

                CouchbaseDb.Fakes.ShimCluster.ConstructorString = (i, v) => { };

                var bucket = new TestBucket();
                Fakes.ShimBucketProvider.GetBucket = () => bucket;

                bucket.InsertBefore = (s, o) =>
                {
                    return false;
                };

                try
                {
                    var command = new UpdateSingletonCommand(new TestValueGenerator());
                    var output = command.Execute("test", type, delta, false).Result;
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

    }
}
