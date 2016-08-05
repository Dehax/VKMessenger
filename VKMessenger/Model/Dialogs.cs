using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace VKMessenger.Model
{
    public class Dialogs : INotifyPropertyChanged
    {
        private ObservableCollection<Dialog> _dialogs = new ObservableCollection<Dialog>();
        public ObservableCollection<Dialog> Content
        {
            get { return _dialogs; }
            set
            {
                if (_dialogs != value)
                {
                    _dialogs = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public Dialogs()
        {

        }

        public Dialogs(IEnumerable<Dialog> dialogs)
        {
            _dialogs = new ObservableCollection<Dialog>(dialogs);
        }

        protected virtual void OnPropertyChanged([CallerMemberName]string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
