using System;
using System.Threading.Tasks;
using Microsoft.OData.Edm;
using OESoftware.Hosted.OData.Api.Core;

namespace OESoftware.Hosted.OData.Api.Db
{
    public interface IValueGenerator
    {
        Task<object> ComputeValue(string tenantId, string propertyName, IEdmType propertyType, IEdmType entityType);

        Task<object> ComputeValue(string tenantId, string propertyName, Type propertyType, string entityType);

        Task ComputeValues(string tenantId, IDynamicEntity entity);
    }
}
