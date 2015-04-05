using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace OESoftware.Hosted.OData.Api.Models
{
    public class SchemaElement
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonRequired]
        public string Name { get; set; }

        [BsonRequired]
        public string Namespace { get; set; }

        [BsonRequired]
        public string Csdl { get; set; }
    }
}