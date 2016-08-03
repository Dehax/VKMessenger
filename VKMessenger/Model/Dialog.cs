using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VkNet.Model;

namespace VKMessenger.Model
{
    public class Dialog
    {
        public User Destination { get; set; }
        public List<Message> Messages { get; set; } = new List<Message>();

        public string LastMessage { get { return Messages.Last().Body; } }
        public string UserFullName
        {
            get
            {
                return $"{Destination.FirstName} {Destination.LastName}";
            }
        }

        public Dialog(User user, List<Message> messages)
        {
            Destination = user;
            Messages.AddRange(messages);
        }

        public void AddMessage(Message message)
        {
            Messages.Add(message);
        }
    }
}
