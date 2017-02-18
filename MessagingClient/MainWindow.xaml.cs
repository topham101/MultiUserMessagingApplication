using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace MessagingClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TcpClient Client = new TcpClient();
                Client.Connect("localhost", 25566);
                if (Client.Connected)
                {
                    textBlock1.Text = "Connected";
                    StreamReader sr = new StreamReader(Client.GetStream());
                    StreamWriter sw = new StreamWriter(Client.GetStream());
                    sw.WriteLine("Message");
                    sw.Flush();
                    string received = sr.ReadToEnd();
                    textBlock1.Text += "\r\n" + received;
                    Client.Close();
                }
            }
            catch (Exception)
            {
                textBlock1.Text = "ERROR";
            }
        }
    }
}
