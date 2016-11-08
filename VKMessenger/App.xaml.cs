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

		/// <summary>
		/// Команда смены пользователя.
		/// </summary>
		public SimpleCommand ReloginCommand { get; set; }
		/// <summary>
		/// Команда вызова настроек.
		/// </summary>
		public SimpleCommand SettingsCommand { get; set; }

		public App()
		{
			ShutdownMode = ShutdownMode.OnExplicitShutdown;
			AppDomain.CurrentDomain.UnhandledException += ProcessUnhandledException;
			//DispatcherUnhandledException += ProcessUnhandledException;

			ReloginCommand = new SimpleCommand(Relogin, () => { return true; });
			SettingsCommand = new SimpleCommand(OpenSettings, () => { return true; });
		}

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			_taskbarIcon = new TaskbarIcon();
			_taskbarIcon.ToolTip = nameof(VKMessenger);
			_taskbarIcon.IconSource = new BitmapImage(new Uri(@"pack://application:,,,/VKMessenger;component/Images/Icons/VKMessenger.ico"));
			_taskbarIcon.TrayMouseDoubleClick += taskbarIcon_TrayMouseDoubleClick;
			_taskbarIcon.ContextMenu = new ContextMenu();
			MenuItem reloginMenuItem = new MenuItem()
			{
				Command = ReloginCommand,
				Header = "Сменить пользователя"
			};
			MenuItem settingsMenuItem = new MenuItem()
			{
				Command = SettingsCommand,
				Header = "Настройки"
			};
			_taskbarIcon.ContextMenu.Items.Add(reloginMenuItem);
			_taskbarIcon.ContextMenu.Items.Add(settingsMenuItem);

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

		/// <summary>
		/// Отображает новое окно.
		/// </summary>
		private void ShowMainWindow()
		{
			MainWindow mainWindow = new MainWindow();
			MainViewModel vm = mainWindow.DataContext as MainViewModel;

			if (vm == null)
			{
				throw new ArgumentNullException(nameof(vm), "MainWindow.DataContext is not MainViewModel!");
			}

			vm.Messenger = _messenger;
			vm.Messenger.Start();
			vm.NewMessage += NewMessage;

			MainWindow = mainWindow;
			mainWindow.Show();
			mainWindow.Closed += MainWindow_Closed;
		}

		/// <summary>
		/// Отображает новое сообщение уведомлениеме в трее.
		/// </summary>
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

		/// <summary>
		/// Сменить пользователя.
		/// </summary>
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

		/// <summary>
		/// Отобразить окно настроек.
		/// </summary>
		private void OpenSettings()
		{
			SettingsWindow settingsWindow = new SettingsWindow();
			settingsWindow.ShowDialog();
		}

		/// <summary>
		/// Аутентифицировать.
		/// </summary>
		/// <param name="relogin">Запросить смену пользователя.</param>
		/// <returns>Показывает успешность аутентификации</returns>
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
				}
				else
				{
					MessageBox.Show("Ошибка авторизации!", "Не авторизован");
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Установить совместимую версию эмуляции браузера.
		/// </summary>
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

		/// <summary>
		/// Обработать исключение на уровне приложения.
		/// </summary>
		private void ProcessUnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			StringBuilder localAppFolderPath = new StringBuilder(Utils.Extensions.ApplicationFolderPath);
			localAppFolderPath.Append(Path.DirectorySeparatorChar);
			localAppFolderPath.Append(LOG_FILE_NAME);
			string logFilePath = localAppFolderPath.ToString();

			using (StreamWriter sw = File.CreateText(logFilePath))
			{
				sw.WriteLine(DateTime.Now.ToString() + " - Необработанное исключение:");
				sw.WriteLine(e.ExceptionObject);
			}

			MessageBox.Show($"Приложение прекратило работу из-за непредвиденной ошибки. Посмотрите файл журнала \"{ logFilePath }\"", "Exception");

			Process.Start(logFilePath);
		}
	}
}
