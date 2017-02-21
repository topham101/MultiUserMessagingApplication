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
using MessagingServer;

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
            if (Client == null)
            {
                try
                {
                    Client = new TcpClient();
                    Client.Connect("localhost", 25566);
                    if (Client.Connected)
                    {
                        OutputTextBlock.Text = "Connected";

                        sr = new StreamReader(Client.GetStream());
                        sw = new StreamWriter(Client.GetStream());

                        Message serverMessageObj = Message.InterpretString(ReadFullStream());
                        OutputTextBlock.Text += serverMessageObj.MessageString;

                        if (serverMessageObj.Code == MessageCode.C001)
                            SendMessage(new Message(MessageCode.C002, string.Empty));
                        else throw new Exception("No Connection Test Received");
                        return;
                        // Start polling connection to see if it's working
                    }
                }
                catch (Exception exc)
                {
                    OutputTextBlock.Text = "ERROR: " + exc.ToString();
                    sr.Close();
                    sw.Close();
                    Client.Close();
                    Environment.Exit(58);
                }
            }
        }

        private string ReadFullStream()
        {
            string fullInput = "";
            while (sr.Peek() >= 0)
            {
                fullInput += sr.ReadLine() + "\r\n";
            }
            sr.DiscardBufferedData();
            return fullInput;
        }

        private void SendMessage(Message message)
        {
            string thing = message.generateMessage();
            sw.WriteLine(thing);
            sw.Flush();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (sr != null)
                sr.Close();
            if (sw != null)
                sw.Close();
            if (Client != null)
                Client.Close();
            Environment.Exit(0);
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SendMessage(new Message(MessageCode.C003, MessageTextBox.Text));
        }
    }
}
