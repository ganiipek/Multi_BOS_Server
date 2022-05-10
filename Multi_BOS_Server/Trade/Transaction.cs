using Multi_BOS_Server.Socket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multi_BOS_Server.Trade
{
    internal class Transaction
    {
        public int Id { get; set; }
        public int BreakoutId { get; set; }
        public List<Order> Orders{ get; set; }
        public int RequiredOrderCount { get; set; }
        public double Volume { get; set; }
        public OrderType OrderType { get; set; }
        public OrderBreakoutType OrderBreakoutType { get; set; }
        public int Step { get; set; }
        public double LastSumProfit { get; set; }
        public bool OpenInfo { get; set; }
        public bool ClosedInfo { get; set; }

        public Transaction()
        {
            Orders = new();
            OpenInfo = false;
            ClosedInfo = false;
        }

        public override string ToString()
        {
            return String.Format("Id: {0}, Step: {1}, OrderType: {2}, OrderBreakoutType: {3}",
                Id.ToString(),
                Step.ToString(),
                OrderType.ToString(),
                OrderBreakoutType.ToString()
                );
        }
    }
}