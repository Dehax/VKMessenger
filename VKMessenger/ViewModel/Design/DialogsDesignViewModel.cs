using System.Collections.ObjectModel;
using VKMessenger.Model;

namespace VKMessenger.ViewModel.Design
{
	public class DialogsDesignViewModel
    {
        public ObservableCollection<Dialog> Model { get; set; } = new ObservableCollection<Dialog>(new Dialog[]
        {
            new Dialog()
            {
                Chat = new VkNet.Model.Chat()
                {
                    Title = "Чат 1"
                }
            },
            new Dialog()
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
