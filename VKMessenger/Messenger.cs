using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VKMessenger.Model;
using VKMessenger.Protocol;
using VKMessenger.Protocol.Messages;
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

	/// <summary>
	/// Мессенджер ВКонтакте. Поддерживает End-to-End (сквозное) шифрование.
	/// </summary>
	public class Messenger
	{
		private VkApi _vk = new VkApi();
		public VkApi Vk { get { return _vk; } }

		public static User User { get; set; }

		public event EventHandler<MessageEventArgs> NewMessage;
		public event EventHandler<MessageEventArgs> MessageSent;

		private bool _cancelRequest = false;
		private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

		private bool _encryptionEnabled = true;
		public bool IsEncryptionEnabled { get { return _encryptionEnabled; } }

		private IEndToEndProtocol _dvProto;

		public Messenger()
		{
			_dvProto = new DVProto(Vk);
		}

		public Task/*<long>*/ SendMessage(string message, Dialog dialog)
		{
			Task/*<long>*/ sendMessageTask;

			if (IsEncryptionEnabled)
			{
				sendMessageTask = Task.Run(() =>
				{
					_dvProto.SendMessage(new MessagesSendParams()
					{
						PeerId = dialog.PeerId,
						Message = message
					});
				});
			}
			else
			{
				sendMessageTask = Task.Run(() =>
				{
					Utils.Extensions.BeginVkInvoke(Vk);
					long id = Vk.Messages.Send(new MessagesSendParams()
					{
						PeerId = dialog.PeerId,
						Message = message
					});
					Utils.Extensions.EndVkInvoke();

					//return id;
				});
			}

			return sendMessageTask;
		}

		/// <summary>
		/// Запустить слушатель новых сообщений ассинхронно.
		/// </summary>
		public async void Start()
		{
			_cancellationTokenSource.Dispose();
			_cancellationTokenSource = new CancellationTokenSource();
			await ListenMessagesAsync();
		}

		/// <summary>
		/// Остановить слушатель новых сообщений.
		/// </summary>
		public void Stop()
		{
			_cancellationTokenSource.Cancel(true);
			_cancelRequest = true;
		}

		/// <summary>
		/// Выполнить авторизацию с использованием маркера доступа <paramref name="accessToken"/>.
		/// </summary>
		/// <param name="accessToken"></param>
		/// <returns></returns>
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
			if (IsEncryptionEnabled)
			{
				VkMessage result;
				if (message.Content.FromId.Value != Vk.UserId.Value && _dvProto.TryParseMessage(message, out result))
				{
					if (result != null)
					{
						// Расшифрованное сообщение.
						NewMessage?.Invoke(this, new MessageEventArgs(result));
					}

					return;
				}
			}

			if (message.Content.FromId.Value == Vk.UserId.Value)
			{
				MessageSent?.Invoke(this, new MessageEventArgs(message));
			}
			else
			{
				NewMessage?.Invoke(this, new MessageEventArgs(message));
			}
		}

		/// <summary>
		/// Запустить цикл мгновенного получения новых сообщений.
		/// </summary>
		/// <returns></returns>
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
						HttpWebResponse resp = await Utils.Extensions.GetResponseAsync(req, _cancellationTokenSource.Token);
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
									// Новое сообщение
									ulong messageId = (ulong)eventArray[1];
									ulong flags = (ulong)eventArray[2];

									VkMessage message = await Task.Run(() =>
									{
										Utils.Extensions.BeginVkInvoke(Vk);
										VkMessage result = new VkMessage(Vk.Messages.GetById(messageId));
										Utils.Extensions.EndVkInvoke();

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

		/// <summary>
		/// Загружает информацию о текущем пользователе.
		/// </summary>
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
