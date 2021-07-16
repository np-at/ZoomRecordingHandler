using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ZoomFileManager.Extensions
{
    public class CustomTaskExtensions
    {
        /// <summary>
        /// Wrapper for Task.WhenAll to capture inner exceptions
        /// </summary>
        /// <param name="tasks"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="AggregateException?"></exception>
        public static async Task<IEnumerable<T>> WhenAll<T>(params Task<T>[] tasks)
        {
            var allTasks = Task.WhenAll(tasks);
            try
            {
                return await allTasks;
            }
            catch (Exception)
            {
                
                // ignore
            }

            throw allTasks.Exception ?? throw new Exception("Impossible");
        }
    }
}