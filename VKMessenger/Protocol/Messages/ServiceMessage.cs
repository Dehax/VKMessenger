using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace VKMessenger.Protocol.Messages
{
	/// <summary>
	/// Тип служебного сообщения.
	/// </summary>
	public enum ServiceMessageType : byte
	{
		/// <summary>
		/// Запрос публичного ключа.
		/// </summary>
		RequestKey = 1,
		/// <summary>
		/// Ответ на запрос публичного ключа.
		/// </summary>
		ResponseKey = 2,
		/// <summary>
		/// Передача ключа шифрования.
		/// </summary>
		SyncKey = 3,
		/// <summary>
		/// Пользовательское сообщение.
		/// </summary>
		UserMessage = 32,
		/// <summary>
		/// Неизвестный тип сообщения.
		/// </summary>
		Unknown = 100,
		/// <summary>
		/// Сообщение не является служебным.
		/// </summary>
		None = 255
	}

	/// <summary>
	/// Служебное сообщение.
	/// </summary>
	public abstract class ServiceMessage
	{
		protected const int SEED_SIZE = 3;

		public const string HEADER = "=== VKMessenger ===\n";
		private readonly byte[] SEED = new byte[SEED_SIZE];

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
				byte[] resultData = new byte[1 + _deviceId.Length + Data.Length];
				resultData[0] = (byte)Type;
				Buffer.BlockCopy(_deviceId, 0, resultData, 1, _deviceId.Length);
				Buffer.BlockCopy(Data, 0, resultData, 1 + _deviceId.Length, Data.Length);

				return $"{HEADER}{Convert.ToBase64String(SEED)}\n{Convert.ToBase64String(resultData)}";
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

		private byte[] _deviceId;
		public string DeviceId
		{
			get
			{
				if (_deviceId == null)
				{
					return string.Empty;
				}

				return Encoding.ASCII.GetString(_deviceId, 1, _deviceId.Length - 1);
			}
			set
			{
				byte[] deviceIdData = Encoding.ASCII.GetBytes(value);
				_deviceId = new byte[1 + deviceIdData.Length];
				_deviceId[0] = (byte)deviceIdData.Length;
				Buffer.BlockCopy(deviceIdData, 0, _deviceId, 1, deviceIdData.Length);
			}
		}

		/// <summary>
		/// Создаёт новое служебное сообщение с неизвестным типом.
		/// </summary>
		public ServiceMessage()
		{
			using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
			{
				rng.GetBytes(SEED);
			}

			DeviceId = KeysStorage.GetHardDriveSerial();
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

			string seedBase64 = messageBase64.Substring(HEADER.Length, SEED_SIZE * 4 / 3);
			SEED = Convert.FromBase64String(seedBase64);
			string encodedBase64 = messageBase64.Substring(HEADER.Length + seedBase64.Length + 1);
			byte[] messageData = Convert.FromBase64String(encodedBase64);
			Data = new byte[messageData.Length - messageData[1] - 2];
			_deviceId = new byte[messageData[1] + 1];
			Buffer.BlockCopy(messageData, 1, _deviceId, 0, _deviceId.Length);
			if (Data.Length > 0)
			{
				Buffer.BlockCopy(messageData, 1 + _deviceId.Length, Data, 0, Data.Length);
			}
			Type = (ServiceMessageType)messageData[0];
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

			string seedBase64 = messageBase64.Substring(HEADER.Length, SEED_SIZE * 4 / 3);
			byte[] seed = Convert.FromBase64String(seedBase64);
			string encodedBase64 = messageBase64.Substring(HEADER.Length + seedBase64.Length + 1);
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
