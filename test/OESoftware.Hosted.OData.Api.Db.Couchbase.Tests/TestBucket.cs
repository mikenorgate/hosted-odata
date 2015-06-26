using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Couchbase;
using Couchbase.Core;
using Couchbase.Core.Buckets;
using Couchbase.IO;
using Couchbase.IO.Operations;
using Couchbase.Management;
using Couchbase.N1QL;
using Couchbase.Views;
using OESoftware.Hosted.OData.Api.Core;

namespace OESoftware.Hosted.OData.Api.Db.Couchbase.Tests
{

    public class TestOperationResult<T> : IOperationResult<T>
    {
        public bool ShouldRetry()
        {
            throw new NotImplementedException();
        }

        public bool Success { get; set; }

        public string Message { get; set; }

        public Exception Exception { get; set; }
        public bool IsNmv()
        {
            throw new NotImplementedException();
        }

        public ulong Cas { get; set; }

        public ResponseStatus Status { get; set; }

        public Durability Durability { get; set; }

        public T Value { get; set; }
    }

    public class TestOperationResult : IOperationResult
    {
        public bool ShouldRetry()
        {
            throw new NotImplementedException();
        }

        public bool Success { get; set; }
        public string Message { get; set; }
        public Exception Exception { get; set; }
        public bool IsNmv()
        {
            throw new NotImplementedException();
        }

        public ulong Cas { get; set; }
        public ResponseStatus Status { get; set; }
        public Durability Durability { get; set; }
    }

    public class TestDocumentResult<T> : IDocumentResult<T>
    {
        public bool ShouldRetry()
        {
            throw new NotImplementedException();
        }

        public bool Success { get; set; }

        public string Message { get; set; }

        public Exception Exception { get; set; }

        public ResponseStatus Status { get; set; }

        public Document<T> Document { get; set; }

        public T Content { get; set; }
    }

    class TestBucket : IBucket
    {
        public IDictionary<string, object> Items { get; set; }
        public IDictionary<string, ulong> Cas { get; set; }

        public Func<string, object, ulong, bool> ReplaceBefore { get; set; } 
        public Func<string, object, bool> InsertBefore { get; set; } 
        public Func<string, ulong, bool> RemoveBefore { get; set; } 

        public TestBucket()
        {
            Items = new Dictionary<string, object>();
            Cas = new Dictionary<string, ulong>();
        }

        public void Dispose()
        {
            Items = null;
            Cas = null;
        }

        public bool Exists(string key)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ExistsAsync(string key)
        {
            throw new NotImplementedException();
        }

        public Task<ObserveResponse> ObserveAsync(string key, ulong cas, bool deletion, ReplicateTo replicateTo, PersistTo persistTo)
        {
            throw new NotImplementedException();
        }

        public ObserveResponse Observe(string key, ulong cas, bool deletion, ReplicateTo replicateTo, PersistTo persistTo)
        {
            throw new NotImplementedException();
        }

        public IDocumentResult<T> Upsert<T>(IDocument<T> document)
        {
            throw new NotImplementedException();
        }

        public Task<IDocumentResult<T>> UpsertAsync<T>(IDocument<T> document)
        {
            throw new NotImplementedException();
        }

        public IDocumentResult<T> Upsert<T>(IDocument<T> document, ReplicateTo replicateTo)
        {
            throw new NotImplementedException();
        }

        public Task<IDocumentResult<T>> UpsertAsync<T>(IDocument<T> document, ReplicateTo replicateTo)
        {
            throw new NotImplementedException();
        }

        public IDocumentResult<T> Upsert<T>(IDocument<T> document, ReplicateTo replicateTo, PersistTo persistTo)
        {
            throw new NotImplementedException();
        }

        public Task<IDocumentResult<T>> UpsertAsync<T>(IDocument<T> document, ReplicateTo replicateTo, PersistTo persistTo)
        {
            throw new NotImplementedException();
        }

        public IOperationResult<T> Upsert<T>(string key, T value)
        {
            throw new NotImplementedException();
        }

        public Task<IOperationResult<T>> UpsertAsync<T>(string key, T value)
        {
            throw new NotImplementedException();
        }

        public IOperationResult<T> Upsert<T>(string key, T value, uint expiration)
        {
            throw new NotImplementedException();
        }

        public Task<IOperationResult<T>> UpsertAsync<T>(string key, T value, uint expiration)
        {
            throw new NotImplementedException();
        }

