using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
        private MainWindowViewModel _viewModel;

        public MainWindow(Messenger messenger)
        {
            _messenger = messenger;

            InitializeComponent();

            _viewModel = new MainWindowViewModel(_messenger);
            DataContext = _viewModel;
            dialogsListBox.DataContext = _viewModel.DialogsViewModel;
            dialogsListBox.ItemsSource = _viewModel.DialogsViewModel.Model.Content;
            messagesListBox.DataContext = _viewModel.MessagesViewModel;
            messagesListBox.ItemsSource = _viewModel.MessagesViewModel.Model.Content;

            _viewModel.NewMessage += ReceiveNewMessage;
            
            StateChanged += MainWindow_StateChanged;
            notifyIcon.TrayMouseDoubleClick += NotifyIcon_TrayMouseDoubleClick;
        }

        private void ReceiveNewMessage(object sender, NewMessageEventArgs e)
        {
            if (e.Message.Content.FromId != Messenger.User.Id)
            {
                string title = e.Dialog != null ? e.Dialog.Title : "Новый диалог";
                string message = e.Message.Content.Body;

                Dispatcher.Invoke(() =>
                {
                    notifyIcon.ShowBalloonTip(title, message, BalloonIcon.Info);
                });
            }

            if (e.Dialog == _viewModel.DialogsViewModel.SelectedDialog)
            {
                Dispatcher.Invoke(() =>
                {
                    messagesListBox.ScrollIntoView(messagesListBox.Items[messagesListBox.Items.Count - 1]);
                });
            }
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
    }
}
