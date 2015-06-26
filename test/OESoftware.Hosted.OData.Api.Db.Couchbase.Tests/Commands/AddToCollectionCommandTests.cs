using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        [TestMethod]
        public void Execute_CreatesNewCollectionAndAddsId_InsertsId()
        {
            using (ShimsContext.Create())
            {
                var type = typeof(TestEntity);
                var entityKey = "test-key";
                var expectedId = Helpers.CreateCollectionId("test", type.FullName);

                CouchbaseDb.Fakes.ShimCluster.ConstructorString = (i, v) => { };

                var bucket = new TestBucket();
                Fakes.ShimBucketProvider.GetBucket = () => bucket;

                var command = new AddToCollectionCommand();
                command.Execute("test", entityKey, type).Wait();

                var collection = (string[])bucket.Items[expectedId];
                Assert.AreEqual(1, collection.Length);
                Assert.IsTrue(collection.Contains(entityKey));
            }
        }

        [TestMethod]
        public void Execute_AppendsCollectionAndAddsId_AppendsId()
        {
            using (ShimsContext.Create())
            {
                var type = typeof(TestEntity);
                var entityKey = "test-key";
                var existingArray = new string[] { "id-1", "id-2" };
                var expectedId = Helpers.CreateCollectionId("test", type.FullName);

                CouchbaseDb.Fakes.ShimCluster.ConstructorString = (i, v) => { };

                var bucket = new TestBucket();
                Fakes.ShimBucketProvider.GetBucket = () => bucket;

                bucket.Items.Add(expectedId, existingArray);
                bucket.Cas.Add(expectedId, 1);

                var command = new AddToCollectionCommand();
                command.Execute("test", entityKey, type).Wait();

                var collection = (string[])bucket.Items[expectedId];
                Assert.AreEqual(3, collection.Length);
                Assert.IsTrue(collection.Contains(entityKey));
            }
        }

        [TestMethod]
        public void Execute_IdAlreadyInArrary_DoesNothing()
        {
            using (ShimsContext.Create())
            {
                var type = typeof(TestEntity);
                var entityKey = "test-key";
                var existingArray = new string[] { "id-1", "id-2", "test-key" };
                var expectedId = Helpers.CreateCollectionId("test", type.FullName);

                CouchbaseDb.Fakes.ShimCluster.ConstructorString = (i, v) => { };

                var bucket = new TestBucket();
                Fakes.ShimBucketProvider.GetBucket = () => bucket;

                bucket.Items.Add(expectedId, existingArray);
                bucket.Cas.Add(expectedId, 1);

                var command = new AddToCollectionCommand();
                command.Execute("test", entityKey, type).Wait();

                var collection = (string[])bucket.Items[expectedId];
                Assert.AreEqual(3, collection.Length);
                Assert.IsTrue(collection.Contains(entityKey));
            }
        }

        [TestMethod]
        public void Execute_InsertFailsDueToExistingKey_Retrys()
        {
            using (ShimsContext.Create())
            {
                var type = typeof(TestEntity);
                var entityKey = "test-key";
                var expectedId = Helpers.CreateCollectionId("test", type.FullName);
                var called = 0;

                CouchbaseDb.Fakes.ShimCluster.ConstructorString = (i, v) => { };

                var bucket = new TestBucket();
                Fakes.ShimBucketProvider.GetBucket = () => bucket;

                bucket.InsertBefore = (s, o) =>
                {
                    called++;
                    return called < 2;
                };

                var command = new AddToCollectionCommand();
                command.Execute("test", entityKey, type).Wait();

                var collection = (string[])bucket.Items[expectedId];
                Assert.AreEqual(1, collection.Length);
                Assert.IsTrue(collection.Contains(entityKey));
            }
        }

        [TestMethod]
        public void Execute_ReplaceFailsDueToExistingKey_Retrys()
        {
            using (ShimsContext.Create())
            {
                var type = typeof(TestEntity);
                var entityKey = "test-key";
                var existingArray = new string[] { "id-1", "id-2" };
                var expectedId = Helpers.CreateCollectionId("test", type.FullName);
                var count = 0;

                CouchbaseDb.Fakes.ShimCluster.ConstructorString = (i, v) => { };

                var bucket = new TestBucket();
                Fakes.ShimBucketProvider.GetBucket = () => bucket;

                bucket.Items.Add(expectedId, existingArray);
                bucket.Cas.Add(expectedId, 1);

                bucket.ReplaceBefore = (s, o, arg3) =>
                {
                    count++;
                    return count < 2;
                };

                var command = new AddToCollectionCommand();
                command.Execute("test", entityKey, type).Wait();

                var collection = (string[])bucket.Items[expectedId];
                Assert.AreEqual(3, collection.Length);
                Assert.IsTrue(collection.Contains(entityKey));
            }
        }
    }
}
