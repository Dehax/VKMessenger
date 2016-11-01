using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Windows;
using VKMessenger.ViewModel;

namespace VKMessenger.View
{
	public partial class MainWindow : Window
    {
        private MainWindowViewModel _viewModel = new MainWindowViewModel();

		public MainWindow()
        {
            InitializeComponent();
			
            DataContext = _viewModel;
            dialogsListBox.DataContext = _viewModel.DialogsViewModel;
            dialogsListBox.ItemsSource = _viewModel.DialogsViewModel.Model.Content;
            messagesListBox.DataContext = _viewModel.MessagesViewModel;
            messagesListBox.ItemsSource = _viewModel.MessagesViewModel.Model.Content;

            _viewModel.NewMessage += ReceiveNewMessage;
            
            StateChanged += MainWindow_StateChanged;
        }

		private void ReceiveNewMessage(object sender, NewMessageEventArgs e)
        {
            if (e.Dialog == _viewModel.DialogsViewModel.SelectedDialog)
            {
                Dispatcher.Invoke(() =>
                {
                    messagesListBox.ScrollIntoView(messagesListBox.Items[messagesListBox.Items.Count - 1]);
                });
            }
        }

        public void UnTrayWindow()
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
