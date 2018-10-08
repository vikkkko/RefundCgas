using System;
using System.Collections.Generic;
using System.Text;

namespace RefundCgas
{
    class Log
    {

        public static void Common_white(string str)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(str);
        }

        public static void Common_Green(string str)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(str);
        }

        public static void Warn(string str)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(str);
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void Error(string str)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(str);
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
