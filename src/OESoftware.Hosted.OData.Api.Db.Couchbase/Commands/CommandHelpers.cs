// Copyright (C) 2015 Michael Norgate

// This software may be modified and distributed under the terms of 
// the Creative Commons Attribution Non-commercial license.  See the LICENSE file for details.

#region usings

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.OData;
using Couchbase;
using Couchbase.Core;
using Fasterflect;
using Microsoft.OData.Edm;
using Newtonsoft.Json.Linq;
using OESoftware.Hosted.OData.Api.Core;

#endregion

namespace OESoftware.Hosted.OData.Api.Db.Couchbase.Commands
{
    /// <summary>
    /// Helpers for commands
    /// </summary>
    public static class CommandHelpers
    {
        /// <summary>
        /// Insert a singleton
        /// </summary>
        /// <param name="bucket"><see cref="IBucket"/></param>
        /// <param name="singletonType">The type of the singleton</param>
        /// <param name="singletonId">ID of the singleton</param>
        /// <param name="tenantId">The id of the tenant</param>
        /// <param name="valueGenerator"><see cref="IValueGenerator"/></param>
        /// <returns><see cref="IDynamicEntity"/></returns>
        public static async Task<IDynamicEntity> InsertSingletonAsync(IBucket bucket, Type singletonType, string singletonId, string tenantId, IValueGenerator valueGenerator, Delta delta = null, bool isPut = false)
        {
            var singleton = (IDynamicEntity)singletonType.CreateInstance();
            await valueGenerator.ComputeValues(tenantId, singleton);

            if (delta != null)
            {
                delta.CallMethod(isPut ? "Put" : "Patch", singleton);
            }

            var insertResult =
                await
                    InsertDocumentAsync(bucket, singletonType, singletonId, singleton);
            if (!insertResult.Success)
            {
                throw ExceptionCreator.CreateDbException(insertResult);
            }

            return singleton;
        }

        /// <summary>
        /// Get a document from the bucket
        /// </summary>
        /// <param name="bucket"><see cref="IBucket"/></param>
        /// <param name="type">The type of the entity</param>
        /// <param name="id">The id of the document</param>
        /// <returns></returns>
        public static async Task<IDocumentResult> GetDocumentAsync(IBucket bucket, Type type, string id, Type castType = null)
        {
            var task = ((Task)bucket.CallMethod(new Type[] { castType ?? type }, "GetDocumentAsync", id));
            await task;

            var find = (IDocumentResult)task.GetPropertyValue("Result");
            return find;
        }

        public static async Task<IOperationResult> ReplaceDocumentAsync(IBucket bucket, Type type, string id,
            object entity, ulong cas)
        {
            var task = ((Task)bucket.CallMethod(new Type[] { type }, "ReplaceAsync", id, entity, cas));
            await task;

            var result = (IOperationResult)task.GetPropertyValue("Result");
            return result;
        }

        public static async Task<IOperationResult> InsertDocumentAsync(IBucket bucket, Type type, string id,
            object entity)
        {
            var task = ((Task)bucket.CallMethod(new Type[] { type }, "InsertAsync", id, entity));
            await task;

            var result = (IOperationResult)task.GetPropertyValue("Result");
            return result;
        }

        public static async Task<IOperationResult> RemoveDocumentAsync(IBucket bucket, string id, ulong cas)
        {
            return await bucket.RemoveAsync(id, cas);
        }

        public static IEnumerable<IDynamicEntity> GetAll(IBucket bucket, IEnumerable<string> ids, Type type, Type castType = null)
        {
            var task = bucket.CallMethod(new Type[] { castType ?? type }, "Get", ids);

            dynamic all = task.GetPropertyValue("Values");

            var list = new List<IDynamicEntity>();

            foreach (IOperationResult operationResult in all)
            {
                if (operationResult.Success)
                {
                    list.Add(operationResult.GetPropertyValue("Value") as IDynamicEntity);
                }
            }

            return list;
        }

        public static object ReflectionGetDocument(IDocumentResult documentResult)
        {
            return documentResult.GetPropertyValue("Document");
        }

        public static T ReflectionGetContent<T>(IDocumentResult documentResult)
        {
            return (T)documentResult.GetPropertyValue("Content");
        }

        public static ulong ReflectionGetCas(IDocumentResult documentResult)
        {
            var document = ReflectionGetDocument(documentResult);
            return (ulong)document.GetPropertyValue("Cas");
        }
    }
}