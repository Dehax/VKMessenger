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
using VKMessenger.View;
using VkNet;
using VkNet.Enums.Filters;
using VkNet.Model;
using VkNet.Model.RequestParams;

namespace VKMessenger
{
    public class MessageEventArgs : EventArgs
    {
        public VkMessage Message { get; set; }

        public MessageEventArgs(VkMessage message)
        {
            Message = message;
        }
    }

    public class Messenger
    {
        private VkApi _vk = new VkApi();
        public VkApi Vk { get { return _vk; } }

        public event EventHandler<MessageEventArgs> NewMessage;

        private bool _cancelRequest = false;

        public Messenger()
        {
        }

        public Task<long> SendMessage(string message, Dialog dialog)
        {
            Task<long> sendMessageTask = Task.Run(() =>
            {
                Utils.Extensions.SleepIfTooManyRequests(Vk);

                long id = Vk.Messages.Send(new MessagesSendParams()
                {
                    PeerId = dialog.PeerId,
                    Message = message
                });

                return id;
            });

            return sendMessageTask;
        }

        public async void Start()
        {
            await ListenMessagesAsync();
        }

        public void Stop()
        {
            _cancelRequest = true;
        }

        public bool Authorize(string accessToken)
        {
            Vk.Authorize(accessToken);

            bool authorized = Vk.IsAuthorized;

            if (authorized)
            {
                LoadUserIdAsync();
            }

            return authorized;
        }

        protected virtual void OnNewMessage(VkMessage message)
        {
            NewMessage?.Invoke(this, new MessageEventArgs(message));
        }

        private Task ListenMessagesAsync()
        {
            return Task.Run(async () =>
            {
                LongPollServerResponse longPoll = Vk.Messages.GetLongPollServer(true, true);

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

                                        VkMessage message = new VkMessage();
                                        message.Content.Id = messageId;
                                        message.Content.FromId = fromId;

                                        if (fromId >= 2000000000)
                                        {
                                            message.Content.ChatId = fromId - 2000000000;
                                        }
                                        else
                                        {
                                            message.Author = await Task.Run(() =>
                                            {
                                                Utils.Extensions.SleepIfTooManyRequests(Vk);

                                                return Vk.Users.Get(fromId);
                                            });
                                        }

                                        message.Content.Date = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(timestamp);
                                        message.Content.Title = subject;
                                        message.Content.Body = text;
                                        message.Content.UserId = fromId;

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

        private async void LoadUserIdAsync()
        {
            Vk.UserId = await Task.Run(() =>
            {
                Utils.Extensions.SleepIfTooManyRequests(Vk);

                return Vk.Users.Get(new long[] { })[0].Id;
            });
        }
    }
}
