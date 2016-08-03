using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using VKMessenger.Model;
using VkNet;
using VkNet.Enums.Filters;
using VkNet.Model;
using VkNet.Model.RequestParams;

namespace VKMessenger
{
    public class MessageEventArgs : EventArgs
    {
        public Message Message { get; set; }

        public MessageEventArgs(Message message)
        {
            Message = message;
        }
    }

    public class Messenger
    {
        private const string MESSAGE_TITLE = "Отправлено через VKMessenger";

        private VkApi _vk = new VkApi();

        private List<Dialog> _dialogs = new List<Dialog>();

        public List<Dialog> Dialogs { get { return _dialogs; } }

        public event EventHandler<MessageEventArgs> NewMessage;
        public event EventHandler DialogsUpdated;

        private bool _cancelRequest = false;

        public Messenger()
        {
            Authenticate();
        }

        public async void SendMessage(string message, Dialog dialog)
        {
            Task<long> sendMessageTask = Task.Run(() =>
            {
                long id = _vk.Messages.Send(new MessagesSendParams()
                {
                    UserId = dialog.Destination.Id,
                    Message = message
                });

                return id;
            });

            long sentMessageId = await sendMessageTask;

            Task<Message> getMessageTask = Task.Run(() =>
            {
                return _vk.Messages.GetById((ulong)sentMessageId);
            });

            Message messageObject = await getMessageTask;

            dialog.Messages.Add(messageObject);
        }

        public async void Start()
        {
            LoadDialogs();

            await ListenMessagesAsync();
        }

        private Task ListenMessagesAsync()
        {
            return Task.Run(async () =>
            {
                LongPollServerResponse longPoll = _vk.Messages.GetLongPollServer(true, true);

                string longPollUrl = @"https://{0}?act=a_check&key={1}&ts={2}&wait=25";
                ulong ts = longPoll.Ts;

                try
                {
                    while (!_cancelRequest)
                    {
                        HttpWebRequest req = WebRequest.CreateHttp(string.Format(longPollUrl, longPoll.Server, longPoll.Key, ts));
                        HttpWebResponse resp = await Utils.Extensions.GetResponseAsync(req, new CancellationToken(_cancelRequest));
                        string responseText;

                        using (StreamReader stream = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                        {
                            responseText = stream.ReadToEnd();
                        }

                        JObject response = JObject.Parse(responseText);
                        ts = (ulong)response["ts"];
                        JArray updatesArray = (JArray)response["updates"];

                        for (int i = 0; i < updatesArray.Count; i++)
                        {
                            JArray eventArray = (JArray)updatesArray[i];

                            int eventType = (int)eventArray[0];

                            switch (eventType)
                            {
                                case 4:
                                    int messageId = (int)eventArray[1];
                                    int flags = (int)eventArray[2];

                                    if ((flags & 2) == 0)
                                    {
                                        long fromId = (long)eventArray[3];
                                        long timestamp = (long)eventArray[4];
                                        string subject = (string)eventArray[5];
                                        string text = (string)eventArray[6];

                                        Message message = new Message();
                                        message.FromId = fromId;
                                        message.Date = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(timestamp);
                                        message.Title = subject;
                                        message.Body = text;
                                        message.UserId = fromId;

                                        OnNewMessage(message);
                                    }
                                    break;
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    _cancelRequest = false;
                }
            });
        }

        public void Stop()
        {
            _cancelRequest = true;
        }

        protected virtual void OnNewMessage(Message message)
        {
            AddMessageToDialog(message);
            NewMessage?.Invoke(this, new MessageEventArgs(message));
        }

        protected virtual void OnDialogsUpdated()
        {
            DialogsUpdated?.Invoke(this, EventArgs.Empty);
        }

        private Task<ReadOnlyCollection<Message>> GetDialogsList()
        {
            return Task.Run(() =>
            {
                MessagesGetObject response = _vk.Messages.GetDialogs(new MessagesDialogsGetParams()
                {
                    Count = 10
                });

                return response.Messages;
            });
        }

        private async void LoadDialogs()
        {
            ReadOnlyCollection<Message> dialogMessages = await GetDialogsList();

            for (int i = 0; i < dialogMessages.Count; i++)
            {
                Message lastMessage = dialogMessages[i];

                User destinationUser = await Task.Run(() =>
                {
                    Thread.Sleep(1000 / _vk.RequestsPerSecond + 1);
                    return _vk.Users.Get(lastMessage.UserId.Value, ProfileFields.Uid | ProfileFields.FirstName | ProfileFields.LastName | ProfileFields.Photo50);
                });

                Dialog dialog = new Dialog(destinationUser, new List<Message>() { lastMessage });

                _dialogs.Add(dialog);
            }

            OnDialogsUpdated();
        }

        private void AddMessageToDialog(Message message)
        {
            for (int i = 0; i < _dialogs.Count; i++)
            {
                Dialog dialog = _dialogs[i];

                if (dialog.Destination.Id == message.UserId.Value)
                {
                    dialog.AddMessage(message);
                    break;
                }
            }
        }

        private void Authenticate()
        {
            string accessToken = Properties.Settings.Default.AccessToken;

            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _vk.Authorize(accessToken);

                if (!_vk.IsAuthorized)
                {
                    throw new Exception("Неправильный маркер доступа!");
                }
            }
            else
            {
                AuthorizationWindow authWindow = new AuthorizationWindow();
                authWindow.ShowDialog();
                accessToken = authWindow.AccessToken;
                _vk.Authorize(accessToken);

                if (_vk.IsAuthorized)
                {
                    Properties.Settings.Default.AccessToken = accessToken;
                    Properties.Settings.Default.Save();

                    MessageBox.Show("Авторизация прошла успешно!", "Авторизовано");
                }
            }
        }
    }
}
