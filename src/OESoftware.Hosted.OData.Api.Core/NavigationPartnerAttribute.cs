using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OESoftware.Hosted.OData.Api.Core
{
    public class NavigationPartnerAttribute : Attribute
    {
        public string PartnerPropertyName { get; private set; }

        public NavigationPartnerAttribute(string partnerPropertyName)
        {
            PartnerPropertyName = partnerPropertyName;
        }
    }
}
