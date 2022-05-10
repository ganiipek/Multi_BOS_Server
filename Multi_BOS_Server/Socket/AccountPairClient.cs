using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Multi_BOS_Server.Trade;

namespace Multi_BOS_Server.Socket
{
    internal class AccountPairClient
    {
        public Account Account { get; set; }
        public Pair Pair { get; set; }
        public TcpClient Client { get; set; }
        public int MagicNumber { get; set; }
    }
}
