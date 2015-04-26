using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Couchbase;
using Couchbase.IO;

namespace OESoftware.Hosted.OData.Api.Db.Couchbase
{
    public static class ExceptionCreator
    {
        public static DbException CreateDbException(IOperationResult result)
        {
            return CreateException(result.Exception, result.Status);
        }

        public static DbException CreateDbException(IDocumentResult result)
        {
            return CreateException(result.Exception, result.Status);
        }

        private static DbException CreateException(Exception exception, ResponseStatus status)
        {
            if (exception != null)
            {
                return new DbException(exception.Message, exception);
            }

            switch (status)
            {
                case ResponseStatus.KeyNotFound:
                    return new DbException(DbError.KeyNotFound);
                case ResponseStatus.KeyExists:
                    return new DbException(DbError.KeyExists);
                case ResponseStatus.ValueTooLarge:
                case ResponseStatus.InvalidArguments:
                case ResponseStatus.IncrDecrOnNonNumericValue:
                case ResponseStatus.InvalidRange:
                    return new DbException(DbError.InvalidData);
                case ResponseStatus.AuthenticationError:
                case ResponseStatus.AuthenticationContinue:
                    return new DbException(DbError.AuthenticationError);
                case ResponseStatus.OperationTimeout:
                    return new DbException(DbError.Timeout);
                default:
                    return new DbException(DbError.InternalError);
            }
        }
    }
}
