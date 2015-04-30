using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OESoftware.Hosted.OData.Api.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ODataPathAttribute : Attribute
    {
        public string PathTemplate { get; private set; }

        public ODataPathAttribute(string pathTemplate)
        {
            PathTemplate = pathTemplate;
        }
    }
}
