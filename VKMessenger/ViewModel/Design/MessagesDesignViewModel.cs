using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VKMessenger.Model;
using VkNet.Model;

namespace VKMessenger.ViewModel.Design
{
    public class MessagesDesignViewModel
    {
        private ObservableCollection<VkMessage> _messages = new ObservableCollection<VkMessage>()
        {
            new VkMessage(new Message()
            {
                Body = "Hello, WOrld!",
                Date = DateTime.Now
            }),
            new VkMessage(new Message()
            {
                Body = "Hello, WOrld!",
                Date = DateTime.Now
            }),
            new VkMessage(new Message()
            {
                Body = "Hello, WOrld У тебя uBlock блокирует в новом дизайне ВК слева рекламу?",
                Date = DateTime.Now
            })
        };
        public ObservableCollection<VkMessage> Messages
        {
            get { return _messages; }
        }

        public MessagesDesignViewModel()
        {
            Messages.Add(new VkMessage(new Message()
            {
                Body = "Dehax none",
                Date = DateTime.Now
            }));
        }
    }
}