        public IOperationResult<T> Upsert<T>(string key, T value, TimeSpan expiration)
        {
            throw new NotImplementedException();
        }

        public Task<IOperationResult<T>> UpsertAsync<T>(string key, T value, TimeSpan expiration)
        {
            throw new NotImplementedException();
        }

        public IOperationResult<T> Upsert<T>(string key, T value, ulong cas)
        {
            throw new NotImplementedException();
        }

        public Task<IOperationResult<T>> UpsertAsync<T>(string key, T value, ulong cas)
        {
            throw new NotImplementedException();
        }

        public IOperationResult<T> Upsert<T>(string key, T value, ulong cas, uint expiration)
        {
            throw new NotImplementedException();
        }

        public Task<IOperationResult<T>> UpsertAsync<T>(string key, T value, ulong cas, uint expiration)
        {
            throw new NotImplementedException();
        }

        public IOperationResult<T> Upsert<T>(string key, T value, ulong cas, TimeSpan expiration)
        {
            throw new NotImplementedException();
        }

        public Task<IOperationResult<T>> UpsertAsync<T>(string key, T value, ulong cas, TimeSpan expiration)
        {
            throw new NotImplementedException();
        }

        public IOperationResult<T> Upsert<T>(string key, T value, ReplicateTo replicateTo)
        {
            throw new NotImplementedException();
        }

        public Task<IOperationResult<T>> UpsertAsync<T>(string key, T value, ReplicateTo replicateTo)
        {
            throw new NotImplementedException();
        }

        public IOperationResult<T> Upsert<T>(string key, T value, ReplicateTo replicateTo, PersistTo persistTo)
        {
            throw new NotImplementedException();
        }

        public Task<IOperationResult<T>> UpsertAsync<T>(string key, T value, ReplicateTo replicateTo, PersistTo persistTo)
        {
            throw new NotImplementedException();
        }

        public IOperationResult<T> Upsert<T>(string key, T value, uint expiration, ReplicateTo replicateTo, PersistTo persistTo)
        {
            throw new NotImplementedException();
        }

        public Task<IOperationResult<T>> UpsertAsync<T>(string key, T value, uint expiration, ReplicateTo replicateTo, PersistTo persistTo)
        {
            throw new NotImplementedException();
        }

        public IOperationResult<T> Upsert<T>(string key, T value, ulong cas, uint expiration, ReplicateTo replicateTo, PersistTo persistTo)
        {
            throw new NotImplementedException();
        }

        public Task<IOperationResult<T>> UpsertAsync<T>(string key, T value, ulong cas, uint expiration, ReplicateTo replicateTo, PersistTo persistTo)
        {
            throw new NotImplementedException();
        }

        public IOperationResult<T> Upsert<T>(string key, T value, TimeSpan expiration, ReplicateTo replicateTo, PersistTo persistTo)
        {
            throw new NotImplementedException();
        }

        public Task<IOperationResult<T>> UpsertAsync<T>(string key, T value, TimeSpan expiration, ReplicateTo replicateTo, PersistTo persistTo)
        {
            throw new NotImplementedException();
        }

        public IOperationResult<T> Upsert<T>(string key, T value, ulong cas, TimeSpan expiration, ReplicateTo replicateTo,
            PersistTo persistTo)
        {
            throw new NotImplementedException();
        }

        public Task<IOperationResult<T>> UpsertAsync<T>(string key, T value, ulong cas, TimeSpan expiration, ReplicateTo replicateTo, PersistTo persistTo)
        {
            throw new NotImplementedException();
        }

        public IDictionary<string, IOperationResult<T>> Upsert<T>(IDictionary<string, T> items)
        {
            throw new NotImplementedException();
        }

        public IDictionary<string, IOperationResult<T>> Upsert<T>(IDictionary<string, T> items, ParallelOptions options)
        {
            throw new NotImplementedException();
        }

        public IDictionary<string, IOperationResult<T>> Upsert<T>(IDictionary<string, T> items, ParallelOptions options, int rangeSize)
        {
            throw new NotImplementedException();
        }

        public IDocumentResult<T> Replace<T>(IDocument<T> document)
        {
            throw new NotImplementedException();
        }

        public Task<IDocumentResult<T>> ReplaceAsync<T>(IDocument<T> document)
        {
            throw new NotImplementedException();
        }

