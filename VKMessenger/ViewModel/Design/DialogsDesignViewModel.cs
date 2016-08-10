using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VKMessenger.Model;

namespace VKMessenger.ViewModel.Design
{
    public class DialogsDesignViewModel
    {
        public Dialogs Model { get; set; } = new Dialogs(new Dialog[]
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
