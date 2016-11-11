using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
		private const int ENCRYPTION_KEY_SIZE = 32;
		private const int ENCRYPTION_IV_SIZE = 4;
		private const int TIMEOUT = 60 * 1000;

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
		public async Task<long> SendMessageAsync(MessagesSendParams message)
		{
			long userId = message.PeerId.Value;

			_handshakeEvents.Add(userId, new AutoResetEvent(false));

			await ApplyForPublicKeyAsync(message.PeerId.Value);

			if (!_handshakeEvents[userId].WaitOne(TIMEOUT))
			{
				// Не дождались ответа, возвращаем ошибку
				_handshakeEvents.Remove(userId);

				return -1;
			}

			_handshakeEvents.Remove(userId);

			return await EncryptAndSendMessageAsync(message, userId);
		}

		/// <summary>
		/// Запросить публичный ключ для отправки зашифрованных сообщений.
		/// </summary>
		/// <param name="userId">ID пользователя, которому будет отправлен запрос</param>
		private Task ApplyForPublicKeyAsync(long userId)
		{
			return Task.Run(() =>
			{
				RequestKeyMessage requestKeyMessage = new RequestKeyMessage();
				Utils.Extensions.BeginVkInvoke(Vk);
				Vk.Messages.Send(new MessagesSendParams()
				{
					PeerId = userId,
					Message = requestKeyMessage.DataBase64
				});
				Utils.Extensions.EndVkInvoke();
			});
		}

		/// <summary>
		/// Подписать, зашифровать и отправить сообщение.
		/// </summary>
		/// <param name="message">Сообщение для отправки</param>
		/// <param name="userId">ID пользователя-получателя сообщения</param>
		private Task<long> EncryptAndSendMessageAsync(MessagesSendParams message, long userId)
		{
			return Task.Run(() =>
			{
				TextUserMessage textUserMessage = new TextUserMessage(TryGetRSAKey(userId, false), message.Message);
				byte[] key = new byte[ENCRYPTION_KEY_SIZE];
				byte[] iv = new byte[ENCRYPTION_IV_SIZE];
				using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
				{
					rng.GetBytes(key);
					//rng.GetBytes(iv);
				}
				Buffer.BlockCopy(key, 0, iv, 0, iv.Length);
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
		/// <returns></returns>
		public bool TryParseMessage(VkMessage message, out VkMessage result)
		{
			result = null;

			switch (ServiceMessage.CheckServiceMessageType(message.Body))
			{
			case ServiceMessageType.RequestKey:
				{
					GenerateAndSendNewKey(message);
				}
				break;
			case ServiceMessageType.ResponseKey:
				{
					// Получен публичный ключ RSA, сохранить для соответствующего пользователя.
					ResponseKeyMessage responseKeyMessage = new ResponseKeyMessage(message.Body);
					long userId = message.UserId.Value;
					CspParameters csp = new CspParameters()
					{
						KeyContainerName = nameof(VKMessenger) + "_to_" + Convert.ToString(message.UserId.Value),
						Flags = CspProviderFlags.CreateEphemeralKey
					};
					RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048, csp);
					rsa.ImportCspBlob(responseKeyMessage.RSAPublicKey);
					SavePublicKey(rsa);

					_handshakeEvents[userId].Set();
				}
				break;
			case ServiceMessageType.UserMessage:
				{
					switch (UserMessage.CheckUserMessageType(message.Body))
					{
					case UserMessageType.Text:
						{
							try
							{
								TextUserMessage textUserMessage = new TextUserMessage(message.Body, TryGetRSAKey(message.FromId.Value, true));
								result = message;
								result.Body = textUserMessage.Text;
							}
							catch (CryptographicException)
							{
								GenerateAndSendNewKey(message);
							}
						}
						break;
					case UserMessageType.File:
						{
							throw new NotImplementedException();
						}
						break;
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
		private void GenerateAndSendNewKey(VkMessage message)
		{
			// Удалить старый и сгенерировать новый ключ
			long userId = message.UserId.Value;
			CspParameters csp = new CspParameters()
			{
				KeyContainerName = nameof(VKMessenger) + "_from_" + Convert.ToString(userId)
			};
			RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048, csp);
			//rsa.PersistKeyInCsp = false;
			//rsa.Clear();
			//rsa = new RSACryptoServiceProvider(2048, csp);
			//RSAParameters r = rsa.ExportParameters(true);
			//string rs = rsa.ToXmlString(true);
			ResponseKeyMessage responseKeyMessage = new ResponseKeyMessage(rsa.ExportCspBlob(false));
			Utils.Extensions.BeginVkInvoke(Vk);
			Vk.Messages.Send(new MessagesSendParams()
			{
				PeerId = message.UserId.Value,
				Message = responseKeyMessage.DataBase64
			});
			Utils.Extensions.EndVkInvoke();
		}

		private RSACryptoServiceProvider TryGetRSAKey(long userId, bool from)
		{
			string containerName;

			if (from)
			{
				containerName = nameof(VKMessenger) + "_from_" + Convert.ToString(userId);
			}
			else
			{
				containerName = nameof(VKMessenger) + "_to_" + Convert.ToString(userId);

				return GetPublicKey(containerName);
			}

			CspParameters csp = new CspParameters()
			{
				KeyContainerName = containerName,
				Flags = CspProviderFlags.UseExistingKey
			};

			RSACryptoServiceProvider rsa;

			try
			{
				rsa = new RSACryptoServiceProvider(2048, csp);
			}
			catch (Exception)
			{
				rsa = null;
			}

			return rsa;
		}

		/// <summary>
		/// Сохранить публичный ключ RSA.
		/// </summary>
		/// <param name="rsaPublicKey">Публичный ключ RSA.</param>
		private void SavePublicKey(RSACryptoServiceProvider rsaPublicKey)
		{
			string publicKeyXml = rsaPublicKey.ToXmlString(false);

			StringBuilder sb = new StringBuilder(Utils.Extensions.ApplicationFolderPath);
			sb.Append(Path.DirectorySeparatorChar);
			sb.Append("PublicKeys");
			Directory.CreateDirectory(sb.ToString());
			sb.Append(Path.DirectorySeparatorChar);
			sb.Append(rsaPublicKey.CspKeyContainerInfo.KeyContainerName + ".xml");
			File.WriteAllText(sb.ToString(), publicKeyXml);
		}

		/// <summary>
		/// Получить публичный ключ RSA.
		/// </summary>
		/// <param name="containerName">Имя ключа, который необходимо получить.</param>
		/// <returns>Публичный ключ RSA</returns>
		private RSACryptoServiceProvider GetPublicKey(string containerName)
		{
			StringBuilder sb = new StringBuilder(Utils.Extensions.ApplicationFolderPath);
			sb.Append(Path.DirectorySeparatorChar);
			sb.Append("PublicKeys");
			sb.Append(Path.DirectorySeparatorChar);
			sb.Append(containerName + ".xml");

			
			RSACryptoServiceProvider rsaPublicKey = new RSACryptoServiceProvider();

			try
			{
				string publicKeyXml = File.ReadAllText(sb.ToString());
				rsaPublicKey.FromXmlString(publicKeyXml);
			}
			catch (Exception ex) when (ex is FileNotFoundException || ex is DirectoryNotFoundException)
			{
				rsaPublicKey.Dispose();
				rsaPublicKey = null;
			}

			return rsaPublicKey;
		}
	}
}
