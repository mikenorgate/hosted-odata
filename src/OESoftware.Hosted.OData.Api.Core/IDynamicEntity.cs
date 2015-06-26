using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OESoftware.Hosted.OData.Api.Core
{
    public interface IDynamicEntity
    {
        object GetProperty(string propertyName);

        IDictionary<string, object> GetKeys();

        IEnumerable<PropertyInfo> GetComputedProperties();

        void SetProperty(string propertyName, object value);
    }
}
