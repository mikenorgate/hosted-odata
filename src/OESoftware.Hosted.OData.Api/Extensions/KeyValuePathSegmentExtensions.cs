using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.OData.Routing;
using Microsoft.OData.Edm;
using OESoftware.Hosted.OData.Api.DBHelpers;

namespace OESoftware.Hosted.OData.Api.Extensions
{
    public static class KeyValuePathSegmentExtensions
    {
        public static IDictionary<string, object> ParseKeyValue(this KeyValuePathSegment keyValuePathSegment, IEdmEntityTypeReference entityType)
        {
            var result = new Dictionary<string, object>();
            if (string.IsNullOrEmpty(keyValuePathSegment.Value)) return result;

            var keyConverter = new KeyParser();

            IEnumerable<string> compoundKeyPairs = keyValuePathSegment.Value.Split(',');
            if (!compoundKeyPairs.Any())
            {
                return result;
            }

            foreach (var compoundKeyPair in compoundKeyPairs)
            {
                var pair = compoundKeyPair.Split('=');
                if (pair.Length != 2)
                {
                    if (entityType.Key().Count() == 1)
                    {
                        result.Add(entityType.Key().First().Name, keyConverter.FromString(keyValuePathSegment.Value, entityType.Key().First().Type.Definition));
                        break;
                    }
                    continue;
                }
                var keyName = pair[0].Trim();
                var keyValue = pair[1].Trim();

                var key =
                    entityType.Key()
                        .FirstOrDefault(f => f.Name.Equals(keyName, StringComparison.InvariantCultureIgnoreCase));

                if(key == null) continue;

                result.Add(key.Name, keyConverter.FromString(keyValue, key.Type.Definition));
            }

            return result;
        } 
    }
}