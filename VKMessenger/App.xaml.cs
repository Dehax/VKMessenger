using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
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
            Startup += App_Startup;
            DispatcherUnhandledException += ProcessUnhandledException;
        }

        private void App_Startup(object sender, StartupEventArgs e)
        {
            if (_messenger.Authenticate())
            {
                MessageBox.Show("Авторизация прошла успешно!", "Авторизовано");

                MainWindow mainWindow = new MainWindow(_messenger);
                MainWindow = mainWindow;
                mainWindow.Show();
                mainWindow.Closed += MainWindow_Closed;
            }
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            Shutdown();
        }

        private void ProcessUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            string localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create);

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
