﻿using System;
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
			
            dialogsListBox.ItemsSource = _viewModel.Dialogs;
            messagesListBox.ItemsSource = _viewModel.Messages;

            _viewModel.NewMessage += ReceiveNewMessage;
            
            StateChanged += MainWindow_StateChanged;
        }

		private void ReceiveNewMessage(object sender, NewMessageEventArgs e)
        {
            if (e.Dialog == _viewModel.SelectedDialog)
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
