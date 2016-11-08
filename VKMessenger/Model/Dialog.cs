using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using VkNet.Model;

namespace VKMessenger.Model
{
	/// <summary>
	/// Диалог с одним или чат с несколькими пользователями.
	/// </summary>
	public class Dialog : INotifyPropertyChanged
    {
        private Messages _messages = new Messages();
		/// <summary>
		/// Список сообщений диалога.
		/// </summary>
        public Messages Messages
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

        private string _photo;
		/// <summary>
		/// URL аватара диалога.
		/// </summary>
        public string Photo
        {
            get
            {
                if (IsChat)
                {
                    return _photo;
                }
                else
                {
                    return User.Photo50.AbsoluteUri;
                }
            }
            set
            {
                if (_photo != value)
                {
                    _photo = value;
                    OnPropertyChanged();
                }
            }
        }

		/// <summary>
		/// Является ли данный диалог чатом с несколькими собеседниками.
		/// </summary>
        public bool IsChat
        {
            get { return Chat != null; }
        }

		/// <summary>
		/// ID пользователя-собеседника в случае диалога, или ID чата.
		/// </summary>
        public long PeerId
        {
            get
            {
                if (IsChat)
                {
                    return Chat.Id + 2000000000;
                }
                else
                {
                    return User.Id;
                }
            }
        }

        private User _user;
		/// <summary>
		/// Пользователь-собеседник.
		/// </summary>
        public User User
        {
            get { return _user; }
            set
            {
                if (_user != value)
                {
                    _user = value;
                    OnPropertyChanged();
                }
            }
        }

        private List<User> _users;
		/// <summary>
		/// Список пользователей-собеседников.
		/// </summary>
        public List<User> Users
        {
            get { return _users; }
            set
            {
                if (_users != value)
                {
                    _users = value;
                    OnPropertyChanged();
                }
            }
        }

        private Chat _chat;
		/// <summary>
		/// Чат.
		/// </summary>
        public Chat Chat
        {
            get { return _chat; }
            set
            {
                if (_chat != value)
                {
                    _chat = value;
                    OnPropertyChanged();
                }
            }
        }

		/// <summary>
		/// Название диалога.
		/// </summary>
        public string Title
        {
            get
            {
                string title;

                if (IsChat)
                {
                    title = Chat.Title;
                }
                else
                {
                    title = $"{User.FirstName} {User.LastName}";
                }

                return title;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public Dialog()
        {
        }

        protected virtual void OnPropertyChanged([CallerMemberName]string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
