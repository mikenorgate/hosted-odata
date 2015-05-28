// Copyright (C) 2015 Michael Norgate

// This software may be modified and distributed under the terms of 
// the Creative Commons Attribution Non-commercial license.  See the LICENSE file for details.

#region usings

using System;
using Couchbase;
using Couchbase.IO;

#endregion

namespace OESoftware.Hosted.OData.Api.Db.Couchbase
{
    /// <summary>
    /// Create exception
    /// </summary>
    public static class ExceptionCreator
    {
        /// <summary>
        /// Create DbException from an operation result
        /// </summary>
        /// <param name="result"><see cref="IOperationResult"/></param>
        /// <returns><see cref="DbException"/></returns>
        public static DbException CreateDbException(IOperationResult result)
        {
            return CreateException(result.Exception, result.Status);
        }

        /// <summary>
        /// Create DbException from an document result
        /// </summary>
        /// <param name="result"><see cref="IDocumentResult"/></param>
        /// <returns><see cref="DbException"/></returns>
        public static DbException CreateDbException(IDocumentResult result)
        {
            return CreateException(result.Exception, result.Status);
        }

        /// <summary>
        /// Create DbException from an exception and status
        /// </summary>
        /// <param name="exception"><see cref="Exception"/></param>
        /// <param name="status"><see cref="ResponseStatus"/></param>
        /// <returns><see cref="DbException"/></returns>
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