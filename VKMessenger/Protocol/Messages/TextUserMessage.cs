using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace VKMessenger.Protocol.Messages
{
	/// <summary>
	/// Пользовательское текстовое сообщение.
	/// </summary>
	public class TextUserMessage : UserMessage
	{
		//private string _text;
		public string Text
		{
			get { return Encoding.UTF8.GetString(UserMessageData); }
			//protected set { _text = value; }
		}

		/// <summary>
		/// Создаёт текстовое пользовательское сообщение.
		/// </summary>
		public TextUserMessage(string message, bool create)
			: base()
		{
			UserMessageType = UserMessageType.Text;
			UserMessageData = Encoding.UTF8.GetBytes(message);
		}

		/// <summary>
		/// Разбирает текстовое пользовательское сообщение.
		/// </summary>
		public TextUserMessage(string messageBase64)
			: base(messageBase64)
		{
		}
	}
}
