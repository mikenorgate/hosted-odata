using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Library;
using OESoftware.Hosted.OData.Api.Core;
using OESoftware.Hosted.OData.Api.Db;
using EdmConstants = OESoftware.Hosted.OData.Api.Core.EdmConstants;

namespace OESoftware.Hosted.OData.Api.Tests.Core
{
    public class TestValueGenerator : IValueGenerator
    {
        private IDictionary<Type, Func<string, string, string, Task<object>>> _typeValueGenerators = new Dictionary<Type, Func<string, string, string, Task<object>>>()
        {
            { typeof(Int16), CreateInt16Key },
            { typeof(Int32), CreateInt32Key },
            { typeof(Int64), CreateInt64Key },
            { typeof(Decimal), CreateDecimalKey },
            { typeof(Double), CreateDoubleKey },
            { typeof(Guid), CreateGuidKey },
            { typeof(Single), CreateSingleKey },
        };


        /// <summary>
        /// Computes a value for the given property
        /// </summary>
        /// <param name="tenantId">The id of the tenant</param>
        /// <param name="entity"><see cref="IDynamicEntity"/></param>
        public async Task ComputeValues(string tenantId, IDynamicEntity entity)
        {
            var computedProperties = entity.GetComputedProperties();
            foreach (var computedProperty in computedProperties)
            {
                var value =
                    await
                        ComputeValue(tenantId, computedProperty.Name, computedProperty.PropertyType,
                            entity.GetType().FullName);
                entity.SetProperty(computedProperty.Name, value);
            }
        }

        public Task<object> ComputeValue(string tenantId, string propertyName, IEdmType propertyType, IEdmType entityType)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Computes a value for the given property
        /// </summary>
        /// <param name="tenantId">The id of the tenant</param>
        /// <param name="propertyName">The name of the property</param>
        /// <param name="propertyType">The type of the property</param>
        /// <param name="entityType">The type of the entity</param>
        /// <returns>Generated value</returns>
        public async Task<object> ComputeValue(string tenantId, string propertyName, Type propertyType, string entityType)
        {
            if (!_typeValueGenerators.ContainsKey(propertyType))
            {
                throw new ApplicationException(string.Format("Unable to compute value of type {0}",
                       propertyType.FullName));
            }
            return await _typeValueGenerators[propertyType].Invoke(tenantId, propertyName, entityType);
        }

        private static async Task<object> CreateInt16Key(string tenantId, string keyName, string entityType)
        {
            return Int16.MaxValue;
        }

        private static async Task<object> CreateInt32Key(string tenantId, string keyName, string entityType)
        {
            return Int32.MaxValue;
        }

        private static async Task<object> CreateInt64Key(string tenantId, string keyName, string entityType)
        {
            return Int64.MaxValue;
        }

        private static async Task<object> CreateDecimalKey(string tenantId, string keyName, string entityType)
        {
            return Decimal.MaxValue;
        }

        private static async Task<object> CreateDoubleKey(string tenantId, string keyName, string entityType)
        {
            return Double.MaxValue;
        }

        private static async Task<object> CreateGuidKey(string tenantId, string keyName, string entityType)
        {
            return Guid.Parse("e3f2977c-4bf1-4a22-b560-2155d05abe58");
        }

        private static async Task<object> CreateSingleKey(string tenantId, string keyName, string entityType)
        {
            return Single.MaxValue;
        }
    }
}