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
		private const int SIGNATURE_SIZE = 40;
		private const int PUBLIC_KEY_SIZE = 444;

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
			byte[] signedSymmKey = SignData(symmKey);
			byte[] encryptedSymmKey = rsaPublicKey.Encrypt(symmKey, true);
			byte[] encryptedAndSignedSymmKey = new byte[signedSymmKey.Length + encryptedSymmKey.Length];
			Buffer.BlockCopy(signedSymmKey, 0, encryptedAndSignedSymmKey, 0, signedSymmKey.Length);
			Buffer.BlockCopy(encryptedSymmKey, 0, encryptedAndSignedSymmKey, signedSymmKey.Length, encryptedSymmKey.Length);
			Data = encryptedAndSignedSymmKey;
		}

		/// <summary>
		/// Разобрать служебное сообщение, содержащее зашифрованный симметричный ключ шифрования.
		/// </summary>
		/// <param name="messageBase64">Текст сообщения, закодированный в Base64.</param>
		public SyncKeyMessage(string messageBase64)
			: base(messageBase64)
		{
		}

		public void Decrypt(RSACryptoServiceProvider rsaPrivateKey)
		{
			byte[] signedSymmKey = new byte[SIGNATURE_SIZE + PUBLIC_KEY_SIZE];
			byte[] encryptedSymmKey = new byte[Data.Length - signedSymmKey.Length];
			Buffer.BlockCopy(Data, 0, signedSymmKey, 0, SIGNATURE_SIZE);
			Buffer.BlockCopy(Data, SIGNATURE_SIZE, signedSymmKey, SIGNATURE_SIZE, PUBLIC_KEY_SIZE);
			Buffer.BlockCopy(Data, SIGNATURE_SIZE + PUBLIC_KEY_SIZE, encryptedSymmKey, 0, encryptedSymmKey.Length);
			byte[] symmKey = rsaPrivateKey.Decrypt(encryptedSymmKey, true);

			if (!CheckSignature(signedSymmKey, symmKey))
			{
				throw new Exception("Подпись не совпадает!");
			}

			Key = new byte[DVProto.ENCRYPTION_KEY_SIZE];
			IV = new byte[DVProto.ENCRYPTION_IV_SIZE];
			Buffer.BlockCopy(symmKey, 0, Key, 0, DVProto.ENCRYPTION_KEY_SIZE);
			Buffer.BlockCopy(symmKey, DVProto.ENCRYPTION_KEY_SIZE, IV, 0, DVProto.ENCRYPTION_IV_SIZE);
		}

		/// <summary>
		/// Подписывает данные ключа.
		/// </summary>
		/// <param name="data">Данные, которые необходимо подписать</param>
		/// <returns>Подпись DSA(SHA-1)</returns>
		private byte[] SignData(byte[] data)
		{
			DSACryptoServiceProvider dsa = new DSACryptoServiceProvider();
			byte[] signature = dsa.SignData(data);
			byte[] publicKey = dsa.ExportCspBlob(false);
			byte[] resultData = new byte[signature.Length + publicKey.Length];
			Buffer.BlockCopy(signature, 0, resultData, 0, signature.Length);
			Buffer.BlockCopy(publicKey, 0, resultData, signature.Length, publicKey.Length);
			dsa.Dispose();

			return resultData;
		}

		/// <summary>
		/// Проверяет подпись пользовательских данных.
		/// </summary>
		/// <param name="signedData">Подпись</param>
		/// <param name="data">Проверяемые данные</param>
		/// <returns>Результат проверки подписи</returns>
		private bool CheckSignature(byte[] signedData, byte[] data)
		{
			bool signatureValid = false;

			byte[] signature = new byte[SIGNATURE_SIZE];
			byte[] publicKey = new byte[PUBLIC_KEY_SIZE];
			Buffer.BlockCopy(signedData, 0, signature, 0, signature.Length);
			Buffer.BlockCopy(signedData, signature.Length, publicKey, 0, publicKey.Length);

			DSACryptoServiceProvider dsa = new DSACryptoServiceProvider();
			dsa.ImportCspBlob(publicKey);
			signatureValid = dsa.VerifyData(data, signature);
			dsa.Dispose();

			return signatureValid;
		}
	}
}
