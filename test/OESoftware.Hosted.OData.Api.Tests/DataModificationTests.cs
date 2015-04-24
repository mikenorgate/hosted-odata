using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.OData;
using Microsoft.Owin.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using RestSharp;
using Simple.OData.Client;

namespace OESoftware.Hosted.OData.Api.Tests
{
    /// <summary>
    /// These tests relate to the Data Modification (11.4) of the OData specification
    /// </summary>
    /// <see cref="http://docs.oasis-open.org/odata/odata/v4.0/errata02/os/complete/part1-protocol/odata-v4.0-errata02-os-part1-protocol-complete.html#_Toc406398319"/>
    [TestClass]
    public class DataModificationTests
    {
        private static IDisposable _webApp;
        private static TestContext _context;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            string baseUrl = "http://*:5000";
            _webApp = WebApp.Start<Startup>(baseUrl);
            _context = context;
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            _webApp.Dispose();
        }

        [TestMethod]
        public void CreateEntity_EntityIsCreated()
        {
            var client = new ODataClient("http://localhost:5000/5520f235c49d580c6c6c62f8/");
            
            var guid1 = Guid.NewGuid();
            var guid2 = Guid.NewGuid();
            var entity = client
                .For("EntityWithOneKey")
                .Set(new { ItemGuid = guid1, NonComputed = guid2 })
                .InsertEntryAsync().Result;

            Assert.AreEqual(guid1, (Guid)entity["ItemGuid"]);
            Assert.AreEqual(guid2, (Guid)entity["NonComputed"]);
            Assert.IsTrue((int)entity["ItemId"] > 0);
        }

        [TestMethod]
        public void UpdateEntity_EntityIsUpdated()
        {
            var client = new ODataClient("http://localhost:5000/5520f235c49d580c6c6c62f8/");

            var guid1 = Guid.NewGuid();
            var guid2 = Guid.NewGuid();
            var entity = client
                .For("EntityWithOneKey")
                .Set(new { ItemGuid = guid1, NonComputed = guid2 })
                .InsertEntryAsync().Result;

            var guid3 = Guid.NewGuid();
            var updated = client
                .For("EntityWithOneKey")
                .Key((int)entity["ItemId"])
                .Set(new { ItemGuid = guid3 })
                .UpdateEntryAsync().Result;

            Assert.AreEqual(guid3, (Guid)updated["ItemGuid"]);
            Assert.AreEqual(guid2, (Guid)updated["NonComputed"]);
            Assert.AreEqual((int)entity["ItemId"], (int)updated["ItemId"]);
        }

        [TestMethod]
        public void DeleteEntity_EntityIsDeleted()
        {
            var client = new ODataClient("http://localhost:5000/5520f235c49d580c6c6c62f8/");

            var guid1 = Guid.NewGuid();
            var guid2 = Guid.NewGuid();
            var entity = client
                .For("EntityWithOneKey")
                .Set(new { ItemGuid = guid1, NonComputed = guid2 })
                .InsertEntryAsync().Result;
            
            client
                .For("EntityWithOneKey")
                .Key((int)entity["ItemId"])
                .DeleteEntryAsync().Wait();

            try
            {
                client
                    .For("EntityWithOneKey")
                    .Key((int) entity["ItemId"])
                    .FindEntryAsync().Wait();
            }
            catch (AggregateException e)
            {
                var i = e.InnerException as AggregateException;
                Assert.IsNotNull(i);
                var w = i.InnerException as WebRequestException;
                Assert.IsNotNull(w);

                Assert.AreEqual(HttpStatusCode.NotFound, w.Code);
            }
        }
    }
}

