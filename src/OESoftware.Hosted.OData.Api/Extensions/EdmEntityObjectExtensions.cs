using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Library;
using OESoftware.Hosted.OData.Api.DBHelpers;

namespace OESoftware.Hosted.OData.Api.Extensions
{
    public static class EdmEntityObjectExtensions
    {
        public static async Task<bool> SetComputedKeys(this EdmEntityObject obj, IEdmModel model, HttpRequestMessage request)
        {
            var type = (IEdmEntityType)model.FindDeclaredType(obj.ActualEdmType.FullTypeName());
            var computedKeys =
                type.DeclaredKey.Where(
                    k =>
                        k.VocabularyAnnotations(model)
                            .Any(
                                v =>
                                    v.Term.FullName() ==
                                    Microsoft.OData.Edm.Vocabularies.V1.CoreVocabularyConstants.Computed));

            var gen = new KeyGenerator();

            foreach (var key in computedKeys)
            {
                object value = null;
                var keyName = string.Format("{0}.{1}", type.FullName(), key.Name);
                switch (key.Type.Definition.FullTypeName())
                {
                    case EdmConstants.EdmInt16TypeName:
                        {
                            value = await gen.CreateInt16Key(request, keyName);
                            break;
                        }
                    case EdmConstants.EdmInt32TypeName:
                        {
                            value = await gen.CreateInt32Key(request, keyName);
                            break;
                        }
                    case EdmConstants.EdmInt64TypeName:
                        {
                            value = await gen.CreateInt64Key(request, keyName);
                            break;
                        }
                    case EdmConstants.EdmDecimalTypeName:
                        {
                            value = await gen.CreateDecimalKey(request, keyName);
                            break;
                        }
                    case EdmConstants.EdmDoubleTypeName:
                        {
                            value = await gen.CreateDoubleKey(request, keyName);
                            break;
                        }
                    case EdmConstants.EdmGuidTypeName:
                        {
                            value = await gen.CreateGuidKey(request, keyName);
                            break;
                        }
                    case EdmConstants.EdmSingleTypeName:
                        {
                            value = await gen.CreateSingleKey(request, keyName);
                            break;
                        }
                    default:
                        {
                            throw new ApplicationException(string.Format("Unable to compute value of type {0}", key.Type.Definition.FullTypeName()));
                        }
                }

                if (!obj.TrySetPropertyValue(key.Name, value))
                {
                    throw new ApplicationException(string.Format("Failed to set computed key {0} for {1}", value, key.Name));
                }
            }

            return true;
        }
    }
}