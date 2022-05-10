using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Multi_BOS_Server.Trade
{
    public enum AccountMarginMode
    {
        HEDGING = 0,
        NETTING = 1,
    }

    public enum AccountHedgeMode
    {
        HALF = 0,
        FULL = 1
    }

    internal class Account
    {
        public int Id { get; set; }
        public Broker Broker { get; set; }
        public AccountMarginMode MarginMode { get; set; }
        public AccountHedgeMode HedgeMode { get; set; }
        public string Owner { get; set; }
        public int Number { get; set; }
        public double Balance { get; set; }
        public bool TerminalTradeAllowed { get; set; }
        public bool TradeExpertAllowed { get; set; }
        public bool TradeAllowed { get; set; }

        public override string ToString()
        {
            return String.Format("Id: {0}, Broker: [{1}], Owner: {2}, Number: {3}, Terminal Trade Allowed: {4}, Trade Expert Allowed: {5}, Trade Allowed: {6}, Balance: {7}",
                Id.ToString(),
                Broker.ToString(),
                Owner,
                Number.ToString(),
                TerminalTradeAllowed.ToString(),
                TradeExpertAllowed.ToString(),
                TradeAllowed.ToString(),
                Balance.ToString()
            );
        }
    }
}
