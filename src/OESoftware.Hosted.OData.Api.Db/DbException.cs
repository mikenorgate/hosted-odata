using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OESoftware.Hosted.OData.Api.Db
{
    public class DbException : Exception
    {
        public DbError Error { get; private set; }

        public DbException(string message) : base(message)
        {
        }

        public DbException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public DbException(DbError error) : base()
        {
            Error = error;
        }
    }
}