        public IDocumentResult<T> Replace<T>(IDocument<T> document, ReplicateTo replicateTo)
        {
            throw new NotImplementedException();
        }

        public Task<IDocumentResult<T>> ReplaceAsync<T>(IDocument<T> document, ReplicateTo replicateTo)
        {
            throw new NotImplementedException();
        }

        public IDocumentResult<T> Replace<T>(IDocument<T> document, ReplicateTo replicateTo, PersistTo persistTo)
        {
            throw new NotImplementedException();
        }

        public Task<IDocumentResult<T>> ReplaceAsync<T>(IDocument<T> document, ReplicateTo replicateTo, PersistTo persistTo)
        {
            throw new NotImplementedException();
        }

        public IOperationResult<T> Replace<T>(string key, T value)
        {
            throw new NotImplementedException();
        }

        public Task<IOperationResult<T>> ReplaceAsync<T>(string key, T value)
        {
            throw new NotImplementedException();
        }

        public IOperationResult<T> Replace<T>(string key, T value, uint expiration)
        {
            throw new NotImplementedException();
        }

        public Task<IOperationResult<T>> ReplaceAsync<T>(string key, T value, uint expiration)
        {
            throw new NotImplementedException();
        }

        public IOperationResult<T> Replace<T>(string key, T value, TimeSpan expiration)
        {
            throw new NotImplementedException();
        }

        public Task<IOperationResult<T>> ReplaceAsync<T>(string key, T value, TimeSpan expiration)
        {
            throw new NotImplementedException();
        }

        public IOperationResult<T> Replace<T>(string key, T value, ulong cas)
        {
            throw new NotImplementedException();
        }

        public async Task<IOperationResult<T>> ReplaceAsync<T>(string key, T value, ulong cas)
        {
            bool success = true;
            if (ReplaceBefore != null)
            {
                success = ReplaceBefore.Invoke(key, value, cas);
            }

            var operationResult = new TestOperationResult<T>();
            if (Items.ContainsKey(key) && Cas[key] == cas && success)
            {
                operationResult.Success = true;
                Items[key] = value;
                Cas[key] = cas + 1;
                operationResult.Cas = Cas[key];
                operationResult.Value = (T)Items[key];
            }
            else
            {
                operationResult.Success = false;
                operationResult.Status = ResponseStatus.KeyNotFound;
            }

            return operationResult;
        }

        public IOperationResult<T> Replace<T>(string key, T value, ulong cas, uint expiration)
        {
            throw new NotImplementedException();
        }

        public Task<IOperationResult<T>> ReplaceAsync<T>(string key, T value, ulong cas, uint expiration)
        {
            throw new NotImplementedException();
        }

        public IOperationResult<T> Replace<T>(string key, T value, ulong cas, TimeSpan expiration)
        {
            throw new NotImplementedException();
        }

        public Task<IOperationResult<T>> ReplaceAsync<T>(string key, T value, ulong cas, TimeSpan expiration)
        {
            throw new NotImplementedException();
        }

        public IOperationResult<T> Replace<T>(string key, T value, ReplicateTo replicateTo)
        {
            throw new NotImplementedException();
        }

        public Task<IOperationResult<T>> ReplaceAsync<T>(string key, T value, ReplicateTo replicateTo)
        {
            throw new NotImplementedException();
        }

        public IOperationResult<T> Replace<T>(string key, T value, ulong cas, ReplicateTo replicateTo)
        {
            throw new NotImplementedException();
        }

        public Task<IOperationResult<T>> ReplaceAsync<T>(string key, T value, ulong cas, ReplicateTo replicateTo)
        {
            throw new NotImplementedException();
        }

        public IOperationResult<T> Replace<T>(string key, T value, ReplicateTo replicateTo, PersistTo persistTo)
        {
            throw new NotImplementedException();
        }

        public Task<IOperationResult<T>> ReplaceAsync<T>(string key, T value, ReplicateTo replicateTo, PersistTo persistTo)
        {
            throw new NotImplementedException();
        }

        public IOperationResult<T> Replace<T>(string key, T value, ulong cas, ReplicateTo replicateTo, PersistTo persistTo)
        {
            throw new NotImplementedException();
        }

        public Task<IOperationResult<T>> ReplaceAsync<T>(string key, T value, ulong cas, ReplicateTo replicateTo, PersistTo persistTo)
        {
            throw new NotImplementedException();
        }

