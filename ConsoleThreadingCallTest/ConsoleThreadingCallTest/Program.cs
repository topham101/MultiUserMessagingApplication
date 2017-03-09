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
            Dictionary<int, string> dict = new Dictionary<int, string>();

            Console.WriteLine(dict);
            dict[0] = "zero";
            Console.WriteLine(dict[0]);
            dict[0] = "zeroChanged";
            Console.WriteLine(dict[0]);

            Console.ReadKey();
        }
    }
}
