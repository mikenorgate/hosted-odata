using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.OData;
using System.Web.OData.Extensions;
using System.Xml;
using Microsoft.OData.Edm.Csdl;
using Microsoft.OData.Edm.Library;
using Microsoft.OData.Edm.Validation;
using MongoDB.Driver;
using OESoftware.Hosted.OData.Api.Attributes;
using OESoftware.Hosted.OData.Api.Models;

namespace OESoftware.Hosted.OData.Api.Controllers
{
    [AnyKeyAuthorize]
    public class MetadataModifyController : MetadataController
    {
        [PrivateKeyAuthorize]
        public async Task<IHttpActionResult> Post()
        {
            var modelProvider = new ModelProvider();
            IEnumerable<EdmError> errors;
            var model = modelProvider.FromXml(await Request.Content.ReadAsStringAsync(), out errors);

            var edmErrors = errors as IList<EdmError> ?? errors.ToList();
            if (edmErrors.Any())
            {
                edmErrors.ToList().ForEach(e => ModelState.AddModelError(e.ErrorLocation.ToString(), e.ErrorMessage));
                return BadRequest(ModelState);
            }

            await modelProvider.SaveModel(model, Request);

            model = await modelProvider.FromRequest(Request);

            var odataProperties = Request.ODataProperties();
            odataProperties.Model = model;
            return Ok(GetMetadata());
        }
    }
}