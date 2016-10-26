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

    public class MainWindowViewModel : INotifyPropertyChanged
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

        public SendMessageCommand SendMessageCommand { get; set; }

        public event EventHandler<NewMessageEventArgs> NewMessage;

        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindowViewModel()
        {
            DialogsViewModel.PropertyChanged += DialogsViewModel_PropertyChanged;
            SendMessageCommand = new SendMessageCommand(SendMessageExecute, CanSendMessage);
        }

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

        protected virtual void OnNewMessage(Dialog dialog, VkMessage message)
        {
            NewMessage?.Invoke(this, new NewMessageEventArgs(dialog, message));
        }

        private async void SendMessageExecute()
        {
            try
            {
                long sentMessageId = await _messenger.SendMessage(MessageText, DialogsViewModel.SelectedDialog);
                MessageText = string.Empty;
            }
            catch (Exception)
            {
            }
        }

        private bool CanSendMessage()
        {
            return !string.IsNullOrWhiteSpace(MessageText) && DialogsViewModel.SelectedDialog != null;
        }
    }
}
