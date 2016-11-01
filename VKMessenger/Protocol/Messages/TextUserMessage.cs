using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace VKMessenger.Protocol.Messages
{
	public class TextUserMessage : UserMessage
	{
		private string _text;
		public string Text
		{
			get { return _text; }
			protected set { _text = value; }
		}

		/// <summary>
		/// Создаёт текстовое пользовательское сообщение.
		/// </summary>
		/// <param name="rsaPublicKey"></param>
		/// <param name="message"></param>
		public TextUserMessage(RSACryptoServiceProvider rsaPublicKey, string message)
			: base(rsaPublicKey)
		{
			UserMessageType = UserMessageType.Text;
			UserMessageData = Encoding.UTF8.GetBytes(message);
		}

		/// <summary>
		/// Разбирает текстовое пользовательское сообщение.
		/// </summary>
		/// <param name="messageBase64">Содержимое текстового пользовательского сообщения в формате Base64</param>
		/// <param name="rsaPrivateKey">Приватный ключ для расшифрования сообщения</param>
		public TextUserMessage(string messageBase64, RSACryptoServiceProvider rsaPrivateKey)
			: base(messageBase64, rsaPrivateKey)
		{
			Text = Encoding.UTF8.GetString(UserMessageData);
		}
	}
}
