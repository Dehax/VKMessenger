using System;
using System.Collections.ObjectModel;
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
                Body = "Тестовое сообщение 1.",
                Date = DateTime.Now,
                FromId = 1
            }, new Conversation() {
                User = new User()
                {
                    FirstName = "Имя",
                    LastName = "Фамилия",
                    Id = 1
                }
            }),
            new VkMessage(new Message()
            {
                Body = "Тестовое сообщение 2.",
                Date = DateTime.Now
            }, null),
            new VkMessage(new Message()
            {
                Body = "Очень длинное тестовое сообщение.\nС переносом строки.\nПозволяет протестировать ширину шаблона.",
                Date = DateTime.Now,
                FromId = 2
            }, new Conversation() {
                User = new User()
                {
                    FirstName = "Имя",
                    LastName = "Фамилия",
                    Id = 2
                }
            })
        };
        public ObservableCollection<VkMessage> Messages
        {
            get { return _messages; }
        }

        public MessagesDesignViewModel()
        {
        }
    }
}
