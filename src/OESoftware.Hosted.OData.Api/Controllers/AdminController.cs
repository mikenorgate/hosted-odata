using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using OESoftware.Hosted.OData.Api.DBHelpers;
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

            var dbConnection = DBConnectionFactory.Open("management");

            var collection = dbConnection.GetCollection<Application>("Applications");

            var query = Query<Application>.Matches(application => application.ApplicationName, new BsonRegularExpression(model.ApplicationName, "i"));
            var existing = await collection.FindAsync(new BsonDocumentFilterDefinition<Application>(query.ToBsonDocument()), new FindOptions<Application>() { Limit = 1 });
            var found = await existing.ToListAsync();

            if (found.Any())
            {
                return BadRequest("An application with this name already exists");
            }

            var app = new Application()
            {
                AdminEmailAddress = model.AdminEmailAddress,
                ApplicationName = model.ApplicationName,
                PrivateApiKey = ObjectId.GenerateNewId(),
                PublicApiKey = ObjectId.GenerateNewId(),
                DbIdentifier = ObjectId.GenerateNewId()
            };

            await collection.InsertOneAsync(app);

            return Ok(app);
        }
    }
}