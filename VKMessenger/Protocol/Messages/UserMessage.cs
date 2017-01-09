using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace VKMessenger.Protocol.Messages
{
	/// <summary>
	/// Тип пользовательского сообщения.
	/// </summary>
	public enum UserMessageType : byte
	{
		Text = 1,
		File = 2,
		Unknown = 100,
		None = 127
	}

	/// <summary>
	/// Пользовательское сообщение.
	/// </summary>
	public abstract class UserMessage : ServiceMessage
	{
		private UserMessageType _userMessageType = UserMessageType.Text;
		public UserMessageType UserMessageType
		{
			get { return _userMessageType; }
			protected set { _userMessageType = value; }
		}

		private byte[] _userMessageData;
		protected byte[] UserMessageData
		{
			get { return _userMessageData; }
			set { _userMessageData = value; }
		}

		/// <summary>
		/// Создаёт пользовательское сообщение.
		/// </summary>
		public UserMessage()
			: base()
		{
			Type = ServiceMessageType.UserMessage;
		}

		/// <summary>
		/// Разбирает пользовательское сообщение.
		/// </summary>
		/// <param name="messageBase64">Исходное служебное сообщение в Base64</param>
		public UserMessage(string messageBase64)
			: base(messageBase64)
		{
			byte code = Data[0];

			if (code < (byte)UserMessageType.Text || code > (byte)UserMessageType.File)
			{
				throw new ArgumentOutOfRangeException(nameof(code), "Неверный код пользовательского сообщения");
			}

			UserMessageType = (UserMessageType)code;
		}

		public void Decrypt(byte[] key, byte[] iv)
		{
			byte[] encryptedContent = new byte[Data.Length - 1];
			Buffer.BlockCopy(Data, 1, encryptedContent, 0, encryptedContent.Length);
			byte[] content = DecryptData(encryptedContent, key, iv);

			UserMessageData = content;
		}

		/// <summary>
		/// Подписывает и зашифровывает данные пользовательского сообщения.
		/// </summary>
		public void Encrypt(byte[] key, byte[] iv)
		{
			byte[] encryptedData = EncryptData(UserMessageData, key, iv);
			Data = new byte[1 + encryptedData.Length];
			Data[0] = (byte)UserMessageType;
			Buffer.BlockCopy(encryptedData, 0, Data, 1, encryptedData.Length);
		}

		/// <summary>
		/// Зашифровывает данные указанным ключом с использованием алгоритма Rijndael.
		/// </summary>
		/// <param name="data">Данные для шифрования</param>
		/// <param name="key">Ключ Rijndael</param>
		/// <param name="iv">Вектор инициализации Rijndael</param>
		/// <returns></returns>
		private byte[] EncryptData(byte[] data, byte[] key, byte[] iv)
		{
			MemoryStream ms = new MemoryStream();
			Rijndael alg = Rijndael.Create();
			alg.IV = iv;
			alg.Key = key;
			CryptoStream cs = new CryptoStream(ms, alg.CreateEncryptor(), CryptoStreamMode.Write);
			cs.Write(data, 0, data.Length);
			cs.Close();

			return ms.ToArray();
		}

		/// <summary>
		/// Расшифровывает данные указанным ключом с использованием алгоритма Rijndael.
		/// </summary>
		/// <param name="data">Данные для расшифрования</param>
		/// <param name="key">Ключ Rijndael</param>
		/// <param name="iv">Вектор инициализации Rijndael</param>
		/// <returns></returns>
		private byte[] DecryptData(byte[] data, byte[] key, byte[] iv)
		{
			MemoryStream ms = new MemoryStream();
			Rijndael alg = Rijndael.Create();
			alg.IV = iv;
			alg.Key = key;
			CryptoStream cs = new CryptoStream(ms, alg.CreateDecryptor(), CryptoStreamMode.Write);
			cs.Write(data, 0, data.Length);
			cs.Close();

			return ms.ToArray();
		}

		/// <summary>
		/// Проверяет тип пользовательского сообщения.
		/// </summary>
		/// <param name="messageBase64">Пользовательское сообщение в формате Base64</param>
		/// <returns>Тип пользовательского сообщени</returns>
		public static UserMessageType CheckUserMessageType(string messageBase64)
		{
			if (messageBase64.Length <= HEADER.Length || !messageBase64.StartsWith(HEADER))
			{
				return UserMessageType.None;
			}

			string seedBase64 = messageBase64.Substring(HEADER.Length, SEED_SIZE * 4 / 3);
			byte[] seed = Convert.FromBase64String(seedBase64);
			string encodedBase64 = messageBase64.Substring(HEADER.Length + seedBase64.Length + 1);
			byte[] data = Convert.FromBase64String(encodedBase64);
			byte code = data[data[1] + 2];

			if (code < (byte)UserMessageType.Text || code > (byte)UserMessageType.File)
			{
				return UserMessageType.Unknown;
			}

			return (UserMessageType)code;
		}
	}
}
