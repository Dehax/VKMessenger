using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VKMessenger.Model;
using VkNet.Model.RequestParams;

namespace VKMessenger.Protocol
{
	interface IEndToEndProtocol
	{
		void SendMessage(MessagesSendParams message);
		bool TryParseMessage(VkMessage message, out VkMessage result);
	}
}
