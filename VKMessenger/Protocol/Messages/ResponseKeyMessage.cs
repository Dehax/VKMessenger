using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VKMessenger.Protocol.Messages
{
	/// <summary>
	/// Служебное сообщение ответа на запрос публичного ключа.
	/// </summary>
	public class ResponseKeyMessage : ServiceMessage
	{
		private const int RSA_PUBLIC_KEY_SIZE = 276;

		private byte[] _rsaPublicKey = new byte[RSA_PUBLIC_KEY_SIZE];
		public byte[] RSAPublicKey { get { return _rsaPublicKey; } }

		/// <summary>
		/// Создание служебного сообщения ответа, содержащего публичный ключ RSA.
		/// </summary>
		/// <param name="rsaPublicKey">Публичный ключ RSA размером 276 байт (2208 бит)</param>
		public ResponseKeyMessage(byte[] rsaPublicKey)
			: base()
		{
			Type = ServiceMessageType.ResponseKey;
			//byte[] oldData = Data;
			Data = new byte[RSA_PUBLIC_KEY_SIZE];
			//Buffer.BlockCopy(oldData, 0, Data, 0, oldData.Length);
			Buffer.BlockCopy(rsaPublicKey, 0, Data, 0, RSA_PUBLIC_KEY_SIZE);
		}

		/// <summary>
		/// Разобрать служебное сообщение, содержащее публичный ключ RSA.
		/// </summary>
		/// <param name="messageBase64">Текст сообщения, закодированный в Base64</param>
		public ResponseKeyMessage(string messageBase64)
			: base(messageBase64)
		{
			Buffer.BlockCopy(Data, 0, _rsaPublicKey, 0, RSA_PUBLIC_KEY_SIZE);
		}
	}
}
