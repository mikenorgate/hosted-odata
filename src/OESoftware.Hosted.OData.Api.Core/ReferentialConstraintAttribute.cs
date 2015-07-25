using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OESoftware.Hosted.OData.Api.Core
{
    public class ReferentialConstraintAttribute : Attribute
    {
        public string DependantProperty { get; private set; }

        public string PrincipalProperty { get; private set; }

        public ReferentialConstraintAttribute(string dependantProperty, string principalProperty)
        {
            DependantProperty = dependantProperty;
            PrincipalProperty = principalProperty;
        }
    }
}
