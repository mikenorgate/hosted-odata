using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace OESoftware.Hosted.OData.Api.Models.Admin
{
    public class Application
    {
        [BsonRequired]
        [BsonId]
        public string ApplicationName { get; set; }

        [BsonRequired]
        [EmailAddress]
        public string AdminEmailAddress { get; set; }

        [BsonRequired]
        public ObjectId PublicApiKey { get; set; }

        [BsonRequired]
        public ObjectId PrivateApiKey { get; set; }

        [BsonRequired]
        [IgnoreDataMember]
        public ObjectId DbIdentifier { get; set; }
    }
}