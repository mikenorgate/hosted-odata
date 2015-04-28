using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.ExceptionHandling;
using Common.Logging;
using Couchbase;
using Microsoft.Owin;

namespace OESoftware.Hosted.OData.Api.Middleware
{
    public class RequestLogging : OwinMiddleware
    {
        private static readonly ILog Log = LogManager.GetLogger("Request");

        public RequestLogging(OwinMiddleware next)
			: base(next)
		{
            
        }

        public override async Task Invoke(IOwinContext owinContext)
        {
                var stopwatch = Stopwatch.StartNew();
                await Next.Invoke(owinContext).ContinueWith((task) =>
                {
                    stopwatch.Stop();
                    var id = owinContext.Get<string>("DbId") ?? "Public";
                    Log.InfoFormat("{0} {1} {2} {3} {4} {5}", id, owinContext.Request.Method, owinContext.Request.Path, owinContext.Request.RemoteIpAddress, owinContext.Response.StatusCode, stopwatch.ElapsedMilliseconds);
                });
        }
    }
}
