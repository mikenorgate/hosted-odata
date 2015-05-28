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
    public class GetCommandTests
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
        public void Execute_Entityxists_ReturnsEntity()
        {
            using (ShimsContext.Create())
            {
                var type = _model.FindDeclaredType("Test.SimpleWithKey") as IEdmEntityType;
                var keys = new Dictionary<string, object> { { "Int32", 1 } };
                var expectedId = Helpers.CreateEntityId("test", keys, type).Result;
                var document1 = new JObject() { { "Int32", 1 }, { "Prop1", "value1" }, { "Prop2", "value2" } };

                CouchbaseDb.Fakes.ShimCluster.ConstructorString = (i, v) => { };

                var bucket = new CouchbaseDb.Core.Fakes.StubIBucket();
                Fakes.ShimBucketProvider.GetBucket = () => bucket;
                bucket.GetAsyncOf1String((id) =>
                {
                    return Task<CouchbaseDb.IOperationResult<JObject>>.Factory.StartNew(() =>
                    {
                        Assert.AreEqual(expectedId, id);

                        var result = new CouchbaseDb.Fakes.StubIOperationResult<JObject>();
                        result.SuccessGet = () => true;
                        result.ValueGet = () => document1;
                        return result;
                    });
                });

                var command = new GetCommand(keys, type);
                var entity = command.Execute("test").Result;

                
                object value;
                Assert.IsTrue(entity.TryGetPropertyValue("Int32", out value));
                Assert.AreEqual(1, value);
            }
        }

        [TestMethod]
        public void Execute_CollectionDoesNotExists_ThrowsDbException()
        {
            using (ShimsContext.Create())
            {
                var type = _model.FindDeclaredType("Test.SimpleWithKey") as IEdmEntityType;
                var keys = new Dictionary<string, object> { { "Int32", 1 } };
                var expectedId = Helpers.CreateEntityId("test", keys, type).Result;

                CouchbaseDb.Fakes.ShimCluster.ConstructorString = (i, v) => { };

                var bucket = new CouchbaseDb.Core.Fakes.StubIBucket();
                Fakes.ShimBucketProvider.GetBucket = () => bucket;
                bucket.GetAsyncOf1String((id) =>
                {
                    return Task<CouchbaseDb.IOperationResult<JObject>>.Factory.StartNew(() =>
                    {
                        Assert.AreEqual(expectedId, id);

                        var result = new CouchbaseDb.Fakes.StubIOperationResult<JObject>();
                        result.SuccessGet = () => false;
                        return result;
                    });
                });

                try
                {
                    var command = new GetCommand(keys, type);
                    var collection = command.Execute("test").Result;
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
