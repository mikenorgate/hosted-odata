using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Fasterflect;
using OESoftware.Hosted.OData.Api.Core;

namespace OESoftware.Hosted.OData.Api.DynamicAssembyGeneration
{
    public class DynamicEntityBase : IDynamicEntity
    {
        public object GetProperty(string propertyName)
        {
            return this.GetPropertyValue(propertyName);
        }

        public IEnumerable<PropertyInfo> GetComputedProperties()
        {
            return this.GetType().PropertiesWith(Flags.InstancePublic, typeof(ComputedAttribute));
        }

        public void SetProperty(string propertyName, object value)
        {
            this.SetPropertyValue(propertyName, value);
        }

        public IDictionary<string, object> GetKeys()
        {
            return this.GetType().PropertiesWith(Flags.InstancePublic, typeof (KeyAttribute)).ToDictionary(k => k.Name, v => v.GetValue(this));
        }
    }
}
