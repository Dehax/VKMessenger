using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VKMessenger.Model;
using VkNet;
using VkNet.Enums.Filters;
using VkNet.Model;
using VkNet.Model.RequestParams;

namespace VKMessenger.ViewModel
{
    public class DialogsLoader : INotifyPropertyChanged
    {
        private Messenger _messenger;
        private VkApi Vk { get { return _messenger.Vk; } }

        private Dialogs _model;
        public Dialogs Model
        {
            get { return _model; }
            set
            {
                _model = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public DialogsLoader(Messenger messenger)
        {
            _messenger = messenger;

            LoadDialogs();
        }

        private async void LoadDialogs()
        {
            ReadOnlyCollection<Message> dialogMessages = await GetDialogsList();
            List<Dialog> dialogs = new List<Dialog>();

            for (int i = 0; i < dialogMessages.Count; i++)
            {
                Message lastMessage = dialogMessages[i];
                Dialog dialog = new Dialog();
                VkMessage message = new VkMessage(lastMessage);

                if (lastMessage.ChatId.HasValue)
                {
                    dialog.Photo = lastMessage.Photo50;
                    dialog.Chat = await LoadChat(lastMessage.ChatId.Value);
                }
                else
                {
                    dialog.User = await LoadUser(lastMessage.UserId.Value);
                }


                dialogs.Add(dialog);
            }

            Model = new Dialogs(dialogs);
        }

        private Task<User> LoadUser(long userId)
        {
            return Task.Run(() =>
            {
                return Vk.Users.Get(userId, ProfileFields.FirstName | ProfileFields.LastName | ProfileFields.Photo50);
            });
        }

        private Task<Chat> LoadChat(long chatId)
        {
            return Task.Run(() =>
            {
                return Vk.Messages.GetChat(chatId);
            });
        }

        private Task<ReadOnlyCollection<Message>> GetDialogsList()
        {
            return Task.Run(() =>
            {
                MessagesGetObject response = Vk.Messages.GetDialogs(new MessagesDialogsGetParams()
                {
                    Count = 10
                });

                return response.Messages;
            });
        }

        protected virtual void OnPropertyChanged([CallerMemberName]string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
