using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Transactions;
using ZFHandler.Models;
using ZoomFileManager.Models;

namespace ZoomFileManager.Services
{
    public static class JobTracker
    {
        private static ConcurrentDictionary<string, Batch> _batches;

        static JobTracker()
        {
            int numProcs = Environment.ProcessorCount;
            int concurrencyLevel = numProcs * 2;
            _batches = new ConcurrentDictionary<string, Batch>(concurrencyLevel, 101);
        }

        public static Batch? GetBatch(string batchId) => _batches.GetValueOrDefault(batchId);

        public static void TryAdd(string batchId, Batch batch) => _batches.TryAdd(batchId, batch);

        /// <summary>
        /// Creates new batch instance in dictionary.  Returns Id handle for Batch instance
        /// </summary>
        /// <returns>string Id that corresponds to batch instance</returns>
        public static string CreateNewBatchInstance()
        {
            var newId = Guid.NewGuid().ToString("N");
            if (_batches.TryAdd(newId, new Batch(newId)))
                return newId;
            throw new TransactionInDoubtException("Error creating new  batch instance in dictionary");
        }

        public static ConcurrentDictionary<string, Batch> Batches
        {
            get => _batches;
            set => _batches = value ?? throw new ArgumentNullException(nameof(value));
        }
    }
}