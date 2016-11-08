using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace VKMessenger.Protocol.Messages
{
	/// <summary>
	/// Пользовательское сообщение, передающее файл.
	/// </summary>
	public class FileUserMessage : UserMessage
	{
		private string _fileName;
		public string FileName
		{
			get { return _fileName; }
		}

		private byte[] _fileContent;
		public byte[] FileContent
		{
			get { return _fileContent; }
		}

		public FileUserMessage(RSACryptoServiceProvider rsa, string fileName, byte[] fileContent)
			: base(rsa)
		{
			throw new NotImplementedException();
		}

		//public FileUserMessage(string message)
		//	: base(message)
		//{
		//	byte fileNameLength = UserMessageData[0];
		//	_fileName = Encoding.ASCII.GetString(UserMessageData, 1, fileNameLength);
		//	int fileContentLength = BitConverter.ToInt32(UserMessageData, fileNameLength + 1);
		//	_fileContent = new byte[fileContentLength];
		//	Buffer.BlockCopy(UserMessageData, fileNameLength + 5, _fileContent, 0, fileContentLength);
		//}
	}
}
