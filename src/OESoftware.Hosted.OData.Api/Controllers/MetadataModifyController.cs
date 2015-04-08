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

        private async Task<IHttpActionResult> UpdateSchema(bool replace)
        {
            try
            {
                var stringReader = new StringReader(await Request.Content.ReadAsStringAsync());
                var xmlReader = XmlReader.Create(stringReader);
                var model = EdmxReader.Parse(xmlReader);

                var xmlBuilder = new StringBuilder();
                IEnumerable<EdmError> errors;
                using (var xmlWriter = XmlWriter.Create(xmlBuilder, new XmlWriterSettings() {Encoding = Encoding.UTF32})
                    )
                {
                    EdmxWriter.TryWriteEdmx(model, xmlWriter, EdmxTarget.OData, out errors);

                    
                }

                var stringReader2 = new StringReader(xmlBuilder.ToString());
                var xmlReader2 = XmlReader.Create(stringReader2);
                model = EdmxReader.Parse(xmlReader2);
                //var modelProvider = new ModelProvider();
                //IEnumerable<EdmError> errors;
                //var model = modelProvider.FromXml(await Request.Content.ReadAsStringAsync(), out errors);

                //var edmErrors = errors as IList<EdmError> ?? errors.ToList();
                //if (edmErrors.Any())
                //{
                //    edmErrors.ToList().ForEach(e => ModelState.AddModelError(e.ErrorLocation.ToString(), e.ErrorMessage));
                //    return BadRequest(ModelState);
                //}

                var updates = model.ToDbUpdates();

                //edmErrors = updates.ModelErrors as IList<EdmError> ?? updates.ModelErrors.ToList();
                //if (edmErrors.Any())
                //{
                //    edmErrors.ToList().ForEach(e => ModelState.AddModelError(e.ErrorLocation.ToString(), e.ErrorMessage));
                //    return BadRequest(ModelState);
                //}

                //await modelProvider.SaveModel(model, Request, replace);

                //model = await modelProvider.FromRequest(Request);

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