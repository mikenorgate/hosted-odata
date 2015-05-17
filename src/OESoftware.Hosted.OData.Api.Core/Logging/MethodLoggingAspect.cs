using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using PostSharp.Aspects;

namespace OESoftware.Hosted.OData.Api.Core.Logging
{
    [MethodLoggingAspect(AttributeExclude = true)]
    [Serializable]
    public class MethodLoggingAspect : OnMethodBoundaryAspect
    {
        [NonSerialized]
        private static readonly ILog Log = LogManager.GetLogger("Method");

        public override void OnEntry(MethodExecutionArgs args)
        {
            args.MethodExecutionTag = DateTime.UtcNow.Ticks;
            base.OnEntry(args);
        }

        public override void OnExit(MethodExecutionArgs args)
        {
            var finishTime = DateTime.UtcNow.Ticks;
            var startTime = (long) args.MethodExecutionTag;
            var executionTime = TimeSpan.FromTicks(finishTime - startTime);
            Log.InfoFormat("{0}.{1} {2} Success", args.Method.DeclaringType.FullName, args.Method.Name, executionTime.TotalMilliseconds);
            base.OnExit(args);
        }

        public override void OnException(MethodExecutionArgs args)
        {
            var finishTime = DateTime.UtcNow.Ticks;
            var startTime = (long)args.MethodExecutionTag;
            var executionTime = TimeSpan.FromTicks(finishTime - startTime);
            Log.ErrorFormat("{0}.{1} {2} Exception{3} {4}", args.Method.DeclaringType.FullName, args.Method.Name, executionTime.TotalMilliseconds, Environment.NewLine, args.Exception);
            base.OnException(args);
        }
    }
}
