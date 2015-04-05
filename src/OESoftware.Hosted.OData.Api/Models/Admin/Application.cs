using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace OESoftware.Hosted.OData.Api.Models.Admin
{
    public class Application
    {
        [Required]
        [BsonId]
        public string ApplicationName { get; set; }

        [Required]
        [EmailAddress]
        public string AdminEmailAddress { get; set; }

        public ObjectId PublicApiKey { get; set; }

        public ObjectId PrivateApiKey { get; set; }
    }
}