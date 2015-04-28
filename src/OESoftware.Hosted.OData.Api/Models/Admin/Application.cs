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
        public string ApplicationName { get; set; }

        [EmailAddress]
        public string AdminEmailAddress { get; set; }

        public Guid PublicApiKey { get; set; }

        public Guid PrivateApiKey { get; set; }

        public Guid DbIdentifier { get; set; }
    }
}