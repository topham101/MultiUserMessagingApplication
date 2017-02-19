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
        private TcpClient Client;
        StreamReader sr;
        StreamWriter sw;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Client = new TcpClient();
                Client.Connect("localhost", 25566);
                if (Client.Connected)
                {
                    textBlock1.Text = "Connected";

                    sr = new StreamReader(Client.GetStream());
                    sw = new StreamWriter(Client.GetStream());
                    string fullInput = "";
                    while (sr.Peek() >= 0)
                    {
                        fullInput += sr.ReadLine() + "\r\n";
                    }
                    textBlock1.Text = fullInput;

                    sw.WriteLine("~~002\r\n\r\n\r\n##");
                    sw.Flush();
                    Client.Close();
                }
            }
            catch (Exception)
            {
                textBlock1.Text = "ERROR";
            }
            finally
            {
                sr.Close();
                sw.Close();
                Client.Close();
                Environment.Exit(58);
            }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            if (sr != null)
                sr.Close();
            if (sw != null)
                sw.Close();
            if (Client != null)
                Client.Close();
            Environment.Exit(0);
        }
    }
}
