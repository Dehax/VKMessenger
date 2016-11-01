using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VKMessenger.Protocol.Messages
{
	/// <summary>
	/// Служебное сообщение запроса публичного ключа.
	/// </summary>
	public class RequestKeyMessage : ServiceMessage
	{
		public RequestKeyMessage()
			: base()
		{
			Type = ServiceMessageType.RequestKey;
		}

		//public RequestKeyMessage(string message)
		//	: base(message)
		//{
		//}
	}
}
