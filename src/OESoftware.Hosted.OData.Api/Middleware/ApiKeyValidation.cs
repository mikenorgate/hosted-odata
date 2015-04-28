using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Owin;
using MongoDB.Bson;
using MongoDB.Driver;
using OESoftware.Hosted.OData.Api.Db.Couchbase;
using OESoftware.Hosted.OData.Api.Models.Admin;

namespace OESoftware.Hosted.OData.Api.Middleware
{
    public class ApiKeyValidation : OwinMiddleware
    {
        public ApiKeyValidation(OwinMiddleware next) : base(next)
        {
        }

        public override async Task Invoke(IOwinContext context)
        {
            var segments = context.Request.Uri.LocalPath.Split('/');
            if (segments.Length > 1)
            {
                var apiKey = segments[1];
                Guid keyAsId;
                if (Guid.TryParse(apiKey, out keyAsId))
                {
                    using (var bucket = BucketProvider.GetBucket("Internal"))
                    {
                        var id = bucket.Get<string>(string.Format("Application:Key:{0}", keyAsId));
                        if (!id.Success)
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                            return;
                        }
                        var application = bucket.Get<Application>(id.Value);
                        if (!application.Success)
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                            return;
                        }

                        context.Set("DbId", application.Value.DbIdentifier.ToString());
                        context.Set("apiKey", keyAsId.ToString());
                        context.Set("apiKey.type", keyAsId == application.Value.PrivateApiKey ? "private" : "public");
                    }
                    
                }
            }

            await Next.Invoke(context);
        }
    }
}