        public IOperationResult<T> Replace<T>(string key, T value, ulong cas, uint expiration, ReplicateTo replicateTo,
            PersistTo persistTo)
        {
            throw new NotImplementedException();
        }

        public Task<IOperationResult<T>> ReplaceAsync<T>(string key, T value, ulong cas, uint expiration, ReplicateTo replicateTo, PersistTo persistTo)
        {
            throw new NotImplementedException();
        }

        public IOperationResult<T> Replace<T>(string key, T value, ulong cas, TimeSpan expiration, ReplicateTo replicateTo,
            PersistTo persistTo)
        {
            throw new NotImplementedException();
        }

        public Task<IOperationResult<T>> ReplaceAsync<T>(string key, T value, ulong cas, TimeSpan expiration, ReplicateTo replicateTo, PersistTo persistTo)
        {
            throw new NotImplementedException();
        }

        public IDocumentResult<T> Insert<T>(IDocument<T> document)
        {
            throw new NotImplementedException();
        }

        public Task<IDocumentResult<T>> InsertAsync<T>(IDocument<T> document)
        {
            throw new NotImplementedException();
        }

        public IDocumentResult<T> Insert<T>(IDocument<T> document, ReplicateTo replicateTo)
        {
            throw new NotImplementedException();
        }

        public Task<IDocumentResult<T>> InsertAsync<T>(IDocument<T> document, ReplicateTo replicateTo)
        {
            throw new NotImplementedException();
        }

        public IDocumentResult<T> Insert<T>(IDocument<T> document, ReplicateTo replicateTo, PersistTo persistTo)
        {
            throw new NotImplementedException();
        }

        public Task<IDocumentResult<T>> InsertAsync<T>(IDocument<T> document, ReplicateTo replicateTo, PersistTo persistTo)
        {
            throw new NotImplementedException();
        }

        public IOperationResult<T> Insert<T>(string key, T value)
        {
            throw new NotImplementedException();
        }

        public async Task<IOperationResult<T>> InsertAsync<T>(string key, T value)
        {
            bool success = true;
            if(InsertBefore != null)
            {
                success = InsertBefore.Invoke(key, value);
            }

            var operationResult = new TestOperationResult<T>();
            if (Items.ContainsKey(key) || !success)
            {
                operationResult.Success = false;
                operationResult.Status = ResponseStatus.KeyExists;
            }
            else
            {
                operationResult.Success = true;
                Items.Add(key, value);
                Cas.Add(key, 1L);
                operationResult.Cas = Cas[key];
                operationResult.Value = (T)Items[key];
            }

            return operationResult;
        }

        public IOperationResult<T> Insert<T>(string key, T value, uint expiration)
        {
            throw new NotImplementedException();
        }

        public Task<IOperationResult<T>> InsertAsync<T>(string key, T value, uint expiration)
        {
            throw new NotImplementedException();
        }

        public IOperationResult<T> Insert<T>(string key, T value, TimeSpan expiration)
        {
            throw new NotImplementedException();
        }

        public Task<IOperationResult<T>> InsertAsync<T>(string key, T value, TimeSpan expiration)
        {
            throw new NotImplementedException();
        }

        public IOperationResult<T> Insert<T>(string key, T value, ReplicateTo replicateTo)
        {
            throw new NotImplementedException();
        }

        public Task<IOperationResult<T>> InsertAsync<T>(string key, T value, ReplicateTo replicateTo)
        {
            throw new NotImplementedException();
        }

        public IOperationResult<T> Insert<T>(string key, T value, ReplicateTo replicateTo, PersistTo persistTo)
        {
            throw new NotImplementedException();
        }

        public Task<IOperationResult<T>> InsertAsync<T>(string key, T value, ReplicateTo replicateTo, PersistTo persistTo)
        {
            throw new NotImplementedException();
        }

        public IOperationResult<T> Insert<T>(string key, T value, uint expiration, ReplicateTo replicateTo, PersistTo persistTo)
        {
            throw new NotImplementedException();
        }

        public Task<IOperationResult<T>> InsertAsync<T>(string key, T value, uint expiration, ReplicateTo replicateTo, PersistTo persistTo)
        {
            throw new NotImplementedException();
        }

        public IOperationResult<T> Insert<T>(string key, T value, TimeSpan expiration, ReplicateTo replicateTo, PersistTo persistTo)
        {
            throw new NotImplementedException();
        }

