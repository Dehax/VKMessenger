﻿using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        public MainWindow(Messenger messenger)
        {
            _messenger = messenger;

            InitializeComponent();

            MainWindowViewModel viewModel = new MainWindowViewModel(_messenger, Dispatcher);
            DataContext = viewModel;
            dialogsListBox.DataContext = viewModel.DialogsViewModel;
            dialogsListBox.ItemsSource = viewModel.DialogsViewModel.Model.Content;
            messagesListBox.DataContext = viewModel.MessagesViewModel;
            messagesListBox.ItemsSource = viewModel.MessagesViewModel.Model.Content;

            viewModel.NewMessage += ReceiveNewMessage;
            
            StateChanged += MainWindow_StateChanged;
            notifyIcon.TrayMouseDoubleClick += NotifyIcon_TrayMouseDoubleClick;
        }

        private void ReceiveNewMessage(object sender, NewMessageEventArgs e)
        {
            string title = e.Dialog != null ? e.Dialog.Title : "Новый диалог";
            string message = e.Message.Content.Body;

            Dispatcher.Invoke(() =>
            {
                notifyIcon.ShowBalloonTip(title, message, BalloonIcon.Info);
            });
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
