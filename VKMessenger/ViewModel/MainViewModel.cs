using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using VKMessenger.Model;
using VKMessenger.ViewModel.Commands;
using VkNet;
using VkNet.Enums.Filters;
using VkNet.Model;
using VkNet.Model.RequestParams;

namespace VKMessenger.ViewModel
{
	public class NewMessageEventArgs : EventArgs
    {
        public Conversation Dialog { get; set; }
        public VkMessage Message { get; set; }

        public NewMessageEventArgs(Conversation dialog, VkMessage message)
        {
            Dialog = dialog;
            Message = message;
        }
    }

	/// <summary>
	/// Бизнес-логика основного окна мессенджера.
	/// </summary>
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
					LoadDialogs();
					OnPropertyChanged();
				}
			}
		}

		public VkApi Vk { get { return _messenger.Vk; } }

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

		private int _selectedDialogIndex = -1;
		/// <summary>
		/// Индекс выбранного диалога в списке.
		/// </summary>
		public int SelectedDialogIndex
		{
			get { return _selectedDialogIndex; }
			set
			{
				if (_selectedDialogIndex != value)
				{
					_selectedDialogIndex = value;
					OnDialogChanged();
					OnPropertyChanged();
				}
			}
		}

		private ObservableCollection<Conversation> _dialogs = new ObservableCollection<Conversation>();
		/// <summary>
		/// Список диалогов пользователя.
		/// </summary>
		public ObservableCollection<Conversation> Conversations
		{
			get { return _dialogs; }
			set
			{
				if (_dialogs != value)
				{
					_dialogs = value;
					OnPropertyChanged();
				}
			}
		}

		/// <summary>
		/// Выбранный диалог.
		/// </summary>
		public Conversation SelectedDialog
		{
			get { return SelectedDialogIndex >= 0 ? Conversations[SelectedDialogIndex] : null; }
		}

		private ObservableCollection<VkMessage> _messages = new ObservableCollection<VkMessage>();
		/// <summary>
		/// Список сообщений выбранного диалога.
		/// </summary>
		public ObservableCollection<VkMessage> Messages
		{
			get { return _messages; }
			set
			{
				if (_messages != value)
				{
					_messages = value;
					OnPropertyChanged();
				}
			}
		}

		#region Команды
		/// <summary>
		/// Команда отправки нового сообщения.
		/// </summary>
		public SimpleCommand SendMessageCommand { get; set; }
		#endregion

		#region События
		/// <summary>
		/// Вызывается при получении нового сообщения.
		/// </summary>
		public event EventHandler<NewMessageEventArgs> NewMessage;
		#endregion

		public event PropertyChangedEventHandler PropertyChanged;

        public MainViewModel()
        {
            SendMessageCommand = new SimpleCommand(SendMessageExecute, CanSendMessage);
        }

		protected virtual void OnPropertyChanged([CallerMemberName]string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		/// <summary>
		/// Обрабатывает новое сообщение.
		/// </summary>
		private void ReceiveNewMessage(object sender, MessageEventArgs e)
        {
            VkMessage message = e.Message;
            Conversation currentDialog = SelectedDialog;

            if (currentDialog != null
                && ((currentDialog.IsChat && currentDialog.Chat.Id == message.ChatId)
                || (!currentDialog.IsChat && currentDialog.User.Id == message.UserId.Value)))
            {
                message.Conversation = currentDialog;

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    currentDialog.Messages.Add(message);
                });
            }
			
            Conversation dialogForMessage = currentDialog;

			foreach (Conversation dialog in Conversations)
			{
				if ((dialog.IsChat && dialog.Chat.Id == message.ChatId)
					|| (!dialog.IsChat && dialog.User.Id == message.UserId.Value))
				{
					dialogForMessage = dialog;
					message.Conversation = dialogForMessage;
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

		/// <summary>
		/// Вызывает событие получения нового сообщения.
		/// </summary>
		/// <param name="dialog">Диалог, сообщение которого было получено.</param>
		/// <param name="message">Сообщение, которое было получено.</param>
        protected virtual void OnNewMessage(Conversation dialog, VkMessage message)
        {
            NewMessage?.Invoke(this, new NewMessageEventArgs(dialog, message));
        }

		/// <summary>
		/// Отправляет новое сообщение.
		/// </summary>
        private async void SendMessageExecute()
        {
			long id = await _messenger.SendMessage(MessageText, SelectedDialog);
			MessageText = string.Empty;
		}

		/// <summary>
		/// Проверяет возможность отправки сообщения.
		/// </summary>
		/// <returns></returns>
        private bool CanSendMessage()
        {
            return !string.IsNullOrWhiteSpace(MessageText) && SelectedDialog != null;
        }

		private async void LoadDialogs()
		{
			ReadOnlyCollection<Message> dialogMessages = await GetDialogsList();

			for (int i = 0; i < dialogMessages.Count; i++)
			{
				Message lastMessage = dialogMessages[i];

				if (lastMessage.UserId < 0)
				{
					continue;
				}

				Conversation dialog = new Conversation();
				VkMessage message = new VkMessage(lastMessage, dialog);

				if (lastMessage.ChatId.HasValue)
				{
					dialog.Photo = lastMessage.Photo50;
					dialog.Chat = await LoadChat(lastMessage.ChatId.Value);
					dialog.Users = await LoadUsers(dialog.Chat.Users);
				}
				else
				{
					dialog.User = await LoadUser(lastMessage.UserId.Value);
				}


				Conversations.Add(dialog);
			}
		}

		private Task<List<User>> LoadUsers(Collection<long> usersIds)
		{
			return Task.Run(() =>
			{
				List<User> users = new List<User>(usersIds.Count);

				Utils.Extensions.BeginVkInvoke(Vk);
				ReadOnlyCollection<User> usersCollection = Vk.Users.Get(usersIds, ProfileFields.FirstName | ProfileFields.LastName | ProfileFields.Photo50);
				Utils.Extensions.EndVkInvoke();

				users.AddRange(usersCollection);

				return users;
			});
		}

		private Task<User> LoadUser(long userId)
		{
			return Task.Run(() =>
			{
				Utils.Extensions.BeginVkInvoke(Vk);
				User user = Vk.Users.Get(userId, ProfileFields.FirstName | ProfileFields.LastName | ProfileFields.Photo50);
				Utils.Extensions.EndVkInvoke();

				return user;
			});
		}

		private Task<Chat> LoadChat(long chatId)
		{
			return Task.Run(() =>
			{
				Utils.Extensions.BeginVkInvoke(Vk);
				Chat chat = Vk.Messages.GetChat(chatId);
				Utils.Extensions.EndVkInvoke();

				return chat;
			});
		}

		private Task<ReadOnlyCollection<Message>> GetDialogsList()
		{
			return Task.Run(() =>
			{
				List<Message> messages = new List<Message>();
				int offset = 0;

				Utils.Extensions.BeginVkInvoke(Vk);
				MessagesGetObject response = Vk.Messages.GetDialogs(new MessagesDialogsGetParams()
				{
					PreviewLength = 1,
					Count = 20
				});
				Utils.Extensions.EndVkInvoke();

				offset += response.Messages.Count;

				messages.AddRange(response.Messages);

				while (offset + response.Messages.Count < response.TotalCount)
				{
					Utils.Extensions.BeginVkInvoke(Vk);
					response = Vk.Messages.GetDialogs(new MessagesDialogsGetParams()
					{
						Offset = offset,
						PreviewLength = 1,
						Count = 20
					});
					Utils.Extensions.EndVkInvoke();

					offset += response.Messages.Count;

					messages.AddRange(response.Messages);
				}

				return new ReadOnlyCollection<Message>(messages);
			});
		}

		public void SetData(IEnumerable<Message> messages, Conversation dialog)
		{
			_messages.Clear();

			foreach (Message message in messages)
			{
				_messages.Insert(0, new VkMessage(message, dialog));
			}
		}

		protected virtual async void OnDialogChanged()
		{
			long peerId = SelectedDialog.PeerId;
			List<Message> messages = await LoadHistory(peerId);

			SetData(messages, SelectedDialog);
			SelectedDialog.Messages = Messages;
		}

		private Task<List<Message>> LoadHistory(long peerId)
		{
			return Task.Run(() =>
			{
				List<Message> messages = new List<Message>();

				Utils.Extensions.BeginVkInvoke(Vk);
				MessagesGetObject history = Vk.Messages.GetHistory(new MessagesGetHistoryParams()
				{
					PeerId = peerId,
					Count = 10
				});
				Utils.Extensions.EndVkInvoke();
				messages.AddRange(history.Messages);

				return messages;
			});
		}
	}
}