        public Task<IOperationResult<T>> InsertAsync<T>(string key, T value, TimeSpan expiration, ReplicateTo replicateTo, PersistTo persistTo)
        {
            throw new NotImplementedException();
        }

        public IOperationResult Remove<T>(IDocument<T> document)
        {
            throw new NotImplementedException();
        }

        public Task<IOperationResult> RemoveAsync<T>(IDocument<T> document)
        {
            throw new NotImplementedException();
        }

        public IOperationResult Remove<T>(IDocument<T> document, ReplicateTo replicateTo)
        {
            throw new NotImplementedException();
        }

        public Task<IOperationResult> RemoveAsync<T>(IDocument<T> document, ReplicateTo replicateTo)
        {
            throw new NotImplementedException();
        }

        public IOperationResult Remove<T>(IDocument<T> document, ReplicateTo replicateTo, PersistTo persistTo)
        {
            throw new NotImplementedException();
        }

        public Task<IOperationResult> RemoveAsync<T>(IDocument<T> document, ReplicateTo replicateTo, PersistTo persistTo)
        {
            throw new NotImplementedException();
        }

        public IOperationResult Remove(string key)
        {
            throw new NotImplementedException();
        }

        public async Task<IOperationResult> RemoveAsync(string key)
        {
            bool success = true;
            if (RemoveBefore != null)
            {
                success = RemoveBefore.Invoke(key, 1);
            }

            var operationResult = new TestOperationResult();
            if (Items.ContainsKey(key) && success)
            {
                operationResult.Success = true;
                Items.Remove(key);
                Cas.Remove(key);
            }
            else
            {
                operationResult.Success = false;
                operationResult.Status = ResponseStatus.KeyNotFound;
            }

            return operationResult;
        }

        public IOperationResult Remove(string key, ulong cas)
        {
            throw new NotImplementedException();
        }

        public async Task<IOperationResult> RemoveAsync(string key, ulong cas)
        {
            bool success = true;
            if (RemoveBefore != null)
            {
                success = RemoveBefore.Invoke(key, cas);
            }

            var operationResult = new TestOperationResult();
            if (Items.ContainsKey(key) && Cas[key] == cas && success)
            {
                operationResult.Success = true;
                Items.Remove(key);
                Cas.Remove(key);
            }
            else
            {
                operationResult.Success = false;
                operationResult.Status = ResponseStatus.KeyNotFound;
            }

            return operationResult;
        }

        public IOperationResult Remove(string key, ReplicateTo replicateTo)
        {
            throw new NotImplementedException();
        }

        public Task<IOperationResult> RemoveAsync(string key, ReplicateTo replicateTo)
        {
            throw new NotImplementedException();
        }

        public IOperationResult Remove(string key, ulong cas, ReplicateTo replicateTo)
        {
            throw new NotImplementedException();
        }

        public Task<IOperationResult> RemoveAsync(string key, ulong cas, ReplicateTo replicateTo)
        {
            throw new NotImplementedException();
        }

        public IOperationResult Remove(string key, ReplicateTo replicateTo, PersistTo persistTo)
        {
            throw new NotImplementedException();
        }

        public Task<IOperationResult> RemoveAsync(string key, ReplicateTo replicateTo, PersistTo persistTo)
        {
            throw new NotImplementedException();
        }

        public IOperationResult Remove(string key, ulong cas, ReplicateTo replicateTo, PersistTo persistTo)
        {
            throw new NotImplementedException();
        }

        public Task<IOperationResult> RemoveAsync(string key, ulong cas, ReplicateTo replicateTo, PersistTo persistTo)
        {
            throw new NotImplementedException();
        }

        public IDictionary<string, IOperationResult> Remove(IList<string> keys)
        {
            throw new NotImplementedException();
        }

        public IDictionary<string, IOperationResult> Remove(IList<string> keys, ParallelOptions options)
        {
            throw new NotImplementedException();
        }

        public IDictionary<string, IOperationResult> Remove(IList<string> keys, ParallelOptions options, int rangeSize)
        {
            throw new NotImplementedException();
        }

        public IOperationResult Touch(string key, TimeSpan expiration)
        {
            throw new NotImplementedException();
        }

        public Task<IOperationResult> TouchAsync(string key, TimeSpan expiration)
        {
            throw new NotImplementedException();
        }

