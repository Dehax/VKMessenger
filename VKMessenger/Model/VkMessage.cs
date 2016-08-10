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
            get { return _message.Date?.ToString("dd.MM.yyyy HH:mm:ss"); }
        }

        public string Title
        {
            get
            {
                if (Dialog.IsChat)
                {
                    string title = Dialog.Title;

                    foreach (User user in Dialog.Users)
                    {
                        if (Content.FromId == user.Id)
                        {
                            title = $"{user.FirstName} {user.LastName}";
                            break;
                        }
                    }

                    return title;
                }
                else
                {
                    if (Content.FromId == Dialog.User.Id)
                    {
                        return $"{Dialog.User.FirstName} {Dialog.User.LastName}";
                    }
                    else
                    {
                        return $"{Messenger.User.FirstName} {Messenger.User.LastName}";
                    }
                }
            }
        }

        public string Image
        {
            get
            {
                if (Dialog.IsChat)
                {
                    string image = Dialog.Photo;

                    foreach (User user in Dialog.Users)
                    {
                        if (Content.FromId == user.Id)
                        {
                            image = user.Photo50.AbsoluteUri;
                            break;
                        }
                    }

                    return image;
                }
                else
                {
                    if (Content.FromId == Dialog.User.Id)
                    {
                        return Dialog.User.Photo50.AbsoluteUri;
                    }
                    else
                    {
                        return Messenger.User.Photo50.AbsoluteUri;
                    }
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public VkMessage()
        {
            _message = new Message();
        }

        public VkMessage(Message message)
        {
            _message = message;
        }

        public VkMessage(Message message, Dialog dialog)
        {
            _message = message;
            Dialog = dialog;
        }

        protected virtual void OnPropertyChanged([CallerMemberName]string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
