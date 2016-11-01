﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VKMessenger.Protocol.Messages
{
	/// <summary>
	/// Тип служебного сообщения.
	/// </summary>
	public enum ServiceMessageType : byte
	{
		RequestKey = 1,
		ResponseKey = 2,
		UserMessage = 3,
		Unknown = 100,
		None = 127
	}

	/// <summary>
	/// Служебное сообщение.
	/// </summary>
	public abstract class ServiceMessage
	{
		public const string HEADER = "=== VKMessenger ===\n";

		private byte[] _data = new byte[0];
		/// <summary>
		/// Данные служебного сообщения без заголовка.
		/// </summary>
		public byte[] Data
		{
			get { return _data; }
			protected set { _data = value; }
		}

		/// <summary>
		/// Заголовок и данные служебного сообщения, представленные в кодировке Base64.
		/// </summary>
		public string DataBase64
		{
			get
			{
				byte[] resultData = new byte[1 + Data.Length];
				resultData[0] = (byte)Type;
				Buffer.BlockCopy(Data, 0, resultData, 1, Data.Length);

				return $"{HEADER}{Convert.ToBase64String(resultData)}";
			}
		}

		private ServiceMessageType _type = ServiceMessageType.Unknown;
		/// <summary>
		/// Тип служебного сообщения.
		/// </summary>
		public ServiceMessageType Type
		{
			get { return _type; }
			protected set { _type = value; }
		}

		/// <summary>
		/// Создаёт новое служебное сообщение с неизвестным типом.
		/// </summary>
		public ServiceMessage()
		{
		}

		/// <summary>
		/// Разбирает служебное сообщение.
		/// </summary>
		/// <param name="messageBase64"></param>
		public ServiceMessage(string messageBase64)
		{
			if (CheckServiceMessageType(messageBase64) == ServiceMessageType.None)
			{
				throw new ArgumentException("Сообщение не является служебным", nameof(messageBase64));
			}

			string encodedBase64 = messageBase64.Substring(HEADER.Length);
			byte[] messageData = Convert.FromBase64String(encodedBase64);
			Data = new byte[messageData.Length - 1];
			Buffer.BlockCopy(messageData, 1, Data, 0, Data.Length);
		}

		/// <summary>
		/// Проверить тип служебного сообщения.
		/// </summary>
		/// <param name="messageBase64">Служебное сообщение в формате Base64</param>
		/// <returns>Тип служебного сообщения</returns>
		public static ServiceMessageType CheckServiceMessageType(string messageBase64)
		{
			if (messageBase64.Length <= HEADER.Length || !messageBase64.StartsWith(HEADER))
			{
				return ServiceMessageType.None;
			}

			string encodedBase64 = messageBase64.Substring(HEADER.Length);
			byte[] data = Convert.FromBase64String(encodedBase64);
			byte code = data[0];

			if (code < (byte)ServiceMessageType.RequestKey || code > (byte)ServiceMessageType.UserMessage)
			{
				return ServiceMessageType.Unknown;
			}

			return (ServiceMessageType)code;
		}
	}
}