using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace VKMessenger.Protocol.Messages
{
	/// <summary>
	/// Сообщение с симметричным ключом шифрования.
	/// </summary>
	public class SyncKeyMessage : ServiceMessage
	{
		public byte[] Key { get; set; }
		public byte[] IV { get; set; }

		/// <summary>
		/// Создание служебного сообщения, содержащего зашифрованный симметричный ключ шифрования.
		/// </summary>
		/// <param name="key">Симметричный ключ шифрования.</param>
		/// <param name="iv">Вектор инициализации.</param>
		/// <param name="rsaPublicKey">Публичный ключ RSA для шифрования симметричного ключа.</param>
		public SyncKeyMessage(byte[] key, byte[] iv, RSACryptoServiceProvider rsaPublicKey)
			: base()
		{
			Type = ServiceMessageType.SyncKey;
			byte[] symmKey = new byte[key.Length + iv.Length];
			Buffer.BlockCopy(key, 0, symmKey, 0, key.Length);
			Buffer.BlockCopy(iv, 0, symmKey, key.Length, iv.Length);
			byte[] encryptedSymmKey = rsaPublicKey.Encrypt(symmKey, true);
			Data = encryptedSymmKey;
		}

		/// <summary>
		/// Разобрать служебное сообщение, содержащее зашифрованный симметричный ключ шифрования.
		/// </summary>
		/// <param name="messageBase64">Текст сообщения, закодированный в Base64.</param>
		/// <param name="rsaPrivateKey">Приватный ключ RSA для расшифровки симметричного ключа.</param>
		public SyncKeyMessage(string messageBase64)
			: base(messageBase64)
		{
		}

		public void Decrypt(RSACryptoServiceProvider rsaPrivateKey)
		{
			byte[] symmKey = rsaPrivateKey.Decrypt(Data, true);
			Key = new byte[DVProto.ENCRYPTION_KEY_SIZE];
			IV = new byte[DVProto.ENCRYPTION_IV_SIZE];
			Buffer.BlockCopy(symmKey, 0, Key, 0, DVProto.ENCRYPTION_KEY_SIZE);
			Buffer.BlockCopy(symmKey, DVProto.ENCRYPTION_KEY_SIZE, IV, 0, DVProto.ENCRYPTION_IV_SIZE);
		}
	}
}
