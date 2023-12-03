using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace UpdateViewer.Command
{
    public class ObservableObject : INotifyPropertyChanged
    {
        private bool _isAuthorizedUser = false;

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ObservableObject()
        {
            // For standard modeling.
            App.Print("Inside ObservableObject Contructor");
        }

        public bool IsAuthorizedUser
        {
            get { return _isAuthorizedUser; }
            set
            {
                if (_isAuthorizedUser != value)
                {
                    _isAuthorizedUser = value;
                    OnPropertyChanged();
                }
            }
        }

    }
}
