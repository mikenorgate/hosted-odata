using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.OData.Edm;

namespace OESoftware.Hosted.OData.Api.DBHelpers
{
    public interface IKeyGenerator
    {
        Task<object> CreateKey(string dbIdentifier, string keyName, IEdmType type);
    }
}
