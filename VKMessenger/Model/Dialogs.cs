using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace VKMessenger.Model
{
    public class Dialogs : INotifyPropertyChanged
    {
        private IList<Dialog> _dialogs = new List<Dialog>();
        public IList<Dialog> Content
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
            foreach (Dialog dialog in dialogs)
            {
                _dialogs.Add(dialog);
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName]string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
