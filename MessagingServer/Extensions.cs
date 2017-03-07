using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MessagingServer
{
    public static class SocketExtensions
    {
        public static bool IsConnected(this Socket socket)
        {
            try
            {
                return !(socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0);
            }
            catch (SocketException) { return false; }
        }
    }
    public static class StreamReaderExtensions
    {
        public static bool ReadNextMessage( this StreamReader sr, out string streamData)
        {
            streamData = string.Empty;
            try
            {
                string fullInput = "";
                while (!sr.EndOfStream)
                {
                    string tempstring = sr.ReadLine();
                    if (tempstring.StartsWith("~~") || fullInput.StartsWith("~~"))
                    {
                        fullInput += tempstring;
                        if (tempstring.EndsWith("~~") && tempstring.Contains("##"))
                        {
                            streamData = fullInput;
                            return true;
                        }
                        fullInput += "\r\n";
                    }
                }
            }
            catch { }
            finally
            {
                sr.DiscardBufferedData();
            }
            return false;
        }
    }
}
