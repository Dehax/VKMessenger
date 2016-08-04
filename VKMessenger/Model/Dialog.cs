﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using VkNet.Model;

namespace VKMessenger.Model
{
    public class Dialog : INotifyPropertyChanged
    {
        private IDictionary<long, VkMessage> _messages = new SortedDictionary<long, VkMessage>();
        public IDictionary<long, VkMessage> Messages
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