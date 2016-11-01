using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using VKMessenger.Properties;
using VKMessenger.View;
using VKMessenger.ViewModel;
using VKMessenger.ViewModel.Commands;

namespace VKMessenger
{
	public partial class App : Application
	{
		private const string LOG_FILE_NAME = @"VKMessenger.log";

		private bool _relogining = false;

		private Messenger _messenger = new Messenger();

		private TaskbarIcon _taskbarIcon;

		public SimpleCommand ReloginCommand { get; set; }

		public App()
		{
			ShutdownMode = ShutdownMode.OnExplicitShutdown;
			DispatcherUnhandledException += ProcessUnhandledException;

			ReloginCommand = new SimpleCommand(() => { Relogin(); }, () => { return true; });
		}

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			_taskbarIcon = new TaskbarIcon();
			_taskbarIcon.ToolTip = nameof(VKMessenger);
			_taskbarIcon.IconSource = new BitmapImage(new Uri(@"pack://application:,,,/VKMessenger;component/Images/Icons/VKMessenger.ico"));
			_taskbarIcon.TrayMouseDoubleClick += taskbarIcon_TrayMouseDoubleClick;
			_taskbarIcon.ContextMenu = new ContextMenu();
			MenuItem reloginMenuItem = new MenuItem();
			reloginMenuItem.Command = ReloginCommand;
			//reloginMenuItem.CommandBindings.Add(new CommandBinding(ReloginCommand));
			reloginMenuItem.Header = "Сменить пользователя";
			_taskbarIcon.ContextMenu.Items.Add(reloginMenuItem);

			if (Authenticate())
			{
				ShowMainWindow();
			}
			else
			{
				Shutdown();
			}
		}

		private void taskbarIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
		{
			((MainWindow)MainWindow).UnTrayWindow();
		}

		protected override void OnExit(ExitEventArgs e)
		{
			_taskbarIcon.Dispose();

			base.OnExit(e);
		}

		private void ShowMainWindow()
		{
			MainWindow mainWindow = new MainWindow();
			MainWindowViewModel vm = mainWindow.DataContext as MainWindowViewModel;

			if (vm == null)
			{
				throw new ArgumentNullException(nameof(vm), "MainWindow.DataContext is not MainWindowViewModel!");
			}

			vm.Messenger = _messenger;
			vm.Messenger.Start();
			//vm.Relogin += Relogin;
			vm.NewMessage += NewMessage;

			MainWindow = mainWindow;
			mainWindow.Show();
			mainWindow.Closed += MainWindow_Closed;
		}

		private void NewMessage(object sender, NewMessageEventArgs e)
		{
			if (e.Message.Content.FromId != Messenger.User.Id)
			{
				string title = e.Dialog != null ? e.Dialog.Title : "Новый диалог";
				string message = e.Message.Content.Body;

				Dispatcher.Invoke(() =>
				{
					_taskbarIcon.ShowBalloonTip(title, message, BalloonIcon.Info);
				});
			}
		}

		private async void Relogin()
		{
			_messenger.Stop();

			_relogining = true;

			MainWindow.Close();

			Settings.Default.AccessToken = string.Empty;

			await Current.Dispatcher.InvokeAsync(() => { Authenticate(true); });

			_relogining = false;

			ShowMainWindow();
		}

		private bool Authenticate(bool relogin = false)
		{
			string accessToken = Settings.Default.AccessToken;

			if (!relogin && !string.IsNullOrWhiteSpace(accessToken))
			{
				if (!_messenger.Authorize(accessToken))
				{
					throw new Exception("Неправильный маркер доступа!");
				}
			}
			else
			{
				SetupWebBrowserEmulationVersion();
				AuthorizationWindow authWindow = new AuthorizationWindow(true);
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
			if (!_relogining)
			{
				Shutdown();
			}
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