        public IOperationResult<T> GetAndTouch<T>(string key, TimeSpan expiration)
        {
            throw new NotImplementedException();
        }

        public Task<IOperationResult<T>> GetAndTouchAsync<T>(string key, TimeSpan expiration)
        {
            throw new NotImplementedException();
        }

        public IDocumentResult<T> GetAndTouchDocument<T>(string key, TimeSpan expiration)
        {
            throw new NotImplementedException();
        }

        public Task<IDocumentResult<T>> GetAndTouchDocumentAsync<T>(string key, TimeSpan expiration)
        {
            throw new NotImplementedException();
        }

        public IDocumentResult<T> GetDocument<T>(string id)
        {
            throw new NotImplementedException();
        }

        public async Task<IDocumentResult<T>> GetDocumentAsync<T>(string id)
        {
            var documentResult = new TestDocumentResult<T>();
            if (Items.ContainsKey(id))
            {
                documentResult.Success = true;
                documentResult.Content = (T)Items[id];
                documentResult.Document = new Document<T>()
                {
                    Cas = Cas[id],
                    Content = (T)Items[id],
                    Id = id
                };
            }
            else
            {
                documentResult.Success = false;
                documentResult.Status = ResponseStatus.KeyNotFound;
            }

            return documentResult;
        }

        public IOperationResult<T> Get<T>(string key)
        {
            throw new NotImplementedException();
        }

        public Task<IOperationResult<T>> GetAsync<T>(string key)
        {
            throw new NotImplementedException();
        }

        public IOperationResult<T> GetFromReplica<T>(string key)
        {
            throw new NotImplementedException();
        }

        public Task<IOperationResult<T>> GetFromReplicaAsync<T>(string key)
        {
            throw new NotImplementedException();
        }

        public IDictionary<string, IOperationResult<T>> Get<T>(IList<string> keys)
        {
            var result = new Dictionary<string, IOperationResult<T>>();
            foreach (var key in keys)
            {
                var operationResult = new TestOperationResult<T>();
                if (Items.ContainsKey(key))
                {
                    operationResult.Success = true;
                    operationResult.Value = (T)Items[key];
                    operationResult.Cas = Cas[key];
                }
                else
                {
                    operationResult.Success = false;
                    operationResult.Status = ResponseStatus.KeyNotFound;
                }
                result.Add(key, operationResult);
            }

            return result;
        }

        public IDictionary<string, IOperationResult<T>> Get<T>(IList<string> keys, ParallelOptions options)
        {
            throw new NotImplementedException();
        }

        public IDictionary<string, IOperationResult<T>> Get<T>(IList<string> keys, ParallelOptions options, int rangeSize)
        {
            throw new NotImplementedException();
        }

        public IOperationResult<T> GetWithLock<T>(string key, uint expiration)
        {
            throw new NotImplementedException();
        }

        public Task<IOperationResult<T>> GetWithLockAsync<T>(string key, uint expiration)
        {
            throw new NotImplementedException();
        }

        public IOperationResult<T> GetWithLock<T>(string key, TimeSpan expiration)
        {
            throw new NotImplementedException();
        }

        public Task<IOperationResult<T>> GetWithLockAsync<T>(string key, TimeSpan expiration)
        {
            throw new NotImplementedException();
        }

        public IOperationResult Unlock(string key, ulong cas)
        {
            throw new NotImplementedException();
        }

        public Task<IOperationResult> UnlockAsync(string key, ulong cas)
        {
            throw new NotImplementedException();
        }

        public IOperationResult<ulong> Increment(string key)
        {
            throw new NotImplementedException();
        }

        public Task<IOperationResult<ulong>> IncrementAsync(string key)
        {
            throw new NotImplementedException();
        }

        public IOperationResult<ulong> Increment(string key, ulong delta)
        {
            throw new NotImplementedException();
        }

        public Task<IOperationResult<ulong>> IncrementAsync(string key, ulong delta)
        {
            throw new NotImplementedException();
        }

        public IOperationResult<ulong> Increment(string key, ulong delta, ulong initial)
        {
            throw new NotImplementedException();
        }

        public Task<IOperationResult<ulong>> IncrementAsync(string key, ulong delta, ulong initial)
        {
            throw new NotImplementedException();
        }

        public IOperationResult<ulong> Increment(string key, ulong delta, ulong initial, uint expiration)
        {
            throw new NotImplementedException();
        }

