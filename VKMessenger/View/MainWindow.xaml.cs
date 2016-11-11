using System;
using System.Windows;
using VKMessenger.ViewModel;

namespace VKMessenger.View
{
	/// <summary>
	/// Основное окно мессенджера.
	/// </summary>
	public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;

		public MainWindow()
        {
            InitializeComponent();

			_viewModel = DataContext as MainViewModel;

			if (_viewModel == null)
			{
				throw new NotSupportedException("Не поддерживается ViewModel, отличный от MainViewModel");
			}

            _viewModel.NewMessage += ScrollToLastMessage;
			_viewModel.MessageSent += ScrollToLastMessage;
			_viewModel.ErrorSendMessage += ErrorSendMessage;
            
            StateChanged += MainWindow_StateChanged;
        }

		private void ErrorSendMessage(object sender, EventArgs e)
		{
			MessageBox.Show(this, "Ошибка отправки сообщения!", "Возможно, собеседник не запустил мессенджер!");
		}

		private void ScrollToLastMessage(object sender, NewMessageEventArgs e)
        {
            if (e.Message.Conversation == _viewModel.SelectedConversation)
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
