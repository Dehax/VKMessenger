using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VKMessenger.Model;
using VKMessenger.Protocol.Messages;
using VkNet;
using VkNet.Model.RequestParams;

namespace VKMessenger.Protocol
{
	/// <summary>
	/// Протокол на основе сквозного шифрования (End-to-End encryption, E2EE).
	/// </summary>
	public class DVProto : IEndToEndProtocol
	{
		public const int ENCRYPTION_KEY_SIZE = 32;
		public const int ENCRYPTION_IV_SIZE = 16;
#if DEBUG
		private const int TIMEOUT = 10 * 60 * 1000;
#else
		private const int TIMEOUT = 10 * 1000;
#endif

		//private object _lock = new object();

		private VkApi _vk;
		public VkApi Vk { get { return _vk; } }

		// Список ожидаемых рукопожатий.
		private Dictionary<long, AutoResetEvent> _handshakeEvents = new Dictionary<long, AutoResetEvent>();

		public DVProto(VkApi vk)
		{
			_vk = vk;
		}

		/// <summary>
		/// Оправить новое сообщение с использованием сквозного шифрования (E2EE).
		/// </summary>
		/// <param name="message">Параметры нового сообщения.</param>
		public async Task<long> SendMessageAsync(MessagesSendParams message, string deviceId)
		{
			long userId = message.PeerId.Value;

			if (_handshakeEvents.ContainsKey(userId))
			{
				return -1;
			}

			byte[] key = null;
			byte[] iv = null;

			// Если есть публичный и симметричный ключи, только отправить сообщение.
			// Иначе запросить публичный и отправить симметричный.
			if (string.IsNullOrEmpty(deviceId) || !KeysStorage.FindPublicKey(false, userId, deviceId) || !KeysStorage.FindEncryptionKey(false, userId, deviceId))
			{
				_handshakeEvents.Add(userId, new AutoResetEvent(false));

				await ApplyForPublicKeyAsync(userId);

				if (!_handshakeEvents[userId].WaitOne(TIMEOUT))
				{
					// Не дождались ответа, возвращаем ошибку
					_handshakeEvents.Remove(userId);

					return -1;
				}

				_handshakeEvents.Remove(userId);

				key = new byte[ENCRYPTION_KEY_SIZE];
				iv = new byte[ENCRYPTION_IV_SIZE];
				using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
				{
					rng.GetBytes(key);
					rng.GetBytes(iv);
				}

				if (deviceId == null)
				{
					deviceId = KeysStorage.GetLastDeviceId(userId);
				}

				KeysStorage.SaveEncryptionKey(false, userId, deviceId, key, iv);

				await EncryptAndSendSymmetricKey(KeysStorage.GetPublicKey(userId, deviceId), userId, key, iv);
			}

			if (key == null || iv == null)
			{
				KeysStorage.GetEncryptionKey(false, userId, deviceId, out key, out iv);
			}

			return await EncryptAndSendMessageAsync(message, userId, key, iv);
		}

		/// <summary>
		/// Запросить публичный ключ для отправки зашифрованных сообщений.
		/// </summary>
		/// <param name="userId">ID пользователя, которому будет отправлен запрос</param>
		private Task<long> ApplyForPublicKeyAsync(long userId)
		{
			return Task.Run(() =>
			{
				RequestKeyMessage requestKeyMessage = new RequestKeyMessage();
				Utils.Extensions.BeginVkInvoke(Vk);
				long id = Vk.Messages.Send(new MessagesSendParams()
				{
					PeerId = userId,
					Message = requestKeyMessage.DataBase64
				});
				Utils.Extensions.EndVkInvoke();

				return id;
			});
		}

		private Task<long> EncryptAndSendSymmetricKey(RSACryptoServiceProvider rsaPublicKey, long userId, byte[] key, byte[] iv)
		{
			return Task.Run(() =>
			{
				SyncKeyMessage syncKeyMessage = new SyncKeyMessage(key, iv, rsaPublicKey);
				Utils.Extensions.BeginVkInvoke(Vk);
				long id = Vk.Messages.Send(new MessagesSendParams()
				{
					PeerId = userId,
					Message = syncKeyMessage.DataBase64
				});
				Utils.Extensions.EndVkInvoke();

				return id;
			});
		}

		/// <summary>
		/// Подписать, зашифровать и отправить сообщение.
		/// </summary>
		/// <param name="message">Сообщение для отправки</param>
		/// <param name="userId">ID пользователя-получателя сообщения</param>
		private Task<long> EncryptAndSendMessageAsync(MessagesSendParams message, long userId, byte[] key, byte[] iv)
		{
			return Task.Run(() =>
			{
				TextUserMessage textUserMessage = new TextUserMessage(message.Message, true);
				textUserMessage.Encrypt(key, iv);
				Utils.Extensions.BeginVkInvoke(Vk);
				long id = Vk.Messages.Send(new MessagesSendParams()
				{
					PeerId = message.PeerId,
					Message = textUserMessage.DataBase64
				});
				Utils.Extensions.EndVkInvoke();

				return id;
			});
		}

