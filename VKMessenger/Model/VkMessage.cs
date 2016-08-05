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
    public class VkMessage : INotifyPropertyChanged
    {
        private Message _message;
        public Message Content
        {
            get { return _message; }
        }

        public string TimePrint
        {
            get { return _message.Date?.ToString("HH:mm:ss dd.MMM.yyyy"); }
        }

        public string Title
        {
            get { return _message.Title; }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public VkMessage(Message message)
        {
            _message = message;
        }

        protected virtual void OnPropertyChanged([CallerMemberName]string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
