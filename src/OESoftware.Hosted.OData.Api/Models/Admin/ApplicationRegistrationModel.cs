using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace OESoftware.Hosted.OData.Api.Models.Admin
{
    public class ApplicationRegistrationModel
    {
        [Required]
        public string ApplicationName { get; set; }

        [Required]
        [EmailAddress]
        public string AdminEmailAddress { get; set; }
    }
}