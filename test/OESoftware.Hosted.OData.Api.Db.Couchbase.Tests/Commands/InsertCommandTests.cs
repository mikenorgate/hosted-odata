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
using OESoftware.Hosted.OData.Api.Tests.Core;
using CouchbaseDb = Couchbase;

namespace OESoftware.Hosted.OData.Api.Db.Couchbase.Tests.Commands
{

    [TestClass]
    public class InsertCommandTests
    {
        [TestMethod]
        public void Execute_InsertsEntity_ReturnEntity()
        {
            using (ShimsContext.Create())
            {
                var type = typeof(TestEntity);
                var keys = new Dictionary<string, object> { { "Int32", 1 } };
                var expectedId = Helpers.CreateEntityId("test", keys, type.FullName).Result;
                var document1 = new TestEntity() { Int32 = 1, Prop1 = "value1", Prop2 = "value2" };

                CouchbaseDb.Fakes.ShimCluster.ConstructorString = (i, v) => { };

                var bucket = new TestBucket();
                Fakes.ShimBucketProvider.GetBucket = () => bucket;

                var command = new InsertCommand(new TestValueGenerator());
                var output = (TestEntity)command.Execute("test", document1).Result;

                var result = (TestEntity)bucket.Items[expectedId];
                Assert.AreEqual(1, result.Int32);
                Assert.AreEqual("value1", result.Prop1);
                Assert.AreEqual("value2", result.Prop2);

                Assert.AreEqual(result, output);
            }
        }

        [TestMethod]
        public void Execute_InsertsFails_ThrowsDbException()
        {
            using (ShimsContext.Create())
            {
                var type = typeof(TestEntity);
                var keys = new Dictionary<string, object> { { "Int32", 1 } };
                var expectedId = Helpers.CreateEntityId("test", keys, type.FullName).Result;
                var document1 = new TestEntity() { Int32 = 1, Prop1 = "value1", Prop2 = "value2" };

                CouchbaseDb.Fakes.ShimCluster.ConstructorString = (i, v) => { };

                var bucket = new TestBucket();
                Fakes.ShimBucketProvider.GetBucket = () => bucket;

                bucket.InsertBefore = (s, o) => false;

                try
                {
                    var command = new InsertCommand(new TestValueGenerator());
                    var output = command.Execute("test", document1).Result;
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
