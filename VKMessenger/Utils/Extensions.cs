using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VkNet;

namespace VKMessenger.Utils
{
	public static class Extensions
	{
		/// <summary>
		/// Точное время последнего вызова VK API-метода.
		/// </summary>
		private static DateTime _lastVkInvokeTime;
		private static Mutex _mutex = new Mutex(false, "VKMessenger");
		private static string _localAppFolderPath = null;

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

		/// <summary>
		/// Получить право выполнить вызов метода VK API.
		/// </summary>
		/// <param name="vk"></param>
		public static void BeginVkInvoke(VkApi vk)
		{
			_mutex.WaitOne();
			TimeSpan lastDelay = DateTime.Now - _lastVkInvokeTime;

			int delay = 1000 / vk.RequestsPerSecond + 1;
			int timespan = (int)lastDelay.TotalMilliseconds - 1;
			if (timespan < delay)
			{
				Thread.Sleep(delay - timespan);
			}
		}

		/// <summary>
		/// Освободить право выполнения метода VK API.
		/// </summary>
		public static void EndVkInvoke()
		{
			_lastVkInvokeTime = DateTime.Now;
			_mutex.ReleaseMutex();
		}

		/// <summary>
		/// Путь к папке программы в локальной папке пользователя.
		/// </summary>
		public static string ApplicationFolderPath
		{
			get
			{
				if (_localAppFolderPath != null)
				{
					return _localAppFolderPath;
				}

				string localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create);

				StringBuilder sb = new StringBuilder(localAppDataPath);
				sb.Append(Path.DirectorySeparatorChar);
				Assembly assembly = Assembly.GetExecutingAssembly();
				sb.Append(assembly.GetCustomAttribute<AssemblyCompanyAttribute>().Company);
				sb.Append(Path.DirectorySeparatorChar);
				sb.Append(assembly.GetCustomAttribute<AssemblyProductAttribute>().Product);
				_localAppFolderPath = sb.ToString();
				Directory.CreateDirectory(_localAppFolderPath);

				return _localAppFolderPath;
			}
		}
	}
}
