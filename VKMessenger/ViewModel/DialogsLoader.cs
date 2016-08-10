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

        private Dialogs _model = new Dialogs();
        public Dialogs Model
        {
            get { return _model; }
            set
            {
                _model = value;
                OnPropertyChanged();
            }
        }

        private int _selectedDialogIndex = -1;
        public int SelectedDialogIndex
        {
            get { return _selectedDialogIndex; }
            set
            {
                if (_selectedDialogIndex != value)
                {
                    _selectedDialogIndex = value;
                    OnPropertyChanged();
                }
            }
        }

        //private Dialog _selectedDialog;
        public Dialog SelectedDialog
        {
            get { return SelectedDialogIndex >= 0 ? Model.Content[SelectedDialogIndex] : null; }
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

            for (int i = 0; i < dialogMessages.Count; i++)
            {
                Message lastMessage = dialogMessages[i];

                if (lastMessage.UserId < 0)
                {
                    continue;
                }

                Dialog dialog = new Dialog();
                VkMessage message = new VkMessage(lastMessage, dialog);

                if (lastMessage.ChatId.HasValue)
                {
                    dialog.Photo = lastMessage.Photo50;
                    dialog.Chat = await LoadChat(lastMessage.ChatId.Value);
                    dialog.Users = await LoadUsers(dialog.Chat.Users);
                }
                else
                {
                    dialog.User = await LoadUser(lastMessage.UserId.Value);
                }


                Model.Content.Add(dialog);
            }
        }

        private Task<List<User>> LoadUsers(Collection<long> usersIds)
        {
            return Task.Run(() =>
            {
                List<User> users = new List<User>(usersIds.Count);

                Utils.Extensions.BeginVkInvoke(Vk);
                ReadOnlyCollection<User> usersCollection = Vk.Users.Get(usersIds, ProfileFields.FirstName | ProfileFields.LastName | ProfileFields.Photo50);
                Utils.Extensions.EndVkInvoke();

                users.AddRange(usersCollection);

                return users;
            });
        }

        private Task<User> LoadUser(long userId)
        {
            return Task.Run(() =>
            {
                Utils.Extensions.BeginVkInvoke(Vk);
                User user = Vk.Users.Get(userId, ProfileFields.FirstName | ProfileFields.LastName | ProfileFields.Photo50);
                Utils.Extensions.EndVkInvoke();

                return user;
            });
        }

        private Task<Chat> LoadChat(long chatId)
        {
            return Task.Run(() =>
            {
                Utils.Extensions.BeginVkInvoke(Vk);
                Chat chat = Vk.Messages.GetChat(chatId);
                Utils.Extensions.EndVkInvoke();

                return chat;
            });
        }

        private Task<ReadOnlyCollection<Message>> GetDialogsList()
        {
            return Task.Run(() =>
            {
                List<Message> messages = new List<Message>();
                int offset = 0;

                Utils.Extensions.BeginVkInvoke(Vk);
                MessagesGetObject response = Vk.Messages.GetDialogs(new MessagesDialogsGetParams()
                {
                    PreviewLength = 1,
                    Count = 20
                });
                Utils.Extensions.EndVkInvoke();

                offset += response.Messages.Count;

                messages.AddRange(response.Messages);

                while (offset + response.Messages.Count < response.TotalCount)
                {
                    Utils.Extensions.BeginVkInvoke(Vk);
                    response = Vk.Messages.GetDialogs(new MessagesDialogsGetParams()
                    {
                        Offset = offset,
                        PreviewLength = 1,
                        Count = 20
                    });
                    Utils.Extensions.EndVkInvoke();

                    offset += response.Messages.Count;

                    messages.AddRange(response.Messages);
                }

                return new ReadOnlyCollection<Message>(messages);
            });
        }

        protected virtual void OnPropertyChanged([CallerMemberName]string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
