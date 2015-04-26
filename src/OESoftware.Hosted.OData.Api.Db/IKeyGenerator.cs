using System.Threading.Tasks;
using Microsoft.OData.Edm;

namespace OESoftware.Hosted.OData.Api.Db
{
    public interface IKeyGenerator
    {
        Task<object> CreateKey(string tenantId, string keyName, IEdmType type);
    }
}
