﻿using System;
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
        public VkMessage Message { get; set; }

        public NewMessageEventArgs(VkMessage message)
        {
            Message = message;
        }
    }

	/// <summary>
	/// Бизнес-логика основного окна мессенджера.
	/// </summary>
	public class MainViewModel : INotifyPropertyChanged
    {
		#region Свойства
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
					LoadConversations();
					OnPropertyChanged();
				}
			}
		}

		protected VkApi Vk { get { return _messenger.Vk; } }

        private string _sendingMessageText;
		/// <summary>
		/// Текст нового сообщения для отправки.
		/// </summary>
        public string SendingMessageText
        {
            get { return _sendingMessageText; }
            set
            {
                if (value != null && !value.Equals(_sendingMessageText))
                {
                    _sendingMessageText = value;
                    OnPropertyChanged();
                    SendMessageCommand.RaiseCanExecuteChanged();
                }
            }
        }

		private int _selectedConversationIndex = -1;
		/// <summary>
		/// Индекс выбранной беседы в списке.
		/// </summary>
		public int SelectedConversationIndex
		{
			get { return _selectedConversationIndex; }
			set
			{
				if (_selectedConversationIndex != value)
				{
					_selectedConversationIndex = value;
					OnSelectedConversationChanged();
					OnPropertyChanged();
					OnPropertyChanged(nameof(SelectedConversation));
				}
			}
		}

		private ObservableCollection<Conversation> _conversations = new ObservableCollection<Conversation>();
		/// <summary>
		/// Список бесед пользователя.
		/// </summary>
		public ObservableCollection<Conversation> Conversations
		{
			get { return _conversations; }
			set
			{
				if (_conversations != value)
				{
					_conversations = value;
					OnPropertyChanged();
				}
			}
		}

		/// <summary>
		/// Выбранная беседа.
		/// </summary>
		public Conversation SelectedConversation
		{
			get { return SelectedConversationIndex >= 0 ? Conversations[SelectedConversationIndex] : null; }
		}
		#endregion

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

		#region MVVM
		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName]string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
		#endregion

		public MainViewModel()
        {
            SendMessageCommand = new SimpleCommand(SendMessageExecute, CanSendMessage);
        }

		/// <summary>
		/// Обрабатывает новое сообщение.
		/// </summary>
		private void ReceiveNewMessage(object sender, MessageEventArgs e)
        {
            VkMessage message = e.Message;
            Conversation currentConversation = SelectedConversation;

            if (currentConversation != null
                && ((currentConversation.IsChat && currentConversation.Chat.Id == message.ChatId)
                || (!currentConversation.IsChat && currentConversation.User.Id == message.UserId.Value)))
            {
                message.Conversation = currentConversation;

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    currentConversation.Messages.Add(message);
                });
            }
			
            Conversation conversationForMessage = currentConversation;

			foreach (Conversation conversation in Conversations)
			{
				if ((conversation.IsChat && conversation.Chat.Id == message.ChatId)
					|| (!conversation.IsChat && conversation.User.Id == message.UserId.Value))
				{
					conversationForMessage = conversation;
					message.Conversation = conversationForMessage;
					break;
				}
			}

			OnNewMessage(message);
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
		/// <param name="conversation">Беседа, сообщение которой было получено.</param>
		/// <param name="message">Сообщение, которое было получено.</param>
        protected virtual void OnNewMessage(VkMessage message)
        {
            NewMessage?.Invoke(this, new NewMessageEventArgs(message));
        }

		/// <summary>
		/// Отправляет новое сообщение.
		/// </summary>
        private async void SendMessageExecute()
        {
			long id = await _messenger.SendMessage(SendingMessageText, SelectedConversation);
			SendingMessageText = string.Empty;
		}

		/// <summary>
		/// Проверяет возможность отправки сообщения.
		/// </summary>
		/// <returns></returns>
        private bool CanSendMessage()
        {
            return !string.IsNullOrWhiteSpace(SendingMessageText) && SelectedConversation != null;
        }

		/// <summary>
		/// Загружает беседы пользователя.
		/// </summary>
		private async void LoadConversations()
		{
			ReadOnlyCollection<Message> conversationsMessages = await GetConversationsList();

			for (int i = 0; i < conversationsMessages.Count; i++)
			{
				Message lastMessage = conversationsMessages[i];

				if (lastMessage.UserId < 0)
				{
					continue;
				}

				Conversation conversation = new Conversation();
				VkMessage message = new VkMessage(lastMessage, conversation);

				if (lastMessage.ChatId.HasValue)
				{
					conversation.Photo = lastMessage.Photo50;
					conversation.Chat = await LoadChat(lastMessage.ChatId.Value);
					conversation.Users = await LoadUsers(conversation.Chat.Users);
				}
				else
				{
					conversation.User = await LoadUser(lastMessage.UserId.Value);
				}


				Conversations.Add(conversation);
			}
		}

		/// <summary>
		/// Загружает информацию об указанных пользователях.
		/// </summary>
		/// <param name="usersIds">Список ID пользователей для загрузки.</param>
		/// <returns>Список пользователей.</returns>
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

		/// <summary>
		/// Загружает информацию об одном пользователе.
		/// </summary>
		/// <param name="userId">ID пользователя.</param>
		/// <returns>Пользователь.</returns>
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

		/// <summary>
		/// Загружает информацию о чате.
		/// </summary>
		/// <param name="chatId">ID чата.</param>
		/// <returns>Чат.</returns>
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

		/// <summary>
		/// Загружает список бесед пользователя.
		/// </summary>
		/// <returns>Список бесед.</returns>
		private Task<ReadOnlyCollection<Message>> GetConversationsList()
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

		/// <summary>
		/// Происходит при изменении выбора беседы.
		/// </summary>
		protected virtual async void OnSelectedConversationChanged()
		{
			long peerId = SelectedConversation.PeerId;
			List<Message> messages = await LoadHistory(peerId);
			
			SelectedConversation.Messages.Clear();

			foreach (Message message in messages)
			{
				SelectedConversation.Messages.Insert(0, new VkMessage(message, SelectedConversation));
			}
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