        public Task<IOperationResult<ulong>> IncrementAsync(string key, ulong delta, ulong initial, uint expiration)
        {
            throw new NotImplementedException();
        }

        public IOperationResult<ulong> Increment(string key, ulong delta, ulong initial, TimeSpan expiration)
        {
            throw new NotImplementedException();
        }

        public Task<IOperationResult<ulong>> IncrementAsync(string key, ulong delta, ulong initial, TimeSpan expiration)
        {
            throw new NotImplementedException();
        }

        public IOperationResult<ulong> Decrement(string key)
        {
            throw new NotImplementedException();
        }

        public Task<IOperationResult<ulong>> DecrementAsync(string key)
        {
            throw new NotImplementedException();
        }

        public IOperationResult<ulong> Decrement(string key, ulong delta)
        {
            throw new NotImplementedException();
        }

        public Task<IOperationResult<ulong>> DecrementAsync(string key, ulong delta)
        {
            throw new NotImplementedException();
        }

        public IOperationResult<ulong> Decrement(string key, ulong delta, ulong initial)
        {
            throw new NotImplementedException();
        }

        public Task<IOperationResult<ulong>> DecrementAsync(string key, ulong delta, ulong initial)
        {
            throw new NotImplementedException();
        }

        public IOperationResult<ulong> Decrement(string key, ulong delta, ulong initial, uint expiration)
        {
            throw new NotImplementedException();
        }

        public Task<IOperationResult<ulong>> DecrementAsync(string key, ulong delta, ulong initial, uint expiration)
        {
            throw new NotImplementedException();
        }

        public IOperationResult<ulong> Decrement(string key, ulong delta, ulong initial, TimeSpan expiration)
        {
            throw new NotImplementedException();
        }

        public Task<IOperationResult<ulong>> DecrementAsync(string key, ulong delta, ulong initial, TimeSpan expiration)
        {
            throw new NotImplementedException();
        }

        public IOperationResult<string> Append(string key, string value)
        {
            throw new NotImplementedException();
        }

        public Task<IOperationResult<string>> AppendAsync(string key, string value)
        {
            throw new NotImplementedException();
        }

        public IOperationResult<byte[]> Append(string key, byte[] value)
        {
            throw new NotImplementedException();
        }

        public Task<IOperationResult<byte[]>> AppendAsync(string key, byte[] value)
        {
            throw new NotImplementedException();
        }

        public IOperationResult<string> Prepend(string key, string value)
        {
            throw new NotImplementedException();
        }

        public Task<IOperationResult<string>> PrependAsync(string key, string value)
        {
            throw new NotImplementedException();
        }

        public IOperationResult<byte[]> Prepend(string key, byte[] value)
        {
            throw new NotImplementedException();
        }

        public Task<IOperationResult<byte[]>> PrependAsync(string key, byte[] value)
        {
            throw new NotImplementedException();
        }

        public IViewResult<T> Query<T>(IViewQuery query)
        {
            throw new NotImplementedException();
        }

        public Task<IViewResult<T>> QueryAsync<T>(IViewQuery query)
        {
            throw new NotImplementedException();
        }

        public IQueryResult<T> Query<T>(string query)
        {
            throw new NotImplementedException();
        }

        public Task<IQueryResult<T>> QueryAsync<T>(string query)
        {
            throw new NotImplementedException();
        }

        public IQueryResult<T> Query<T>(IQueryRequest queryRequest)
        {
            throw new NotImplementedException();
        }

        public Task<IQueryResult<T>> QueryAsync<T>(IQueryRequest queryRequest)
        {
            throw new NotImplementedException();
        }

        public IViewQuery CreateQuery(string designDoc, string view)
        {
            throw new NotImplementedException();
        }

        public IViewQuery CreateQuery(string designdoc, string view, bool development)
        {
            throw new NotImplementedException();
        }

        public IQueryResult<IQueryPlan> Prepare(string statement)
        {
            throw new NotImplementedException();
        }

        public IQueryResult<IQueryPlan> Prepare(IQueryRequest toPrepare)
        {
            throw new NotImplementedException();
        }

        public IBucketManager CreateManager(string username, string password)
        {
            throw new NotImplementedException();
        }

        public string Name
        {
            get { throw new NotImplementedException(); }
        }

        public BucketTypeEnum BucketType
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsSecure
        {
            get { throw new NotImplementedException(); }
        }
    }
}
