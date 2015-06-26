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
using OESoftware.Hosted.OData.Api.Core;
using OESoftware.Hosted.OData.Api.Db.Couchbase.Commands;
using CouchbaseDb = Couchbase;

namespace OESoftware.Hosted.OData.Api.Db.Couchbase.Tests.Commands
{
    [TestClass]
    public class GetAllCommandTests
    {
        [TestMethod]
        public void Execute_CollectionExists_ReturnsCollection()
        {
            using (ShimsContext.Create())
            {
                var type = typeof(TestEntity);
                var collectionId = Helpers.CreateCollectionId("test", type.FullName);

                var keys = new Dictionary<string, object> { { "Int32", 1 } };
                var expectedId = Helpers.CreateEntityId("test", keys, type.FullName).Result;
                var document1 = new TestEntity() { Int32 = 1, Prop1 = "value1", Prop2 = "value2" };

                var keys2 = new Dictionary<string, object> { { "Int32", 2 } };
                var expectedId2 = Helpers.CreateEntityId("test", keys2, type.FullName).Result;
                var document2 = new TestEntity() { Int32 = 2, Prop1 = "value1", Prop2 = "value2" };

                var collectionKeys = new string[] {expectedId, expectedId2};

                CouchbaseDb.Fakes.ShimCluster.ConstructorString = (i, v) => { };

                var bucket = new TestBucket();
                Fakes.ShimBucketProvider.GetBucket = () => bucket;

                bucket.Items.Add(expectedId, document1);
                bucket.Cas.Add(expectedId, 1);
                bucket.Items.Add(expectedId2, document2);
                bucket.Cas.Add(expectedId2, 1);
                bucket.Items.Add(collectionId, collectionKeys);
                bucket.Cas.Add(collectionId, 1);

                var command = new GetAllCommand();
                var collection = command.Execute("test", type).Result;

                Assert.AreEqual(2, collection.Count());
                var item1 = (TestEntity)collection.First(f=>((TestEntity)f).Int32 == 1);
                Assert.AreEqual(document1, item1);

                var item2 = (TestEntity)collection.First(f => ((TestEntity)f).Int32 == 2);
                Assert.AreEqual(document2, item2);
            }
        }

        [TestMethod]
        public void Execute_CollectionDoesNotExists_ThrowsDbException()
        {
            using (ShimsContext.Create())
            {
                var type = typeof(TestEntity);

                CouchbaseDb.Fakes.ShimCluster.ConstructorString = (i, v) => { };

                var bucket = new TestBucket();
                Fakes.ShimBucketProvider.GetBucket = () => bucket;

                try
                {
                    var command = new GetAllCommand();
                    var collection = command.Execute("test", type).Result;
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
        public void Execute_CollectionExistsOneMemberOfCollectionDoesNotExists_ReturnsCollectionIgnoringMissingElement()
        {
            using (ShimsContext.Create())
            {
                var type = typeof(TestEntity);
                var collectionId = Helpers.CreateCollectionId("test", type.FullName);

                var keys = new Dictionary<string, object> { { "Int32", 1 } };
                var expectedId = Helpers.CreateEntityId("test", keys, type.FullName).Result;
                var document1 = new TestEntity() { Int32 = 1, Prop1 = "value1", Prop2 = "value2" };

                var keys2 = new Dictionary<string, object> { { "Int32", 2 } };
                var expectedId2 = Helpers.CreateEntityId("test", keys2, type.FullName).Result;

                var collectionKeys = new string[] { expectedId, expectedId2 };

                CouchbaseDb.Fakes.ShimCluster.ConstructorString = (i, v) => { };

                var bucket = new TestBucket();
                Fakes.ShimBucketProvider.GetBucket = () => bucket;

                bucket.Items.Add(expectedId, document1);
                bucket.Cas.Add(expectedId, 1);
                bucket.Items.Add(collectionId, collectionKeys);
                bucket.Cas.Add(collectionId, 1);
                var command = new GetAllCommand();
                var collection = command.Execute("test", type).Result;

                Assert.AreEqual(1, collection.Count());
                var item1 = (TestEntity)collection.First();
                Assert.AreEqual(document1, item1);
            }
        }

        [TestMethod]
        public void Execute_Cast_ReturnsCollection()
        {
            using (ShimsContext.Create())
            {
                var type = typeof(SubTestEntity);
                var castType = typeof(TestEntity);
                var collectionId = Helpers.CreateCollectionId("test", type.FullName);

                var keys = new Dictionary<string, object> { { "Int32", 1 } };
                var expectedId = Helpers.CreateEntityId("test", keys, type.FullName).Result;
                var document1 = new SubTestEntity() { Int32 = 1, Prop1 = "value1", Prop2 = "value2" };

                var keys2 = new Dictionary<string, object> { { "Int32", 2 } };
                var expectedId2 = Helpers.CreateEntityId("test", keys2, type.FullName).Result;
                var document2 = new SubTestEntity() { Int32 = 2, Prop1 = "value1", Prop2 = "value2" };

                var collectionKeys = new string[] { expectedId, expectedId2 };

                CouchbaseDb.Fakes.ShimCluster.ConstructorString = (i, v) => { };

                var bucket = new TestBucket();
                Fakes.ShimBucketProvider.GetBucket = () => bucket;

                bucket.Items.Add(expectedId, document1);
                bucket.Cas.Add(expectedId, 1);
                bucket.Items.Add(expectedId2, document2);
                bucket.Cas.Add(expectedId2, 1);
                bucket.Items.Add(collectionId, collectionKeys);
                bucket.Cas.Add(collectionId, 1);

                var command = new GetAllCommand();
                var collection = command.Execute("test", type, castType).Result;

                Assert.AreEqual(2, collection.Count());
                var item1 = collection.First(f => ((TestEntity)f).Int32 == 1);
                Assert.IsInstanceOfType(item1, castType);
                Assert.AreEqual(document1, item1);

                var item2 = collection.First(f => ((TestEntity)f).Int32 == 2);
                Assert.IsInstanceOfType(item2, castType);
                Assert.AreEqual(document2, item2);
            }
        }
    }
}
