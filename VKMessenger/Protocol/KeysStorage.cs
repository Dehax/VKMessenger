using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace VKMessenger.Protocol
{
	public static class KeysStorage
	{
		private const string ENCRYPTION_KEYS_FOLDER = "Keys";
		private const string PUBLIC_KEYS_FOLDER = "PublicKeys";

		public static bool FindEncryptionKey(bool from, long userId, string deviceId)
		{
			StringBuilder keyPathSb = new StringBuilder(Utils.Extensions.ApplicationFolderPath);
			keyPathSb.Append(Path.DirectorySeparatorChar);
			keyPathSb.Append(ENCRYPTION_KEYS_FOLDER);
			Directory.CreateDirectory(keyPathSb.ToString());
			keyPathSb.Append(Path.DirectorySeparatorChar);
			keyPathSb.Append(GetKeyContainerName(from, userId, deviceId));
			keyPathSb.Append(".xml");

			return File.Exists(keyPathSb.ToString());
		}

		public static void GetEncryptionKey(bool from, long userId, string deviceId, out byte[] key, out byte[] iv)
		{
			StringBuilder keyPathSb = new StringBuilder(Utils.Extensions.ApplicationFolderPath);
			keyPathSb.Append(Path.DirectorySeparatorChar);
			keyPathSb.Append(ENCRYPTION_KEYS_FOLDER);
			Directory.CreateDirectory(keyPathSb.ToString());
			keyPathSb.Append(Path.DirectorySeparatorChar);
			keyPathSb.Append(GetKeyContainerName(from, userId, deviceId));
			keyPathSb.Append(".xml");

			byte[] encryptionKeyData = File.ReadAllBytes(keyPathSb.ToString());
			key = new byte[DVProto.ENCRYPTION_KEY_SIZE];
			iv = new byte[DVProto.ENCRYPTION_IV_SIZE];
			Buffer.BlockCopy(encryptionKeyData, 0, key, 0, DVProto.ENCRYPTION_KEY_SIZE);
			Buffer.BlockCopy(encryptionKeyData, DVProto.ENCRYPTION_KEY_SIZE, iv, 0, DVProto.ENCRYPTION_IV_SIZE);
		}

		public static void SaveEncryptionKey(bool from, long userId, string deviceId, byte[] key, byte[] iv)
		{
			StringBuilder keyPathSb = new StringBuilder(Utils.Extensions.ApplicationFolderPath);
			keyPathSb.Append(Path.DirectorySeparatorChar);
			keyPathSb.Append(ENCRYPTION_KEYS_FOLDER);
			Directory.CreateDirectory(keyPathSb.ToString());
			keyPathSb.Append(Path.DirectorySeparatorChar);
			keyPathSb.Append(GetKeyContainerName(from, userId, deviceId));
			keyPathSb.Append(".xml");

			byte[] encryptionKeyData = new byte[key.Length + iv.Length];
			Buffer.BlockCopy(key, 0, encryptionKeyData, 0, key.Length);
			Buffer.BlockCopy(iv, 0, encryptionKeyData, key.Length, iv.Length);

			File.WriteAllBytes(keyPathSb.ToString(), encryptionKeyData);
		}

		public static RSACryptoServiceProvider TryGetRSAKey(bool from, long userId, string deviceId)
		{
			string containerName = GetKeyContainerName(from, userId, deviceId);

			if (!from)
			{
				return GetPublicKey(userId, deviceId);
			}

			CspParameters csp = new CspParameters()
			{
				KeyContainerName = containerName,
				Flags = CspProviderFlags.UseExistingKey
			};

			RSACryptoServiceProvider rsa;

			try
			{
				rsa = new RSACryptoServiceProvider(2048, csp);
			}
			catch (Exception)
			{
				rsa = null;
			}

			return rsa;
		}

		/// <summary>
		/// Сохранить публичный ключ RSA.
		/// </summary>
		/// <param name="rsaPublicKey">Публичный ключ RSA.</param>
		public static void SavePublicKey(RSACryptoServiceProvider rsaPublicKey, long userId, string deviceId)
		{
			string publicKeyXml = rsaPublicKey.ToXmlString(false);

			StringBuilder keyPathSb = new StringBuilder(Utils.Extensions.ApplicationFolderPath);
			keyPathSb.Append(Path.DirectorySeparatorChar);
			keyPathSb.Append(PUBLIC_KEYS_FOLDER);
			Directory.CreateDirectory(keyPathSb.ToString());
			keyPathSb.Append(Path.DirectorySeparatorChar);
			keyPathSb.Append(GetKeyContainerName(false, userId, deviceId));
			keyPathSb.Append(".xml");

			File.WriteAllText(keyPathSb.ToString(), publicKeyXml);
		}

		public static bool FindPublicKey(bool from, long userId, string deviceId)
		{
			StringBuilder keyPathSb = new StringBuilder(Utils.Extensions.ApplicationFolderPath);
			keyPathSb.Append(Path.DirectorySeparatorChar);
			keyPathSb.Append(PUBLIC_KEYS_FOLDER);
			Directory.CreateDirectory(keyPathSb.ToString());
			keyPathSb.Append(Path.DirectorySeparatorChar);
			keyPathSb.Append(GetKeyContainerName(from, userId, deviceId));
			keyPathSb.Append(".xml");

			return File.Exists(keyPathSb.ToString());
		}

		/// <summary>
		/// Получить публичный ключ RSA.
		/// </summary>
		/// <param name="containerName">Имя ключа, который необходимо получить.</param>
		/// <returns>Публичный ключ RSA</returns>
		public static RSACryptoServiceProvider GetPublicKey(long userId, string deviceId)
		{
			StringBuilder keyPathSb = new StringBuilder(Utils.Extensions.ApplicationFolderPath);
			keyPathSb.Append(Path.DirectorySeparatorChar);
			keyPathSb.Append(PUBLIC_KEYS_FOLDER);
			Directory.CreateDirectory(keyPathSb.ToString());
			keyPathSb.Append(Path.DirectorySeparatorChar);
			keyPathSb.Append(GetKeyContainerName(false, userId, deviceId));
			keyPathSb.Append(".xml");

			RSACryptoServiceProvider rsaPublicKey = new RSACryptoServiceProvider();

			try
			{
				string publicKeyXml = File.ReadAllText(keyPathSb.ToString());
				rsaPublicKey.FromXmlString(publicKeyXml);
			}
			catch (Exception ex) when (ex is FileNotFoundException || ex is DirectoryNotFoundException)
			{
				rsaPublicKey.Dispose();
				rsaPublicKey = null;
			}

			return rsaPublicKey;
		}

		/// <summary>
		/// Возвращает имя контейнера ключа.
		/// </summary>
		/// <param name="from">Показывает, что ключ используется для получения сообщений.</param>
		/// <param name="userId">ID пользователя.</param>
		/// <param name="deviceId">ID устройства пользователя.</param>
		/// <returns></returns>
		public static string GetKeyContainerName(bool from, long userId, string deviceId)
		{
			return $"{nameof(VKMessenger)}{(from ? "_from_" : "_to_")}{Convert.ToString(userId)}-{Convert.ToString(deviceId)}";
		}

		public static void SaveLastDeviceId(long userId, string deviceId)
		{
			StringBuilder deviceIdPathSb = new StringBuilder(Utils.Extensions.ApplicationFolderPath);
			deviceIdPathSb.Append(Path.DirectorySeparatorChar);
			deviceIdPathSb.Append($"DeviceID-{userId}.xml");

			File.WriteAllText(deviceIdPathSb.ToString(), deviceId, Encoding.ASCII);
		}

		public static string GetLastDeviceId(long userId)
		{
			StringBuilder deviceIdPathSb = new StringBuilder(Utils.Extensions.ApplicationFolderPath);
			deviceIdPathSb.Append(Path.DirectorySeparatorChar);
			deviceIdPathSb.Append($"DeviceID-{userId}.xml");

			if (!File.Exists(deviceIdPathSb.ToString()))
			{
				return null;
			}

			return File.ReadAllText(deviceIdPathSb.ToString(), Encoding.ASCII);
		}

		/// <summary>
		/// Возвращает серийный номер первого жёсткого диска.
		/// </summary>
		/// <returns>Серийный номер жёсткого диска.</returns>
		public static string GetHardDriveSerial()
		{
			ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMedia");

			string serial = null;
			foreach (ManagementObject wmiHardDrive in searcher.Get())
			{
				serial = wmiHardDrive["SerialNumber"].ToString();
				break;
			}

			return serial;
		}
	}
}
