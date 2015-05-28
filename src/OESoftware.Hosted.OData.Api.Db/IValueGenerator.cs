using System.Threading.Tasks;
using Microsoft.OData.Edm;

namespace OESoftware.Hosted.OData.Api.Db
{
    public interface IValueGenerator
    {
        Task<object> ComputeValue(string tenantId, string propertyName, IEdmType propertyType, IEdmType entityType);
    }
}
