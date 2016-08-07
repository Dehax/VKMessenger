﻿using Newtonsoft.Json.Linq;
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
        public Message Message { get; set; }

        public MessageEventArgs(Message message)
        {
            Message = message;
        }
    }

    public class Messenger
    {
        //private const string MESSAGE_TITLE = "Отправлено через VKMessenger";

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
                Utils.Extensions.SleepIfTooManyRequests(_vk);
                long id = _vk.Messages.Send(new MessagesSendParams()
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
                                        message.Id = messageId;
                                        message.FromId = fromId;

                                        if (fromId >= 2000000000)
                                        {
                                            message.ChatId = fromId - 2000000000;
                                        }

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
            NewMessage?.Invoke(this, new MessageEventArgs(message));
        }

        public bool Authenticate()
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
                }
                else
                {
                    return false;
                }
            }

            return true;
        }
    }
}
