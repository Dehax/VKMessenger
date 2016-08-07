using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using VkNet;
using VkNet.Model;

namespace VKMessenger.Model
{
    public class Messages : INotifyPropertyChanged
    {
        private ObservableCollection<VkMessage> _messages = new ObservableCollection<VkMessage>();
        public ObservableCollection<VkMessage> Content
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

        public event PropertyChangedEventHandler PropertyChanged;

        public Messages()
        {
        }

        public Messages(IEnumerable<VkMessage> messages)
        {
            foreach (VkMessage message in messages)
            {
                _messages.Insert(0, message);
            }
        }

        public Messages(IEnumerable<Message> messages, Dialog dialog)
        {
            SetData(messages, dialog);
        }

        public void SetData(IEnumerable<Message> messages, Dialog dialog)
        {
            _messages.Clear();

            foreach (Message message in messages)
            {
                _messages.Insert(0, new VkMessage(message, dialog));
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName]string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
