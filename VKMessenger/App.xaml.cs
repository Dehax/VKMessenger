using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using VKMessenger.Properties;
using VKMessenger.View;
using VKMessenger.ViewModel;

namespace VKMessenger
{
	public partial class App : Application
	{
		private const string LOG_FILE_NAME = @"VKMessenger.log";

		private Messenger _messenger = new Messenger();

		public App()
		{
			ShutdownMode = ShutdownMode.OnExplicitShutdown;
			DispatcherUnhandledException += ProcessUnhandledException;
		}

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			if (Authenticate())
			{
				MainWindow mainWindow = new MainWindow(_messenger);
				MainWindow = mainWindow;
				mainWindow.Show();
				mainWindow.Closed += MainWindow_Closed;
			}
			else
			{
				Shutdown();
			}
		}

		public bool Authenticate()
		{
			string accessToken = Settings.Default.AccessToken;

			if (!string.IsNullOrWhiteSpace(accessToken))
			{
				if (!_messenger.Authorize(accessToken))
				{
					throw new Exception("Неправильный маркер доступа!");
				}
			}
			else
			{
				SetupWebBrowserEmulationVersion();
				AuthorizationWindow authWindow = new AuthorizationWindow();
				authWindow.ShowDialog();
				accessToken = authWindow.AccessToken;

				if (_messenger.Authorize(accessToken))
				{
					Settings.Default.AccessToken = accessToken;
					Settings.Default.Save();

					MessageBox.Show("Авторизация прошла успешно!", "Авторизовано");
				}
				else
				{
					return false;
				}
			}

			return true;
		}

		private void SetupWebBrowserEmulationVersion()
		{
			string appName = Process.GetCurrentProcess().ProcessName + ".exe";

			RegistryKey regKey = null;
			try
			{
				regKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\\Microsoft\\Internet Explorer\\Main\\FeatureControl\\FEATURE_BROWSER_EMULATION", true);

				if (regKey == null)
				{
					return;
				}

				string findAppkey = Convert.ToString(regKey.GetValue(appName));

				if (string.IsNullOrEmpty(findAppkey))
				{
					regKey.SetValue(appName, 11000, RegistryValueKind.DWord);
				}
				else if (findAppkey == "11000")
				{
					regKey.Close();
					return;
				}

				findAppkey = Convert.ToString(regKey.GetValue(appName));

				if (findAppkey != "11000")
				{
					throw new Exception($"Ошибка регистрации версии Internet Explorer: {findAppkey}");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
			finally
			{
				regKey?.Close();
			}
		}

		private void MainWindow_Closed(object sender, EventArgs e)
		{
			Shutdown();
		}

		private void ProcessUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			string localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create);

			StringBuilder sb = new StringBuilder(localAppDataPath);
			sb.Append(Path.DirectorySeparatorChar);
			Assembly assembly = Assembly.GetExecutingAssembly();
			sb.Append(assembly.GetCustomAttribute<AssemblyCompanyAttribute>().Company);
			sb.Append(Path.DirectorySeparatorChar);
			sb.Append(assembly.GetCustomAttribute<AssemblyProductAttribute>().Product);
			Directory.CreateDirectory(sb.ToString());
			sb.Append(Path.DirectorySeparatorChar);
			sb.Append(LOG_FILE_NAME);
			string logFilePath = sb.ToString();

			using (StreamWriter sw = File.CreateText(logFilePath))
			{
				sw.WriteLine(DateTime.Now.ToString() + " - Необработанное исключение:");
				sw.WriteLine(e.Exception);
			}

			MessageBox.Show($"Приложение прекратило работу из-за непредвиденной ошибки. Посмотрите файл журнала \"{ logFilePath }\"", "Exception");
		}
	}
}
