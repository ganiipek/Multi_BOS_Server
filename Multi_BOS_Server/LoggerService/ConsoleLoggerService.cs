using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multi_BOS_Server.LoggerService
{
    internal class ConsoleLoggerService : ILoggerService
    {
        public void Send(LoggerType type, string message)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss.fff"));

            switch (type)
            {
                case LoggerType.ERROR:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LoggerType.WARNING:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LoggerType.INFO:
                    Console.ForegroundColor = ConsoleColor.Blue;
                    break;
                case LoggerType.DEBUG:
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    break;
                case LoggerType.SUCCESS:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
            }

            Console.Write(" [" + type.ToString() + "] ");
            Console.ResetColor();
            Console.WriteLine(message);
        }
    }
}
