using System;
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

        private Dialog _dialog;
        public Dialog Dialog
        {
            get { return _dialog; }
        }

        public string TimePrint
        {
            get { return _message.Date?.ToString("HH:mm:ss dd.MM.yyyy"); }
        }

        public string Title
        {
            get { return _message.Title; }
        }

        public string Image
        {
            get
            {
                if (Dialog.IsChat)
                {
                    return Dialog.Photo;
                }

                return null;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public VkMessage(Message message, Dialog dialog)
        {
            _message = message;
            _dialog = dialog;
        }

        protected virtual void OnPropertyChanged([CallerMemberName]string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
