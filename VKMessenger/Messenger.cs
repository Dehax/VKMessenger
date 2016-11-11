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
using VkNet.Enums;
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

	public class MessageReadEventArgs : EventArgs
	{
		public long MessageId { get; set; }

		public MessageReadEventArgs(long messageId)
		{
			MessageId = messageId;
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

		/// <summary>
		/// Происходит при получении нового сообщения.
		/// </summary>
		public event EventHandler<MessageEventArgs> NewMessage;
		/// <summary>
		/// Происходит при успешном отправлении нового сообщения.
		/// </summary>
		public event EventHandler<MessageEventArgs> MessageSent;
		/// <summary>
		/// Происходит при прочтении сообщения.
		/// </summary>
		public event EventHandler<MessageReadEventArgs> MessageRead;

		private bool _cancelRequest = false;
		private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

		/// <summary>
		/// Показывает, включено ли сквозное шифрование (E2EE).
		/// </summary>
		public bool IsEncryptionEnabled { get { return Properties.Settings.Default.IsEncryptionEnabled; } }

		private IEndToEndProtocol _dvProto;

		public Messenger()
		{
			_dvProto = new DVProto(Vk);
		}

		/// <summary>
		/// Отправить сообщение в указанную беседу.
		/// </summary>
		/// <param name="message">Текст сообщения для отправки.</param>
		/// <param name="conversation">Беседа, в которую будет отправлено сообщение.</param>
		public async Task<long> SendMessage(string message, Conversation conversation)
		{
			Task<long> sendMessageTask;

			if (IsEncryptionEnabled)
			{
				sendMessageTask = Task.Run(() =>
				{
					return _dvProto.SendMessageAsync(new MessagesSendParams()
					{
						PeerId = conversation.PeerId,
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
						PeerId = conversation.PeerId,
						Message = message
					});
					Utils.Extensions.EndVkInvoke();

					return id;
				});
			}

			long messageId = await sendMessageTask;

			if (IsEncryptionEnabled)
			{
				Message msg = await LoadMessageAsync(messageId);
				msg.Body = message;
				OnMessageSent(new VkMessage(msg, conversation));
			}

			return messageId;
		}

		private Task<Message> LoadMessageAsync(long messageId)
		{
			return Task.Run(() =>
			{
				Utils.Extensions.BeginVkInvoke(Vk);
				Message msg = Vk.Messages.GetById((ulong)messageId);
				Utils.Extensions.EndVkInvoke();

				return msg;
			});
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
						try
						{
							ts = (ulong)response["ts"];
						}
						catch (Exception)
						{
							continue;
						}

						JArray updatesArray = (JArray)response["updates"];

						for (int i = 0; i < updatesArray.Count; i++)
						{
							JArray eventArray = (JArray)updatesArray[i];

							int eventType = (int)eventArray[0];

							switch (eventType)
							{
							case 3:
								{
									// Сброс флагов сообщения
									long messageId = (long)eventArray[1];
									long mask = (long)eventArray[2];

									// Сообщение прочитано
									if ((mask & 1) > 0)
									{
										OnMessageRead(messageId);
									}
								}
								break;
							case 4:
								{
									// Новое сообщение
									long messageId = (long)eventArray[1];
									long flags = (long)eventArray[2];

									VkMessage message = new VkMessage(await LoadMessageAsync(messageId));

									message.FromId = (message.Type == MessageType.Received) ? message.UserId : Vk.UserId;

									message.Author = await Task.Run(() =>
									{
										Utils.Extensions.BeginVkInvoke(Vk);
										User user = Vk.Users.Get(message.FromId.Value);
										Utils.Extensions.EndVkInvoke();

										return user;
									});

									switch (message.Type.Value)
									{
									case MessageType.Received:
										// Новое сообщение
										OnNewMessage(message);
										break;
									case MessageType.Sended:
										if (!IsEncryptionEnabled)
										{
											// Успешно отправлено сообщение
											OnMessageSent(message);
										}
										break;
									}
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
		/// <param name="accessToken">Маркер доступа к VK API.</param>
		/// <returns>true при успешной авторизации.</returns>
		public bool Authorize(string accessToken)
		{
			Utils.Extensions.BeginVkInvoke(Vk);
			Vk.Authorize(accessToken);
			Utils.Extensions.EndVkInvoke();

			bool authorized = Vk.IsAuthorized;

			if (authorized)
			{
				LoadUserId();
			}

			return authorized;
		}

		/// <summary>
		/// Получено новое сообщение.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		protected virtual void OnNewMessage(VkMessage message)
		{
			if (IsEncryptionEnabled)
			{
				VkMessage result;
				if (_dvProto.TryParseMessage(message, out result))
				{
					if (result != null)
					{
						// Расшифрованное сообщение.
						NewMessage?.Invoke(this, new MessageEventArgs(result));
					}
				}
				else
				{
					NewMessage?.Invoke(this, new MessageEventArgs(message));
				}
			}
			else
			{
				NewMessage?.Invoke(this, new MessageEventArgs(message));
			}
		}

		/// <summary>
		/// Отправлено новое сообщение.
		/// </summary>
		/// <param name="message">Отправленное сообщение.</param>
		protected virtual void OnMessageSent(VkMessage message)
		{
			MessageSent?.Invoke(this, new MessageEventArgs(message));
		}

		/// <summary>
		/// Сообщение было прочитано.
		/// </summary>
		/// <param name="messageId">ID прочитанного сообщения.</param>
		private void OnMessageRead(long messageId)
		{
			MessageRead?.Invoke(this, new MessageReadEventArgs(messageId));
		}

		/// <summary>
		/// Загружает информацию о текущем пользователе.
		/// </summary>
		private async void LoadUserId()
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
