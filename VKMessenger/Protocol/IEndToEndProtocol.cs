using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VKMessenger.Model;
using VkNet.Model.RequestParams;

namespace VKMessenger.Protocol
{
	/// <summary>
	/// Интерфейс протокола, поддерживающего сквозное шифрование сообщений.
	/// </summary>
	interface IEndToEndProtocol
	{
		/// <summary>
		/// Отправить сообщение.
		/// </summary>
		/// <param name="message">Параметры сообщения.</param>
		long SendMessage(MessagesSendParams message);
		/// <summary>
		/// Разобрать сообщение.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		/// <param name="result">Результат расшифровки пользовательского сообщения.</param>
		/// <returns>Успешность разбора сообщения.</returns>
		bool TryParseMessage(VkMessage message, out VkMessage result);
	}
}
