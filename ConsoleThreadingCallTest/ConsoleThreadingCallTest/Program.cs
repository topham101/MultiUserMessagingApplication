using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleThreadingCallTest
{
    class Program
    {
        static void Main(string[] args)
        {
            string temp = "HELLOh";
            Console.WriteLine(temp.Substring(5, temp.Length - 5));
        }
    }
}
