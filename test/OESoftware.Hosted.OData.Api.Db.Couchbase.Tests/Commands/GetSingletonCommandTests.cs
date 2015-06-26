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
using OESoftware.Hosted.OData.Api.Core;
using OESoftware.Hosted.OData.Api.Db.Couchbase.Commands;
using OESoftware.Hosted.OData.Api.Tests.Core;
using CouchbaseDb = Couchbase;

namespace OESoftware.Hosted.OData.Api.Db.Couchbase.Tests.Commands
{
    [TestClass]
    public class GetSingletonCommandTests
    {
        [TestMethod]
        public void Execute_SingletonExists_ReturnsEntity()
        {
            using (ShimsContext.Create())
            {
                var type = typeof(TestEntity);
                var expectedId = Helpers.CreateSingletonId("test", type.FullName);
                var document1 = new TestEntity() { Int32 = 1, Prop1 = "value1", Prop2 = "value2" };

                CouchbaseDb.Fakes.ShimCluster.ConstructorString = (i, v) => { };

                var bucket = new TestBucket();
                Fakes.ShimBucketProvider.GetBucket = () => bucket;

                bucket.Items.Add(expectedId, document1);
                bucket.Cas.Add(expectedId, 1);


                var command = new GetSingletonCommand(new TestValueGenerator());
                var entity = (TestEntity)command.Execute("test", type).Result;

                
                Assert.AreEqual(document1, entity);
            }
        }

        [TestMethod]
        public void Execute_DoesNotExists_Inserts()
        {
            using (ShimsContext.Create())
            {
                var type = typeof(TestEntity);
                var expectedId = Helpers.CreateSingletonId("test", type.FullName);

                CouchbaseDb.Fakes.ShimCluster.ConstructorString = (i, v) => { };

                var bucket = new TestBucket();
                Fakes.ShimBucketProvider.GetBucket = () => bucket;
            

                var command = new GetSingletonCommand(new TestValueGenerator());
                var output = (TestEntity)command.Execute("test", type).Result;

                var result = (TestEntity)bucket.Items[expectedId];

                Assert.AreEqual(0, result.Int32);
                Assert.AreEqual(null, result.Prop1);
                Assert.AreEqual(null, result.Prop2);

                Assert.AreEqual(result, output);
            }
        }

        [TestMethod]
        public void Execute_InsertFails_ThrowsDbException()
        {
            using (ShimsContext.Create())
            {
                var type = typeof(TestEntity);
                var expectedId = Helpers.CreateSingletonId("test", type.FullName);

                var bucket = new TestBucket();
                Fakes.ShimBucketProvider.GetBucket = () => bucket;

                bucket.InsertBefore = (s, o) => false;

                try
                {
                    var command = new GetSingletonCommand(new TestValueGenerator());
                    var output = command.Execute("test", type).Result;
                    Assert.Fail();
                }
                catch (AggregateException exception)
                {
                    Assert.AreEqual(1, exception.InnerExceptions.Count);
                    var firstException = exception.InnerExceptions.First();
                    Assert.IsInstanceOfType(firstException, typeof(DbException));
                }

                Assert.IsFalse(bucket.Items.ContainsKey(expectedId));
            }
        }
    }
}
