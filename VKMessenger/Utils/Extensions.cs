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

        public static void SleepIfTooManyRequests(VkApi vk)
        {
            int delay = 1000 / vk.RequestsPerSecond + 1;
            int timespan = vk.LastInvokeTimeSpan.HasValue ? (int)vk.LastInvokeTimeSpan.Value.TotalMilliseconds - 1 : 0;
            if (timespan < delay)
            {
                Thread.Sleep(delay - timespan);
            }
        }
    }
}
