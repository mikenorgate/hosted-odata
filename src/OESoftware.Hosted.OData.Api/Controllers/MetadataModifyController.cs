using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.OData;
using System.Web.OData.Extensions;
using System.Xml;
using Microsoft.OData.Edm.Validation;
using MongoDB.Driver;
using OESoftware.Hosted.OData.Api.Attributes;
using OESoftware.Hosted.OData.Api.DBHelpers;
using OESoftware.Hosted.OData.Api.Models;

namespace OESoftware.Hosted.OData.Api.Controllers
{
    [AnyKeyAuthorize]
    public class MetadataModifyController : MetadataController
    {
        [PrivateKeyAuthorize]
        public async Task<IHttpActionResult> Post()
        {
            return await UpdateSchema(true);
        }

        [PrivateKeyAuthorize]
        public async Task<IHttpActionResult> Put()
        {
            return await UpdateSchema(false);
        }

        [PrivateKeyAuthorize]
        public async Task<IHttpActionResult> Delete()
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

            await modelProvider.DeleteModel(model, Request);

            model = await modelProvider.FromRequest(Request);

            var odataProperties = Request.ODataProperties();
            odataProperties.Model = model;
            return Ok(GetMetadata());
        }

        private async Task<IHttpActionResult> UpdateSchema(bool replace)
        {
            try
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

                var updates = model.ToDbUpdates();

                edmErrors = updates.ModelErrors as IList<EdmError> ?? updates.ModelErrors.ToList();
                if (edmErrors.Any())
                {
                    edmErrors.ToList().ForEach(e => ModelState.AddModelError(e.ErrorLocation.ToString(), e.ErrorMessage));
                    return BadRequest(ModelState);
                }

                await modelProvider.SaveModel(model, Request, replace);

                model = await modelProvider.FromRequest(Request);

                var odataProperties = Request.ODataProperties();
                odataProperties.Model = model;
                return Ok(GetMetadata());
            }
            catch (XmlException e)
            {
                return BadRequest(e.Message);
            }
        }


    }
}