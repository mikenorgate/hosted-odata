using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MongoDB.Bson.Serialization.Attributes;

namespace OESoftware.Hosted.OData.Api.Models
{
    public class IdCounter
    {
        [BsonId]
        public string Name { get; set; }

        public object Counter { get; set; }
    }
}