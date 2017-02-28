using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
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
        private int MYID;
        private const int PollRateMS = 500;
        private TcpClient Client;
        StreamReader sr;
        StreamWriter sw;
        public MainWindow()
        {
            InitializeComponent();
            
        }

        private void PollStreamForNew()
        {
            while (Client.Connected)
            {
                try
                {
                    // Check for messages
                    if (sr.Peek() >= 0)
                    {
                        string nextMessage;
                        while (sr.ReadNextMessage(out nextMessage))
                        {
                            // Handle Messages 
                            messageHandler(Message.InterpretString(nextMessage));
                            nextMessage = string.Empty;
                        }
                    }
                    Task.Delay(PollRateMS);
                }
                catch (Exception)
                {
                    OutputTextBlock.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate () {
                        OutputTextBlock.Text = "POLLING ERROR";
                    }));
                    return;
                }
            }
        }

        private void messageHandler(Message message)
        {
            OutputTextBlock.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate () {
                OutputTextBlock.Text += Environment.NewLine + message.senderID + " " + message.Code + ": " + message.MessageString;
            }));
        }
        
        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (Client == null)
            {
                MYID = int.Parse(ReceiverTextBox.Text);
                try
                {
                    Client = new TcpClient();
                    Client.Connect("localhost", 25566);
                    if (Client.Connected)
                    {
                        OutputTextBlock.Text = "Connected";

                        sr = new StreamReader(Client.GetStream());
                        sw = new StreamWriter(Client.GetStream());

                        string streamData;
                        if (!sr.ReadNextMessage(out streamData))
                            throw new Exception("");

                        Message serverMessageObj = Message.InterpretString(streamData);
                        OutputTextBlock.Text += serverMessageObj.MessageString;

                        if (serverMessageObj.Code == MessageCode.C001)
                            SendMessage(new Message(MessageCode.C002, MYID, 0, string.Empty));
                        else throw new Exception("No Connection Test Received");

                        // Start polling connection to see if it's working
                        Task t1 = new Task(() => PollStreamForNew());
                        t1.Start();

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
            SendMessage(new Message(MessageCode.C003, MYID, int.Parse(ReceiverTextBox.Text), MessageTextBox.Text));
        }
    }
}
