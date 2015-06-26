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
        [TestMethod]
        public void Execute_EntityExists_RemovesItemAndRemovesFromCollection()
        {
            using (ShimsContext.Create())
            {
                var type = typeof(TestEntity);
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

                var command = new DeleteCommand();
                command.Execute("test", keys, type).Wait();

                Assert.IsTrue(bucket.Items.ContainsKey(expectedId2));
                Assert.IsFalse(bucket.Items.ContainsKey(expectedId));

                var remainingKeys = (string[])bucket.Items[collectionId];
                Assert.AreEqual(1, remainingKeys.Length);
                Assert.IsTrue(remainingKeys.Contains(expectedId2));
            }
        }

        [TestMethod]
        public void Execute_EntityDoesNotExists_ThrowsDbException()
        {
            using (ShimsContext.Create())
            {
                var type = typeof(TestEntity);
                var keys = new Dictionary<string, object> { { "Int32", 123 } };

                CouchbaseDb.Fakes.ShimCluster.ConstructorString = (i, v) => { };

                var bucket = new TestBucket();
                Fakes.ShimBucketProvider.GetBucket = () => bucket;

                try
                {
                    var command = new DeleteCommand();
                    command.Execute("test", keys, type).Wait();
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
