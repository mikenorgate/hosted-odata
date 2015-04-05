using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.OData.Edm;

namespace OESoftware.Hosted.OData.Api.Interfaces
{
    public interface IModelProvider
    {
        Task<IEdmModel> FromRequest(HttpRequestMessage request);
    }
}
