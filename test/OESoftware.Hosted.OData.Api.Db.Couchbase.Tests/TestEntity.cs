using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using OESoftware.Hosted.OData.Api.Core;

namespace OESoftware.Hosted.OData.Api.Db.Couchbase.Tests
{
    public class TestEntity : IDynamicEntity
    {
        public int Int32 { get; set; }
        public string Prop1 { get; set; }
        public string Prop2 { get; set; }

        public object GetProperty(string propertyName)
        {
            switch (propertyName)
            {
                case "Int32":
                    return Int32;
                case "Prop1":
                    return Prop1;
                case "Prop2":
                    return Prop2;
            }
            return null;
        }

        public IDictionary<string, object> GetKeys()
        {
            return new Dictionary<string, object>()
            {
                { "Int32", Int32 }
            };
        }

        public IEnumerable<PropertyInfo> GetComputedProperties()
        {
            return new List<PropertyInfo>();
        }

        public void SetProperty(string propertyName, object value)
        {
            switch (propertyName)
            {
                case "Int32":
                    Int32 = (int)value;
                    break;
                case "Prop1":
                    Prop1 = (string)value;
                    break;
                case "Prop2":
                    Prop2 = (string)value;
                    break;
            }
        }
    }
}
