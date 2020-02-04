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
                Thread.Sleep(args.Length > 0 ? int.Parse(args[0]) : 200);
                if ((++counter % 2) == 0)
                {
                    Console.Error.WriteLine("TestApp error output.");

                    break;
                }
            }
        }
    }
}
