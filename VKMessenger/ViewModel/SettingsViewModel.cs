using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using VKMessenger.Properties;
using VKMessenger.ViewModel.Commands;

namespace VKMessenger.ViewModel
{
	public class SettingsViewModel : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;
		
		/// <summary>
		/// Показывает, включено ли сквозное шифрование (E2EE).
		/// </summary>
		public bool IsEncryptionEnabled
		{
			get
			{
				return Settings.Default.IsEncryptionEnabled;
			}
			set
			{
				Settings.Default.IsEncryptionEnabled = value;
				OnPropertyChanged();
			}
		}

		/// <summary>
		/// Включены ли уведомления о новых сообщениях.
		/// </summary>
		public bool IsNotificationsEnabled
		{
			get
			{
				return Settings.Default.IsNotificationsEnabled;
			}
			set
			{
				Settings.Default.IsNotificationsEnabled = value;
				OnPropertyChanged();
			}
		}

		/// <summary>
		/// Команда сохранения настроек.
		/// </summary>
		public SimpleCommand SaveSettingsCommand { get; set; }

		public SettingsViewModel()
		{
			SaveSettingsCommand = new SimpleCommand(() => { Settings.Default.Save(); });
		}

		protected virtual void OnPropertyChanged([CallerMemberName]string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
