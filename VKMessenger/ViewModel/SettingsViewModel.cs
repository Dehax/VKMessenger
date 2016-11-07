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
