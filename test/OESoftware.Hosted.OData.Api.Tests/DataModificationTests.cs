using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Owin.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using RestSharp;

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

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            string baseUrl = "http://*:5000";
            _webApp = WebApp.Start<Startup>(baseUrl);
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            _webApp.Dispose();
        }

        // The region contains tests related to 11.4.2 Create an Entity
        #region Create Entity

        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        public void CreateEntityInCollection()
        {
            using (var client = new HttpClient())
            {
                // New code:
                client.BaseAddress = new Uri("http://localhost:5000/5520f235c49d580c6c6c62f8/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var sampleItem = SampleGenerator.CreateItem();

                var req = new HttpRequestMessage(HttpMethod.Post, "Items")
                {
                    Content = new StringContent(sampleItem.ToString(), Encoding.UTF8, "application/json")
                };
                var response = client.SendAsync(req).Result;
                
                Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);

                var locationHeader =
                    response.Headers.FirstOrDefault(h => h.Key.Equals(HttpResponseHeader.Location.ToString()));
                Assert.IsNotNull(locationHeader);

                var r = new Regex(string.Format(@"{0}Items\(\d+\)", client.BaseAddress), RegexOptions.IgnoreCase);
                Assert.IsTrue(r.IsMatch(locationHeader.Value.First()));
            }
        }

        [TestMethod]
        public void CreateEntityInCollection_FailsIfExtraPropertiesOnNonOpenType()
        {
            using (var client = new HttpClient())
            {
                // New code:
                client.BaseAddress = new Uri("http://localhost:5000/5520f235c49d580c6c6c62f8/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var sampleItem = SampleGenerator.CreateItem();
                sampleItem.Add("NewProperty", new JValue("test"));

                var req = new HttpRequestMessage(HttpMethod.Post, "Items")
                {
                    Content = new StringContent(sampleItem.ToString(), Encoding.UTF8, "application/json")
                };
                var response = client.SendAsync(req).Result;

                Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
            }
        }

        [TestMethod]
        public void CreateEntityInCollection_OpenType()
        {
            using (var client = new HttpClient())
            {
                // New code:
                client.BaseAddress = new Uri("http://localhost:5000/5520f235c49d580c6c6c62f8/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var sampleItem = SampleGenerator.CreateItem();
                sampleItem.Add("NewProperty", new JValue("test"));

                var req = new HttpRequestMessage(HttpMethod.Post, "OpenItems")
                {
                    Content = new StringContent(sampleItem.ToString(), Encoding.UTF8, "application/json")
                };
                var response = client.SendAsync(req).Result;

                Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);

                var locationHeader =
                    response.Headers.FirstOrDefault(h => h.Key.Equals(HttpResponseHeader.Location.ToString()));
                Assert.IsNotNull(locationHeader);

                var r = new Regex(string.Format(@"{0}OpenItems\(\d+\)", client.BaseAddress), RegexOptions.IgnoreCase);
                Assert.IsTrue(r.IsMatch(locationHeader.Value.First()));
            }
        }

        #endregion
    }
}

