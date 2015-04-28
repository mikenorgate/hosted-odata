using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using OESoftware.Hosted.OData.Api.Db.Couchbase;
using OESoftware.Hosted.OData.Api.Models.Admin;

namespace OESoftware.Hosted.OData.Api.Controllers
{
    [RoutePrefix("admin")]
    public class AdminController : ApiController
    {
        [Route("register")]
        [HttpPost]
        public async Task<IHttpActionResult> RegisterApplication(ApplicationRegistrationModel model)
        {
            if (model == null || !ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            using (var bucket = BucketProvider.GetBucket("Internal"))
            {
                var id = string.Format("Application:{0}", model.ApplicationName);
                var app = new Application()
                {
                    AdminEmailAddress = model.AdminEmailAddress,
                    ApplicationName = model.ApplicationName,
                    PrivateApiKey = Guid.NewGuid(),
                    PublicApiKey = Guid.NewGuid(),
                    DbIdentifier = Guid.NewGuid()
                };
                var insertResult = bucket.Insert(id, app);
                if (!insertResult.Success)
                {
                    return BadRequest("An application with this name already exists");
                }
                bucket.Insert(string.Format("Application:Key:{0}", app.PrivateApiKey), id);
                bucket.Insert(string.Format("Application:Key:{0}", app.PublicApiKey), id);
                bucket.Insert(string.Format("Application:Key:{0}", app.DbIdentifier), id);

                return Ok(app);
            }
        }
    }
}