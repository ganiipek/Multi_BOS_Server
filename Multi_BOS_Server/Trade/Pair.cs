using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Multi_BOS_Server.Trade
{
    internal class Pair
    {
        public int Id { get; set; }
        public Broker Broker { get; set; }
        public string Symbol { get; set; }
        public double Ask { get; set; }
        public double Bid { get; set; }
        public int Spread { get; set; }
        public int ContractSize { get; set; }
        public int Digits { get; set; }
        public double VolumeMin { get; set; }
        public int VolumeDecimalCount { get; set; }
        public DateTime TickTime { get; set; }

        public override string ToString()
        {
            return String.Format("Id: {0}, Broker: [{1}], Symbol: {2}, Ask: {3}, Bid: {4}, Spread:{5}, Contract Size: {6}, Digits: {7}, Volume Min: {8}, Tick Time: {9}",
                Id.ToString(),
                Broker.ToString(),
                Symbol.ToString(),
                Ask.ToString(),
                Bid.ToString(),
                Spread.ToString(),
                ContractSize.ToString(),
                Digits.ToString(),
                VolumeMin.ToString(),
                TickTime.ToString()
                );
        }
    }
}
