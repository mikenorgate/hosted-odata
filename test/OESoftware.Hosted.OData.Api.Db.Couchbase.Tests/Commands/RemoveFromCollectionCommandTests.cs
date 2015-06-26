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
    public class RemoveFromCollectionCommandTests
    {
        [TestMethod]
        public void Execute_ExistingArrary_RemovesId()
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

                var command = new RemoveFromCollectionCommand();
                command.Execute("test", entityKey, type).Wait();

                var result = (string[])bucket.Items[expectedId];

                Assert.AreEqual(2, result.Length);
                Assert.IsTrue(result.Contains("id-1"));
                Assert.IsTrue(result.Contains("id-2"));
            }
        }

        [TestMethod]
        public void Execute_ReplaceFailsDueToExistingKey_Retrys()
        {
            using (ShimsContext.Create())
            {
                var type = typeof(TestEntity);
                var entityKey = "test-key";
                var expectedId = Helpers.CreateCollectionId("test", type.FullName);
                var called = 0;
                var existingArray = new string[] { "id-1", "id-2", "test-key" };

                CouchbaseDb.Fakes.ShimCluster.ConstructorString = (i, v) => { };

                var bucket = new TestBucket();
                Fakes.ShimBucketProvider.GetBucket = () => bucket;

                bucket.Items.Add(expectedId, existingArray);
                bucket.Cas.Add(expectedId, 1);

                bucket.ReplaceBefore = (s, o, arg3) =>
                {
                    called++;
                    return called < 2;
                };

                var command = new RemoveFromCollectionCommand();
                command.Execute("test", entityKey, type).Wait();

                var result = (string[])bucket.Items[expectedId];

                Assert.AreEqual(2, result.Length);
                Assert.IsTrue(result.Contains("id-1"));
                Assert.IsTrue(result.Contains("id-2"));
            }
        }

        [TestMethod]
        public void Execute_IdNotInArrary_DoesNothing()
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

                var command = new RemoveFromCollectionCommand();
                command.Execute("test", entityKey, type).Wait();

                var result = (string[])bucket.Items[expectedId];

                Assert.AreEqual(2, result.Length);
                Assert.IsTrue(result.Contains("id-1"));
                Assert.IsTrue(result.Contains("id-2"));
            }
        }

        [TestMethod]
        public void Execute_KeyDoesNotExist_DoesNothing()
        {
            using (ShimsContext.Create())
            {
                var type = typeof(TestEntity);
                var entityKey = "test-key";

                CouchbaseDb.Fakes.ShimCluster.ConstructorString = (i, v) => { };

                var bucket = new TestBucket();
                Fakes.ShimBucketProvider.GetBucket = () => bucket;

                var command = new RemoveFromCollectionCommand();
                command.Execute("test", entityKey, type).Wait();
            }
        }
    }
}
