using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace OESoftware.Hosted.OData.Api.Tests
{
    public static class SampleGenerator
    {
        public static JObject CreateItem()
        {
            return new JObject()
            {
                { "ItemName", "Name" }
            };
        }
    }
}
