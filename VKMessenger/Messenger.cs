using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VKMessenger.Model;
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

        public static User User { get; set; }

        public event EventHandler<MessageEventArgs> NewMessage;

        private bool _cancelRequest = false;

        public Messenger()
        {
        }

        public Task<long> SendMessage(string message, Dialog dialog)
        {
            Task<long> sendMessageTask = Task.Run(() =>
            {
                Utils.Extensions.BeginVkInvoke(Vk);
                long id = Vk.Messages.Send(new MessagesSendParams()
                {
                    PeerId = dialog.PeerId,
                    Message = message
                });
                Utils.Extensions.EndVkInvoke();

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
            Utils.Extensions.BeginVkInvoke(Vk);
            Vk.Authorize(accessToken);
            Utils.Extensions.EndVkInvoke();

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
                                    {
                                        ulong messageId = (ulong)eventArray[1];
                                        ulong flags = (ulong)eventArray[2];

                                        VkMessage message = await Task.Run(() =>
                                        {
                                            VkMessage result = new VkMessage(Vk.Messages.GetById(messageId));

                                            return result;
                                        });

                                        message.Content.FromId = ((flags & 2) == 0) ? message.Content.UserId : Vk.UserId;

                                        message.Author = await Task.Run(() =>
                                        {
                                            Utils.Extensions.BeginVkInvoke(Vk);
                                            User user = Vk.Users.Get(message.Content.FromId.Value);
                                            Utils.Extensions.EndVkInvoke();

                                            return user;
                                        });

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
            User = await Task.Run(() =>
            {
                Utils.Extensions.BeginVkInvoke(Vk);
                User user = Vk.Users.Get(new long[] { }, ProfileFields.FirstName | ProfileFields.LastName | ProfileFields.Photo50)[0];
                Utils.Extensions.EndVkInvoke();

                return user;
            });

            Vk.UserId = User.Id;
        }
    }
}
