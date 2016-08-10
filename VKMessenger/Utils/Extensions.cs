using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VkNet;

namespace VKMessenger.Utils
{
    public static class Extensions
    {
        private static DateTime _lastVkInvokeTime;
        private static readonly object _syncRoot = new object();

        static Extensions()
        {
            _lastVkInvokeTime = DateTime.Now;
        }

        public static async Task<HttpWebResponse> GetResponseAsync(this HttpWebRequest request, CancellationToken ct)
        {
            using (ct.Register(() => request.Abort(), false))
            {
                try
                {
                    var response = await request.GetResponseAsync();
                    return (HttpWebResponse)response;
                }
                catch (WebException ex)
                {
                    if (ct.IsCancellationRequested)
                    {
                        throw new OperationCanceledException(ex.Message, ex, ct);
                    }
                    
                    throw;
                }
            }
        }
        
        public static void BeginVkInvoke(VkApi vk)
        {
            lock (_syncRoot)
            {
                TimeSpan lastDelay = DateTime.Now - _lastVkInvokeTime;

                int delay = 1000 / vk.RequestsPerSecond + 1;
                int timespan = (int)lastDelay.TotalMilliseconds - 1;
                if (timespan < delay)
                {
                    Thread.Sleep(delay - timespan);
                }
            }
        }

        public static void EndVkInvoke()
        {
            lock (_syncRoot)
            {
                _lastVkInvokeTime = DateTime.Now;
            }
        }
    }
}
