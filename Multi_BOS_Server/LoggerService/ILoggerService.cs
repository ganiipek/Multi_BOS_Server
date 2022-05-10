using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multi_BOS_Server.LoggerService
{
    public enum LoggerType
    {
        ERROR = 0,
        WARNING = 1,
        INFO = 2,
        DEBUG = 3,
        SUCCESS = 4
    }

    internal interface ILoggerService
    {
        void Send(LoggerType type, string message);
    }
}
