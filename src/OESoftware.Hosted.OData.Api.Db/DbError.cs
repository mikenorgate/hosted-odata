using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OESoftware.Hosted.OData.Api.Db
{
    public enum DbError
    {
        Unknown,
        KeyNotFound,
        KeyExists,
        InvalidData,
        AuthenticationError,
        Timeout,
        InternalError
    }
}
