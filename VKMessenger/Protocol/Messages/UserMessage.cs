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
	public enum UserMessageType : byte
	{
		Text = 1,
		File = 2,
		Unknown = 100,
		None = 127
	}

	public abstract class UserMessage : ServiceMessage
	{
		private const int ENCRYPTED_KEY_SIZE = 256;
		private const int SIGNATURE_SIZE = 40;
		private const int PUBLIC_KEY_SIZE = 444;
		private const int ENCRYPTION_KEY_SIZE = 32;

		private UserMessageType _userMessageType = UserMessageType.Text;
		public UserMessageType UserMessageType
		{
			get { return _userMessageType; }
			protected set { _userMessageType = value; }
		}

		//private byte[] _encryptedKey = new byte[ENCRYPTED_KEY_SIZE];
		//public byte[] EncryptedKey
		//{
		//	get { return _encryptedKey; }
		//}

		private byte[] _userMessageData;
		protected byte[] UserMessageData
		{
			get { return _userMessageData; }
			set { _userMessageData = value; }
		}

		//private byte[] _encryptedSignedMessageData;

		private RSACryptoServiceProvider _rsaPublicKey;
		//protected RSACryptoServiceProvider RSAPublicKey
		//{
		//	get { return _rsaPublicKey; }
		//	set { _rsaPublicKey = value; }
		//}

		/// <summary>
		/// Создаёт, подписывает и зашифровывает пользовательское сообщение.
		/// </summary>
		/// <param name="rsaPublicKey">Публичный ключ RSA</param>
		public UserMessage(RSACryptoServiceProvider rsaPublicKey)
			: base()
		{
			Type = ServiceMessageType.UserMessage;
			//byte[] oldData = Data;
			//Data = new byte[oldData.Length + 1];
			//Buffer.BlockCopy(oldData, 0, Data, 0, oldData.Length);
			_rsaPublicKey = rsaPublicKey;
		}

		/// <summary>
		/// Разбирает, расшифровывает и проверяет подпись пользовательского сообщения.
		/// </summary>
		/// <param name="messageBase64">Исходное служебное сообщение в Base64</param>
		public UserMessage(string messageBase64, RSACryptoServiceProvider rsaPrivateKey)
			: base(messageBase64)
		{
			byte code = Data[0];

			if (code < (byte)UserMessageType.Text || code > (byte)UserMessageType.File)
			{
				throw new ArgumentOutOfRangeException(nameof(code), "Неверный код пользовательского сообщения");
			}

			UserMessageType = (UserMessageType)code;

			byte[] encryptedKey = new byte[ENCRYPTED_KEY_SIZE];
			byte[] encryptedContent = new byte[Data.Length - ENCRYPTED_KEY_SIZE - 1];
			Buffer.BlockCopy(Data, 1, encryptedKey, 0, ENCRYPTED_KEY_SIZE);
			Buffer.BlockCopy(Data, 1 + ENCRYPTED_KEY_SIZE, encryptedContent, 0, encryptedContent.Length);
			byte[] key = rsaPrivateKey.Decrypt(encryptedKey, true);
			byte[] signedContent = DecryptData(encryptedContent, key);
			byte[] content;

			if (!CheckSignature(signedContent, out content))
			{
				throw new ArgumentException("Нарушена целостность сообщения! Ошибка проверки подписи.");
			}
			
			UserMessageData = content;
			//Buffer.BlockCopy(Data, ENCRYPTED_KEY_SIZE + 2, _userMessageData, 0, messageLength);
		}

		/// <summary>
		/// Подписывает и зашифровывает данные пользовательского сообщения.
		/// </summary>
		public void Encrypt()
		{
			// Создать подпись, добавить в пользовательские данные
			byte[] signedData = SignData(UserMessageData);
			// Сгенерировать Rijndael-ключ
			byte[] key = new byte[ENCRYPTION_KEY_SIZE];
			RandomNumberGenerator.Create().GetBytes(key);
			// Зашифровать данные ключом
			byte[] encryptedData = EncryptData(signedData, key);
			// Зашифровать ключ
			byte[] encryptedKey = _rsaPublicKey.Encrypt(key, true);
			// Сохранить зашифрованный ключ + записать в данные
			Data = new byte[1 + encryptedKey.Length + encryptedData.Length];
			Data[0] = (byte)UserMessageType;
			Buffer.BlockCopy(encryptedKey, 0, Data, 1, encryptedKey.Length);
			Buffer.BlockCopy(encryptedData, 0, Data, 1 + encryptedKey.Length, encryptedData.Length);
		}

		/// <summary>
		/// Подписывает пользовательские данные и добавляет подпись в начало.
		/// </summary>
		/// <param name="data">Данные, которые необходимо подписать</param>
		/// <param name="hashAlgorithm">Алгоритм хеширования для подписи</param>
		/// <returns>Подписанные данные</returns>
		private byte[] SignData(byte[] data)
		{
			DSACryptoServiceProvider dsa = new DSACryptoServiceProvider();
			byte[] signature = dsa.SignData(data);
			byte[] publicKey = dsa.ExportCspBlob(false);
			byte[] resultData = new byte[signature.Length + publicKey.Length + data.Length];
			Buffer.BlockCopy(signature, 0, resultData, 0, signature.Length);
			Buffer.BlockCopy(publicKey, 0, resultData, signature.Length, publicKey.Length);
			Buffer.BlockCopy(data, 0, resultData, signature.Length + publicKey.Length, data.Length);
			dsa.Dispose();

			return resultData;
		}

		/// <summary>
		/// Проверяет подпись пользовательских данных.
		/// </summary>
		/// <param name="signedData">Подписанные данные</param>
		/// <param name="data">Извлечённые данные без подписи</param>
		/// <returns>Результат проверки подписи</returns>
		private bool CheckSignature(byte[] signedData, out byte[] data)
		{
			bool signatureValid = false;

			byte[] signature = new byte[SIGNATURE_SIZE];
			byte[] publicKey = new byte[PUBLIC_KEY_SIZE];
			byte[] content = new byte[signedData.Length - SIGNATURE_SIZE - PUBLIC_KEY_SIZE];
			data = content;
			Buffer.BlockCopy(signedData, 0, signature, 0, signature.Length);
			Buffer.BlockCopy(signedData, signature.Length, publicKey, 0, publicKey.Length);
			Buffer.BlockCopy(signedData, signature.Length + publicKey.Length, content, 0, content.Length);

			DSACryptoServiceProvider dsa = new DSACryptoServiceProvider();
			dsa.ImportCspBlob(publicKey);
			signatureValid = dsa.VerifyData(signedData, signature);
			dsa.Dispose();

			return signatureValid;
		}

		/// <summary>
		/// Зашифровывает данные указанным ключом с использованием алгоритма Rijndael.
		/// </summary>
		/// <param name="data">Данные для шифрования</param>
		/// <param name="key">Ключ Rijndael</param>
		/// <returns></returns>
		private byte[] EncryptData(byte[] data, byte[] key)
		{
			MemoryStream ms = new MemoryStream();
			Rijndael alg = Rijndael.Create();
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
		/// <returns></returns>
		private byte[] DecryptData(byte[] data, byte[] key)
		{
			MemoryStream ms = new MemoryStream();
			Rijndael alg = Rijndael.Create();
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

			string encodedBase64 = messageBase64.Substring(HEADER.Length);
			byte[] data = Convert.FromBase64String(encodedBase64);
			byte code = data[1];

			if (code < (byte)UserMessageType.Text || code > (byte)UserMessageType.File)
			{
				return UserMessageType.Unknown;
			}

			return (UserMessageType)code;
		}
	}
}
