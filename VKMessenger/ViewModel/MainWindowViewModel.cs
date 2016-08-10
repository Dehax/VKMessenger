using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using VKMessenger.Model;
using VKMessenger.ViewModel.Commands;
using VkNet;
using VkNet.Model;

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
        public VkApi Vk { get { return _messenger.Vk; } }

        private DialogsLoader _dialogsViewModel;
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

        private MessagesLoader _messagesViewModel;
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

        public MainWindowViewModel(Messenger messenger)
        {
            _messenger = messenger;
            _messenger.NewMessage += ReceiveNewMessage;
            _messenger.Start();

            DialogsViewModel = new DialogsLoader(_messenger);
            DialogsViewModel.PropertyChanged += DialogsViewModel_PropertyChanged;
            MessagesViewModel = new MessagesLoader(_messenger);
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

            if (dialogForMessage == null)
            {
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
