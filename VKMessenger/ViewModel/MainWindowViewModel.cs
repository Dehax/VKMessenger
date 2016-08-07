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

        private Dispatcher _dispatcher;
        public Dispatcher Dispatcher { get { return _dispatcher; } }

        public SendMessageCommand SendMessageCommand { get; set; }

        public event EventHandler<NewMessageEventArgs> NewMessage;

        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindowViewModel(Messenger messenger, Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;

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
            Message message = e.Message;
            Dialog currentDialog = DialogsViewModel.SelectedDialog;

            if (currentDialog != null
                && ((currentDialog.IsChat && currentDialog.Chat.Id == message.ChatId)
                || (!currentDialog.IsChat && currentDialog.User.Id == message.UserId.Value)))
            {
                Dispatcher.Invoke(() =>
                {
                    currentDialog.Messages.Content.Add(new VkMessage(message));
                });
            }

            Dialog dialogForMessage = null;

            foreach (Dialog dialog in DialogsViewModel.Model.Content)
            {
                if ((dialog.IsChat && dialog.Chat.Id == message.ChatId)
                    || (!dialog.IsChat && dialog.User.Id == message.UserId.Value))
                {
                    dialogForMessage = dialog;
                    break;
                }
            }

            OnNewMessage(dialogForMessage, new VkMessage(message));
        }

        private void DialogsViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(DialogsViewModel.SelectedDialogIndex):
                    break;
            }

            MessagesViewModel.Dialog = DialogsViewModel.SelectedDialog;
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

                Task<Message> getMessageTask = Task.Run(() =>
                {
                    Utils.Extensions.SleepIfTooManyRequests(Vk);
                    return Vk.Messages.GetById((ulong)sentMessageId);
                });

                Message messageObject = await getMessageTask;
                MessagesViewModel.Model.Content.Add(new VkMessage(messageObject));
                MessageText = string.Empty;
            }
            catch (Exception)
            {
            }
        }

        private bool CanSendMessage()
        {
            return !string.IsNullOrWhiteSpace(MessageText);
        }
    }
}
