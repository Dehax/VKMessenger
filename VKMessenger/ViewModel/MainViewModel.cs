using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using VKMessenger.Model;
using VKMessenger.ViewModel.Commands;
using VkNet;

namespace VKMessenger.ViewModel
{
	public class NewMessageEventArgs : EventArgs
    {
        public Dialog Dialog { get; set; }
        public VkMessage Message { get; set; }

        public NewMessageEventArgs(Dialog dialog, VkMessage message)
        {
            Dialog = dialog;
            Message = message;
        }
    }

	public class MainViewModel : INotifyPropertyChanged
    {
        private Messenger _messenger;
		public Messenger Messenger
		{
			get { return _messenger; }
			set
			{
				if (_messenger != value)
				{
					_messenger = value;
					_messenger.NewMessage += ReceiveNewMessage;
					_messenger.MessageSent += NewMessageSent;
					DialogsViewModel.Messenger = _messenger;
					MessagesViewModel.Messenger = _messenger;
					DialogsViewModel.LoadDialogs();
					OnPropertyChanged();
				}
			}
		}

		public VkApi Vk { get { return _messenger.Vk; } }

        private DialogsLoader _dialogsViewModel = new DialogsLoader();
        public DialogsLoader DialogsViewModel
        {
            get { return _dialogsViewModel; }
            set
            {
                if (_dialogsViewModel != value)
                {
                    _dialogsViewModel = value;
                    OnPropertyChanged();
                }
            }
        }

        private MessagesLoader _messagesViewModel = new MessagesLoader();
        public MessagesLoader MessagesViewModel
        {
            get { return _messagesViewModel; }
            set
            {
                if (_messagesViewModel != value)
                {
                    _messagesViewModel = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _messageText;
		/// <summary>
		/// Текст нового сообщения для отправки.
		/// </summary>
        public string MessageText
        {
            get { return _messageText; }
            set
            {
                if (value != null && !value.Equals(_messageText))
                {
                    _messageText = value;
                    OnPropertyChanged();
                    SendMessageCommand.RaiseCanExecuteChanged();
                }
            }
        }
		
		/// <summary>
		/// Команда отправки нового сообщения.
		/// </summary>
        public SimpleCommand SendMessageCommand { get; set; }

		/// <summary>
		/// Вызывается при получении нового сообщения.
		/// </summary>
		public event EventHandler<NewMessageEventArgs> NewMessage;

        public event PropertyChangedEventHandler PropertyChanged;

        public MainViewModel()
        {
            DialogsViewModel.PropertyChanged += DialogsViewModel_PropertyChanged;
            SendMessageCommand = new SimpleCommand(SendMessageExecute, CanSendMessage);
        }

		/// <summary>
		/// Обрабатывает новое сообщение.
		/// </summary>
        private void ReceiveNewMessage(object sender, MessageEventArgs e)
        {
            VkMessage message = e.Message;
            Dialog currentDialog = DialogsViewModel.SelectedDialog;

            if (currentDialog != null
                && ((currentDialog.IsChat && currentDialog.Chat.Id == message.Content.ChatId)
                || (!currentDialog.IsChat && currentDialog.User.Id == message.Content.UserId.Value)))
            {
                message.Dialog = currentDialog;

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    currentDialog.Messages.Content.Add(message);
                });
            }
			
            Dialog dialogForMessage = currentDialog;

			foreach (Dialog dialog in DialogsViewModel.Model.Content)
			{
				if ((dialog.IsChat && dialog.Chat.Id == message.Content.ChatId)
					|| (!dialog.IsChat && dialog.User.Id == message.Content.UserId.Value))
				{
					dialogForMessage = dialog;
					message.Dialog = dialogForMessage;
					break;
				}
			}

			OnNewMessage(dialogForMessage, message);
        }

		/// <summary>
		/// Обрабатывает отправленное сообщение.
		/// </summary>
		private void NewMessageSent(object sender, MessageEventArgs e)
		{
			// TODO: Check this out.
			ReceiveNewMessage(sender, e);
		}

        private void DialogsViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(DialogsViewModel.SelectedDialogIndex):
                    MessagesViewModel.Dialog = DialogsViewModel.SelectedDialog;
                    SendMessageCommand.RaiseCanExecuteChanged();
                    break;
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName]string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

		/// <summary>
		/// Вызывает событие получения нового сообщения.
		/// </summary>
		/// <param name="dialog">Диалог, сообщение которого было получено.</param>
		/// <param name="message">Сообщение, которое было получено.</param>
        protected virtual void OnNewMessage(Dialog dialog, VkMessage message)
        {
            NewMessage?.Invoke(this, new NewMessageEventArgs(dialog, message));
        }

		/// <summary>
		/// Отправляет новое сообщение.
		/// </summary>
        private async void SendMessageExecute()
        {
			await _messenger.SendMessage(MessageText, DialogsViewModel.SelectedDialog);
			MessageText = string.Empty;
		}

		/// <summary>
		/// Проверяет возможность отправки сообщения.
		/// </summary>
		/// <returns></returns>
        private bool CanSendMessage()
        {
            return !string.IsNullOrWhiteSpace(MessageText) && DialogsViewModel.SelectedDialog != null;
        }
    }
}
