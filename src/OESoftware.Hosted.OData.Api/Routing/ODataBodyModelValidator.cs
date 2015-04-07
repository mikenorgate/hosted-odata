using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Metadata;
using System.Web.Http.Validation;
using System.Web.OData;

namespace OESoftware.Hosted.OData.Api.Routing
{
    public class ODataBodyModelValidator : DefaultBodyModelValidator
    {
        public override bool ShouldValidateType(Type type)
        {
            return type != typeof(EdmEntityObject) && base.ShouldValidateType(type);
        }
    }
}