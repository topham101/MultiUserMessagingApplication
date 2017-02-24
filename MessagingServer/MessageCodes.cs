using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace MessagingServer
{
    public static class MessageCodes
    {
        public static Dictionary<int, string> MessageCodeDict { get; private set; }

        public static bool PopulateDictionary()
        {
            MessageCodeDict = new Dictionary<int, string>();
            using (StreamReader codeReader = new StreamReader("ProtocolCodes.txt"))
            {
                string protocolCodes = codeReader.ReadToEnd();
                if (string.IsNullOrEmpty(protocolCodes))
                    return false;
                string[] seperators = new string[] { ">", ";" };
                string[] results = protocolCodes.Split(seperators,
                    StringSplitOptions.RemoveEmptyEntries);
                char[] trimChars = new char[] { '\r', '\n' };
                for (int i = 0; i < results.Length; i++)
                    results[i] = results[i].TrimStart(trimChars);

                if (results.Length % 2 == 0)
                {
                    for (int i = 0; i < (results.Length / 2); i++)
                    {
                        int key;
                        if (int.TryParse(results[i * 2], out key))
                            MessageCodeDict.Add(key, results[(i * 2) + 1]);
                        else return false;
                    }
                    return true;
                }
                else return false; 
            }
        }
    }
}
