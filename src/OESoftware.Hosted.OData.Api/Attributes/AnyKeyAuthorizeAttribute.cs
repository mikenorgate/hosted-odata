using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace OESoftware.Hosted.OData.Api.Attributes
{
    public class AnyKeyAuthorizeAttribute : AuthorizeAttribute
    {
        protected override bool IsAuthorized(HttpActionContext actionContext)
        {
            var owinVariables = actionContext.Request.GetOwinEnvironment();
            return owinVariables.ContainsKey("apiKey.type") && (owinVariables["apiKey.type"] as string) != null;
        }
    }
}