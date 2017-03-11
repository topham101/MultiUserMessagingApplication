using MessagingClientMVVM.MicroMVVM;
using MessagingClientMVVM.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MessagingClientMVVM
{
    public class LoginViewModel : ObservableObject
    {
        private string _outputString;
        public string OutputString
        {
            get
            {
                return _outputString;
            }
            set
            {
                _outputString = value;
                RaisePropertyChanged("OutputString");
            }
        }
        private string _username;
        public string Username
        {
            get
            {
                return _username;
            }
            set
            {
                _username = value;
                RaisePropertyChanged("Username");
            }
        }
        public string Password { get; set; }


        void ConnectExecute() // AMEND LATER
        {
            CommunicationHandler.Connect(Username, Password, false);
        }
        bool CanConnectExecute() // Add IsConnected return later
        {
            return !CommunicationHandler.Connected;
        }
        public ICommand ConnectCommand { get { return new RelayCommand(ConnectExecute, CanConnectExecute); } }
    }
}
