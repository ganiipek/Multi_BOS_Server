using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Multi_BOS_Server.Socket;

namespace Multi_BOS_Server.Trade
{
    public enum OrderType
    {
        BUY = 0,
        SELL = 1,
        BUY_LIMIT = 2,
        SELL_LIMIT = 3,
        BUY_STOP = 4,
        SELL_STOP = 5
    }

    public enum OrderBreakoutType
    {
        STEP = 0,
        PARTIAL = 1,
        HEDGE_IN = 2,
        HEDGE_OUT = 3
    }

    public enum OrderProcess
    {
        ERROR = 0,
        PREPARED = 1,
        SEND_OPEN = 2,
        IN_PROCESS = 3,
        SEND_CLOSE = 4,
        CLOSED = 5
    }

    public enum OrderError
    {
        NOT_ERROR = 0,
        ORDER_NOT_FOUND = 1,
        ORDER_NOT_CLOSED = 2
    }


    internal class Order
    {
        public AccountPairClient AccountPairClient { get; set; }
        public int Id { get; set; }
        public int Ticket { get; set; }
        public OrderType Type { get; set; }
        public OrderProcess Process { get; set; }
        public OrderBreakoutType BreakoutType { get; set; }
        public OrderError Error { get; set; }
        public DateTime SendedTime { get; set; }
        public decimal SendedPrice { get; set; }
        public DateTime OpenTime { get; set; }
        public decimal OpenPrice { get; set; }
        public DateTime ClosedTime { get; set; }
        public decimal ClosedPrice { get; set; }
        public double Volume { get; set; }
        public double Commission { get; set; }
        public double Swap { get; set; }
        public double Profit { get; set; }
        public DateTime LastControl { get; set; }

        public Order()
        {
            Error = OrderError.NOT_ERROR;
            SendedTime = DateTime.MinValue;
            OpenTime = DateTime.MinValue;
            ClosedTime = DateTime.MinValue;
            ClosedPrice = 0;
            LastControl = DateTime.MinValue;
        }

        public override string ToString()
        {
            return String.Format("Id: {0}, Type: {1}, Process: {2}, Error: {3}, Ticket: {4}, Sended: [{5} - {6}], Open: [{7} - {8}], Volume: {9}, Commission: {10}, Swap: {11}, Profit: {12}, Close: [{13} - {14}]",
                Id.ToString(),
                Type.ToString(),
                Process.ToString(),
                Error.ToString(),
                Ticket.ToString(),
                SendedTime.ToString(),
                SendedPrice.ToString(),
                OpenTime.ToString(),
                OpenPrice.ToString(),
                Volume.ToString(),
                Commission.ToString(),
                Swap.ToString(),
                Profit.ToString(),
                ClosedTime.ToString(),
                ClosedPrice.ToString()
                );
        }

        public string ToSummary()
        {
            return String.Format("Id: {0}, Ticket: {1}, Type: {2}, Process: {3}, Error: {4}",
                Id.ToString(),
                Ticket.ToString(),
                Type.ToString(),
                Process.ToString(),
                Error.ToString()
                );
        }
    }
}
