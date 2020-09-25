using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ZoomFileManager.Helpers
{
    using System;
    using System.IO;

    
      
    public class NoDedicatedThreadQueue
    {
        private Queue<string> _jobs = new Queue<string>();
        private bool _delegateQueuedOrRunning = false;
 
        public void Enqueue(string job)
        {
            lock (_jobs)
            {
                _jobs.Enqueue(job);
                if (!_delegateQueuedOrRunning)
                {
                    _delegateQueuedOrRunning = true;
                    ThreadPool.UnsafeQueueUserWorkItem(ProcessQueuedItems, null);
                }
            }
        }
 
        private void ProcessQueuedItems(object ignored)
        {
            while (true)
            {
                string item;
                lock (_jobs)
                {
                    if (_jobs.Count == 0)
                    {
                        _delegateQueuedOrRunning = false;
                        break;
                    }
 
                    item = _jobs.Dequeue();
                }
 
                try
                {
                    //do job
                    Console.WriteLine(item);
                }
                catch
                {
                    ThreadPool.UnsafeQueueUserWorkItem(ProcessQueuedItems, null);
                    throw;
                }
            }
        }
    }


// The example creates a file named "Test.data" and writes the integers 0 through 10 to it in binary format.
// It then writes the contents of Test.data to the console with each integer on a separate line.

  
}