		/// <summary>
		/// Попытаться разобрать сообщение с использованием протокола DVProto.
		/// </summary>
		/// <param name="message">Сообщение, которое необходимо разобрать.</param>
		/// <param name="result">Результат разбора сообщения.</param>
		/// <returns>Указывает, является ли сообщение служебным.</returns>
		public bool TryParseMessage(VkMessage message, out VkMessage result)
		{
			result = null;

			long userId = message.UserId.Value;

			switch (ServiceMessage.CheckServiceMessageType(message.Body))
			{
			case ServiceMessageType.RequestKey:
				{
					RequestKeyMessage requestKeyMessage = new RequestKeyMessage(message.Body);
					KeysStorage.SaveLastDeviceId(userId, requestKeyMessage.DeviceId);
					GenerateAndSendNewKey(message, requestKeyMessage.DeviceId);
				}
				break;
			case ServiceMessageType.ResponseKey:
				{
					// Получен публичный ключ RSA, сохранить для соответствующего пользователя.
					ResponseKeyMessage responseKeyMessage = new ResponseKeyMessage(message.Body);
					
					KeysStorage.SaveLastDeviceId(userId, responseKeyMessage.DeviceId);
					CspParameters csp = new CspParameters()
					{
						KeyContainerName = KeysStorage.GetKeyContainerName(false, userId, responseKeyMessage.DeviceId),
						Flags = CspProviderFlags.CreateEphemeralKey
					};
					RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048, csp);
					rsa.ImportCspBlob(responseKeyMessage.RSAPublicKey);
					KeysStorage.SavePublicKey(rsa, userId, responseKeyMessage.DeviceId);

					if (_handshakeEvents.ContainsKey(userId))
					{
						_handshakeEvents[userId].Set();
					}
					else
					{
						byte[] key;
						byte[] iv;
						KeysStorage.GetEncryptionKey(false, userId, responseKeyMessage.DeviceId, out key, out iv);
						EncryptAndSendSymmetricKey(rsa, userId, key, iv).Wait();

						throw new Exception("Последнее сообщение не было отправлено! Повторите отправку.");
					}
				}
				break;
			case ServiceMessageType.SyncKey:
				{
					SyncKeyMessage syncKeyMessage = new SyncKeyMessage(message.Body);
					syncKeyMessage.Decrypt(KeysStorage.TryGetRSAKey(true, userId, syncKeyMessage.DeviceId));
					
					KeysStorage.SaveEncryptionKey(true, userId, syncKeyMessage.DeviceId, syncKeyMessage.Key, syncKeyMessage.IV);
				}
				break;
			case ServiceMessageType.UserMessage:
				{
					switch (UserMessage.CheckUserMessageType(message.Body))
					{
					case UserMessageType.Text:
						{
							byte[] key;
							byte[] iv;
							TextUserMessage textUserMessage = new TextUserMessage(message.Body);

							if (!KeysStorage.FindEncryptionKey(true, userId, textUserMessage.DeviceId))
							{
								GenerateAndSendNewKey(message, textUserMessage.DeviceId);
								break;
							}

							KeysStorage.GetEncryptionKey(true, userId, textUserMessage.DeviceId, out key, out iv);
							textUserMessage.Decrypt(key, iv);
							result = message;
							result.DeviceId = textUserMessage.DeviceId;
							result.Body = textUserMessage.Text;

							KeysStorage.SaveLastDeviceId(userId, textUserMessage.DeviceId);
						}
						break;
					case UserMessageType.File:
						{
							throw new NotImplementedException();
						}
						//break;
					}
				}
				break;
			default:
				return false;
			}

			return true;
		}

		/// <summary>
		/// Сгенерировать и отправить новый публичный ключ.
		/// </summary>
		/// <param name="message">Параметры сообщения, содержащие ID пользователя, которому необходимо отправить ключ.</param>
		/// <param name="deviceId">ID устройства.</param>
		private void GenerateAndSendNewKey(VkMessage message, string deviceId)
		{
			// Удалить старый и сгенерировать новый ключ
			long userId = message.UserId.Value;
			CspParameters csp = new CspParameters()
			{
				KeyContainerName = KeysStorage.GetKeyContainerName(true, userId, deviceId)
			};
			RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048, csp);
			ResponseKeyMessage responseKeyMessage = new ResponseKeyMessage(rsa.ExportCspBlob(false));
			Utils.Extensions.BeginVkInvoke(Vk);
			Vk.Messages.Send(new MessagesSendParams()
			{
				PeerId = message.UserId.Value,
				Message = responseKeyMessage.DataBase64
			});
			Utils.Extensions.EndVkInvoke();
		}
	}
}
