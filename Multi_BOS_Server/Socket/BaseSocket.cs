using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Multi_BOS_Server.Socket
{
    internal class BaseSocket
    {
        public dynamic Host;
        public int Port;
        public int BufferSize;

        public BaseSocket()
        {
            Host = IPAddress.Any;
            Port = 6969;
            BufferSize = 225;
        }
    }
}
