using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.ExceptionHandling;
using Common.Logging;

namespace OESoftware.Hosted.OData.Api.Middleware
{
    public class ExceptionLogging : ExceptionLogger
    {
        private static readonly ILog Log = LogManager.GetLogger("Request");

        public override bool ShouldLog(ExceptionLoggerContext context)
        {
            return true;
        }

        public override Task LogAsync(ExceptionLoggerContext context, CancellationToken cancellationToken)
        {
            var id = context.Request.GetOwinContext().Get<string>("DbId") ?? "Public";
            Log.ErrorFormat("{0} {1} {2} {3}", context.Exception, id, context.Request.Method, context.Request.GetOwinContext().Request.Path, context.Request.GetOwinContext().Request.RemoteIpAddress);

            return base.LogAsync(context, cancellationToken);
        }
    }
}
