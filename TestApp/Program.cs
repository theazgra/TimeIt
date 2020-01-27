using System;
using System.Threading;

namespace TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            int counter = 0;
            while (true)
            {
                Console.WriteLine("TestApp output");
                if ((++counter % 5) == 0)
                {
                    Console.Error.WriteLine("TestApp error output.");
                    break;
                }
                Thread.Sleep(1000);
            }
        }
    }
}
