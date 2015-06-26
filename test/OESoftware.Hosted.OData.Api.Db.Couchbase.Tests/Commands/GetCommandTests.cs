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
using OESoftware.Hosted.OData.Api.Core;
using OESoftware.Hosted.OData.Api.Db.Couchbase.Commands;
using CouchbaseDb = Couchbase;

namespace OESoftware.Hosted.OData.Api.Db.Couchbase.Tests.Commands
{
    [TestClass]
    public class GetCommandTests
    {
        [TestMethod]
        public void Execute_EntityExists_ReturnsEntity()
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

                bucket.Items.Add(expectedId, document1);
                bucket.Cas.Add(expectedId, 1);

                var command = new GetCommand();
                var entity = (TestEntity)command.Execute("test", keys, type).Result;

                Assert.AreEqual(document1, entity);
            }
        }

        [TestMethod]
        public void Execute_EntityDoesNotExists_ThrowsDbException()
        {
            using (ShimsContext.Create())
            {
                var type = typeof(TestEntity);
                var keys = new Dictionary<string, object> { { "Int32", 1 } };

                CouchbaseDb.Fakes.ShimCluster.ConstructorString = (i, v) => { };

                var bucket = new TestBucket();
                Fakes.ShimBucketProvider.GetBucket = () => bucket;

                try
                {
                    var command = new GetCommand();
                    var entity = command.Execute("test", keys, type).Result;
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
        public void Execute_Cast_ReturnsEntity()
        {
            using (ShimsContext.Create())
            {
                var type = typeof(SubTestEntity);
                var castType = typeof(TestEntity);
                var keys = new Dictionary<string, object> { { "Int32", 1 } };
                var expectedId = Helpers.CreateEntityId("test", keys, type.FullName).Result;
                var document1 = new SubTestEntity() { Int32 = 1, Prop1 = "value1", Prop2 = "value2" };

                CouchbaseDb.Fakes.ShimCluster.ConstructorString = (i, v) => { };

                var bucket = new TestBucket();
                Fakes.ShimBucketProvider.GetBucket = () => bucket;

                bucket.Items.Add(expectedId, document1);
                bucket.Cas.Add(expectedId, 1);

                var command = new GetCommand();
                var entity = command.Execute("test", keys, type, castType).Result;

                Assert.IsInstanceOfType(entity, castType);

                Assert.AreEqual(document1, entity);
            }
        }
    }
}
