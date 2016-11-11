using System.ComponentModel;
using System.Runtime.CompilerServices;
using VkNet.Enums;
using VkNet.Model;

namespace VKMessenger.Model
{
	/// <summary>
	/// Сообщение.
	/// </summary>
	public class VkMessage : Message, INotifyPropertyChanged
	{
		/// <summary>
		/// Беседа.
		/// </summary>
		public Conversation Conversation { get; set; }
		/// <summary>
		/// Автор сообщения.
		/// </summary>
		public User Author { get; set; }
		/// <summary>
		/// Время сообщения.
		/// </summary>
		public string TimePrint
		{
			get { return Date?.ToString("dd.MM.yyyy HH:mm:ss"); }
		}

		/// <summary>
		/// Фамилия Имя автора сообщения.
		/// </summary>
		public string AuthorFullName
		{
			get
			{
				if (Conversation.IsChat)
				{
					string title = Conversation.Title;

					foreach (User user in Conversation.Users)
					{
						if (FromId == user.Id)
						{
							title = $"{user.FirstName} {user.LastName}";
							break;
						}
					}

					return title;
				}
				else
				{
					if (FromId == Conversation.User.Id)
					{
						return $"{Conversation.User.FirstName} {Conversation.User.LastName}";
					}
					else
					{
						return $"{Messenger.User.FirstName} {Messenger.User.LastName}";
					}
				}
			}
		}

		/// <summary>
		/// URL аватара беседы.
		/// </summary>
		public string Image
		{
			get
			{
				if (Conversation.IsChat)
				{
					string image = Conversation.Photo;

					foreach (User user in Conversation.Users)
					{
						if (FromId == user.Id)
						{
							image = user.Photo50.AbsoluteUri;
							break;
						}
					}

					return image;
				}
				else
				{
					if (FromId == Conversation.User.Id)
					{
						return Conversation.User.Photo50.AbsoluteUri;
					}
					else
					{
						return Messenger.User.Photo50.AbsoluteUri;
					}
				}
			}
		}

		public new MessageReadState? ReadState
		{
			get
			{
				return base.ReadState;
			}
			set
			{
				base.ReadState = value;
				OnPropertyChanged();
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName]string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public VkMessage()
		{
		}

		public VkMessage(Message message)
		{
			Id = message.Id;
			OwnerId = message.OwnerId;

			Action = message.Action;
			ActionEmail = message.ActionEmail;
			ActionMid = message.ActionMid;
			ActionText = message.ActionText;
			AdminId = message.AdminId;
			Attachments = message.Attachments;
			Body = message.Body;
			ChatActiveIds = message.ChatActiveIds;
			ChatId = message.ChatId;
			ChatPushSettings = message.ChatPushSettings;
			ContainsEmojiSmiles = message.ContainsEmojiSmiles;
			Date = message.Date;
			ForwardedMessages = message.ForwardedMessages;
			FromId = message.FromId;
			Geo = message.Geo;
			InRead = message.InRead;
			IsDeleted = message.IsDeleted;
			IsImportant = message.IsImportant;
			OutRead = message.OutRead;
			Photo100 = message.Photo100;
			Photo200 = message.Photo200;
			Photo50 = message.Photo50;
			PhotoPreviews = message.PhotoPreviews;
			ReadState = message.ReadState;
			Title = message.Title;
			Type = message.Type;
			Unread = message.Unread;
			UserId = message.UserId;
			UsersCount = message.UsersCount;
		}

		/// <summary>
		/// Создаёт и добавляет сообщение в беседу.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		/// <param name="conversation">Беседа.</param>
		public VkMessage(Message message, Conversation conversation)
			: this(message)
		{
			Conversation = conversation;
			// TODO: Необходимо?
			//Conversation.Messages.Add(this);
		}
	}
}
