using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using VkNet.Model;

namespace VKMessenger.Model
{
	public class Dialog : INotifyPropertyChanged
    {
        private Messages _messages = new Messages();
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

        public bool IsChat
        {
            get { return Chat != null; }
        }

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
