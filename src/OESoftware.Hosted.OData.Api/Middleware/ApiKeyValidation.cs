using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Owin;
using MongoDB.Bson;
using MongoDB.Driver;
using OESoftware.Hosted.OData.Api.DBHelpers;
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
                ObjectId keyAsId;
                if (ObjectId.TryParse(apiKey, out keyAsId))
                {
                    //TODO: Move these strings somewhere
                    var dbConnection = DBConnectionFactory.Open("management");

                    var collection = dbConnection.GetCollection<Application>("Applications");

                    var findResults =
                        await (await collection.FindAsync(
                            new ExpressionFilterDefinition<Application>(
                                a => a.PrivateApiKey == keyAsId || a.PublicApiKey == keyAsId),
                            new FindOptions<Application>() {Limit = 1})).ToListAsync();

                    var application = findResults.FirstOrDefault();

                    if (application == null)
                    {
                        context.Response.StatusCode = (int) HttpStatusCode.NotFound;
                        return;
                    }

                    context.Set("DbId", application.DbIdentifier.ToString());
                    context.Set("apiKey", keyAsId.ToString());
                    context.Set("apiKey.type", keyAsId == application.PrivateApiKey ? "private" : "public");
                }
            }

            await Next.Invoke(context);
        }
    }
}