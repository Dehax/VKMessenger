using System.Collections.ObjectModel;
using VKMessenger.Model;

namespace VKMessenger.ViewModel.Design
{
	public class DialogsDesignViewModel
    {
        public ObservableCollection<Conversation> Model { get; set; } = new ObservableCollection<Conversation>(new Conversation[]
        {
            new Conversation()
            {
                Chat = new VkNet.Model.Chat()
                {
                    Title = "Чат 1"
                }
            },
            new Conversation()
            {
                User = new VkNet.Model.User()
                {
                    FirstName = "Имя",
                    LastName = "Фамилия"
                }
            }
        });
    }
}
