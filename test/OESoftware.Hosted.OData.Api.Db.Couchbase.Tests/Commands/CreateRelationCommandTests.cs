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
using Microsoft.OData.Edm.Validation;
using Microsoft.QualityTools.Testing.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using OESoftware.Hosted.OData.Api.Db.Couchbase.Commands;
using CouchbaseDb = Couchbase;

namespace OESoftware.Hosted.OData.Api.Db.Couchbase.Tests.Commands
{

    [TestClass]
    public class CreateRelationCommandTests
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
        public void Execute_CreatesRelation_ReplacesDbEntry()
        {
            using (ShimsContext.Create())
            {
                var replaceCalled = false;
                var type = _model.FindDeclaredType("Test.SimpleWithNavigation") as IEdmEntityType;
                var keys = new Dictionary<string, object> { { "Int32", 1 } };
                var keys2 = new Dictionary<string, object> { { "Int32", 2 } };
                var property = type.DeclaredNavigationProperties().First(p => p.Name == "Collection");

                CouchbaseDb.Fakes.ShimCluster.ConstructorString = (i, v) => { };
                var bucket = new CouchbaseDb.Core.Fakes.StubIBucket();
                Fakes.ShimBucketProvider.GetBucket = () => bucket;

                var command = new CreateRelationCommand(keys, keys2, property, _model);
                command.Execute("test").Wait();

                
            }
        }
    }
}
