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
                var keyName = string.Format("{0}.{1}", type.FullName(), key.Name);
                var value = gen.CreateKey(request, keyName, key.Type.Definition);

                if (!obj.TrySetPropertyValue(key.Name, value))
                {
                    throw new ApplicationException(string.Format("Failed to set computed key {0} for {1}", value, key.Name));
                }
            }

            return true;
        }
        
    }
}