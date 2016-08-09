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
        
        public Dialog Dialog { get; set; }

        public User Author { get; set; }

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
                return Author?.Photo50.AbsoluteUri;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public VkMessage()
        {
            _message = new Message();
        }

        public VkMessage(Message message, Dialog dialog)
        {
            _message = message;
            Dialog = dialog;

            //dialog.Messages.Content.Add(this);
        }

        protected virtual void OnPropertyChanged([CallerMemberName]string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
