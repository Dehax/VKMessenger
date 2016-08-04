using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VKMessenger.Model;
using VKMessenger.ViewModel;
using VkNet.Model;

namespace VKMessenger.View
{
    public partial class MainWindow : Window
    {
        private Messenger _messenger;

        public MainWindow(Messenger messenger)
        {
            _messenger = messenger;

            InitializeComponent();

            Loaded += MainWindow_Loaded;
            StateChanged += MainWindow_StateChanged;
            notifyIcon.TrayMouseDoubleClick += NotifyIcon_TrayMouseDoubleClick;

            dialogsListBox.SelectionChanged += SelectDialog;
            _messenger.NewMessage += ProcessNewMessage;

            sendButton.Click += SendMessage;
        }

        private void NotifyIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            Show();
            WindowState = WindowState.Normal;
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            switch (WindowState)
            {
                case WindowState.Minimized:
                    Hide();
                    break;
            }
        }

        private void SendMessage(object sender, RoutedEventArgs e)
        {
            string message = messageTextBox.Text;

            //_messenger.SendMessage(message, (Dialog)dialogsListBox.SelectedItem);
            //messagesListBox.Items.Refresh();

            messageTextBox.Clear();
        }

        private void SelectDialog(object sender, SelectionChangedEventArgs e)
        {
            Dialog dialog = (Dialog)e.AddedItems[0];
            messagesListBox.ItemsSource = dialog.Messages;
            messagesListBox.Items.Refresh();
        }

        private void ProcessNewMessage(object sender, MessageEventArgs e)
        {
            notifyIcon.ShowBalloonTip(e.Message.Title, e.Message.Body, BalloonIcon.Info);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            dialogsListBox.DataContext = new DialogsLoader(_messenger);
        }
    }
}
