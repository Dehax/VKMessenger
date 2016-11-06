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
		private VkApi _vk;
		public VkApi Vk { get { return _vk; } }

		private Dictionary<long, AutoResetEvent> _handshakeEvents = new Dictionary<long, AutoResetEvent>();

		public DVProto(VkApi vk)
		{
			_vk = vk;
		}

		public async void SendMessage(MessagesSendParams message)
		{
			long userId = message.PeerId.Value;

			if (TryGetRSAKey(userId, false) == null)
			{
				_handshakeEvents.Add(userId, new AutoResetEvent(false));

				await ApplyForPublicKeyAsync(message.PeerId.Value);

				_handshakeEvents[userId].WaitOne();

				_handshakeEvents.Remove(userId);
			}

			await EncryptAndSendMessageAsync(message, userId);
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
		private Task EncryptAndSendMessageAsync(MessagesSendParams message, long userId)
		{
			return Task.Run(() =>
			{
				TextUserMessage textUserMessage = new TextUserMessage(TryGetRSAKey(userId, false), message.Message);
				textUserMessage.Encrypt();
				Utils.Extensions.BeginVkInvoke(Vk);
				Vk.Messages.Send(new MessagesSendParams()
				{
					PeerId = message.PeerId,
					Message = textUserMessage.DataBase64
				});
				Utils.Extensions.EndVkInvoke();
			});
		}

		public bool TryParseMessage(VkMessage message, out VkMessage result)
		{
			result = null;

			switch (ServiceMessage.CheckServiceMessageType(message.Content.Body))
			{
			case ServiceMessageType.RequestKey:
				{
					GenerateAndSendNewKey(message);
				}
				break;
			case ServiceMessageType.ResponseKey:
				{
					// Получен публичный ключ RSA, сохранить для соответствующего пользователя.
					ResponseKeyMessage responseKeyMessage = new ResponseKeyMessage(message.Content.Body);
					long userId = message.Content.UserId.Value;
					CspParameters csp = new CspParameters()
					{
						KeyContainerName = nameof(VKMessenger) + "_to_" + Convert.ToString(message.Content.UserId.Value),
						Flags = CspProviderFlags.CreateEphemeralKey
					};
					RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048, csp);
					//rsa.PersistKeyInCsp = false;
					//rsa.Clear();
					//csp.Flags = CspProviderFlags.UseMachineKeyStore;
					//csp.Flags = CspProviderFlags.UseDefaultKeyContainer;
					//rsa = new RSACryptoServiceProvider(2048, csp);
					rsa.ImportCspBlob(responseKeyMessage.RSAPublicKey);
					//RSAParameters r = rsa.ExportParameters(false);
					//string rs = rsa.ToXmlString(false);
					SavePublicKey(rsa);

					_handshakeEvents[userId].Set();
				}
				break;
			case ServiceMessageType.UserMessage:
				{
					switch (UserMessage.CheckUserMessageType(message.Content.Body))
					{
					case UserMessageType.Text:
						{
							try
							{
								TextUserMessage textUserMessage = new TextUserMessage(message.Content.Body, TryGetRSAKey(message.Content.FromId.Value, true));
								result = message;
								result.Content.Body = textUserMessage.Text;
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

		private void GenerateAndSendNewKey(VkMessage message)
		{
			// Удалить старый и сгенерировать новый ключ
			long userId = message.Content.UserId.Value;
			CspParameters csp = new CspParameters()
			{
				KeyContainerName = nameof(VKMessenger) + "_from_" + Convert.ToString(userId)
			};
			RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048, csp);
			rsa.PersistKeyInCsp = false;
			rsa.Clear();
			rsa = new RSACryptoServiceProvider(2048, csp);
			//RSAParameters r = rsa.ExportParameters(true);
			//string rs = rsa.ToXmlString(true);
			ResponseKeyMessage responseKeyMessage = new ResponseKeyMessage(rsa.ExportCspBlob(false));
			Utils.Extensions.BeginVkInvoke(Vk);
			Vk.Messages.Send(new MessagesSendParams()
			{
				PeerId = message.Content.UserId.Value,
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
				//RSAParameters r = rsa.ExportParameters(true);
				//string rs = rsa.ToXmlString(true);
			}
			catch (Exception)
			{
				rsa = null;
			}

			return rsa;
		}

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
			catch (FileNotFoundException)
			{
				rsaPublicKey.Dispose();
				rsaPublicKey = null;
			}

			return rsaPublicKey;
		}
	}
}
