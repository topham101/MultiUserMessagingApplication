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
using System.Windows.Navigation;

namespace MessagingClientMVVM
{
    public class LoginViewModel : ObservableObject
    {
        private string _outputString;
        private string _username;

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
        public NavigationService ns { get; set; }


        void ConnectExecute() // AMEND LATER
        {
            try
            {
                CommunicationHandler.Connect(Username, Password, false);
                if (CommunicationHandler.Connected)
                    ns.Navigate(new Uri("Views/MessagingPage.xaml", UriKind.Relative));
            }
            catch (Exception e)
            {
                OutputString = e.Message;
            }
        }
        bool CanConnectExecute() // Add IsConnected return later
        {
            return (!CommunicationHandler.Connected &&
                !string.IsNullOrWhiteSpace(Password) &&
                !string.IsNullOrWhiteSpace(Username));
        }
        public ICommand ConnectCommand { get { return new RelayCommand(ConnectExecute, CanConnectExecute); } }

        void RegisterExecute() // AMEND LATER
        {
            try
            {
                CommunicationHandler.Connect(Username, Password, true);
                if (CommunicationHandler.Connected)
                    ns.Navigate(new Uri("Views/MessagingPage.xaml", UriKind.Relative));
            }
            catch (Exception e)
            {
                OutputString = e.Message;
            }
        }
        bool CanRegisterExecute() // Add IsConnected return later
        {
            return (!CommunicationHandler.Connected &&
                !string.IsNullOrWhiteSpace(Password) &&
                !string.IsNullOrWhiteSpace(Username));
        }
        public ICommand RegisterCommand { get { return new RelayCommand(RegisterExecute, CanRegisterExecute); } }
    }
}
