using Multi_BOS_Server.Socket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Multi_BOS_Server.Trade
{
    public enum BreakoutType
    {
        WAITING = 0,
        TRADE_PENDING = 1,
        LIMIT_BOARDING = 2,
        OPEN_BUY = 3,
        OPEN_SELL = 4,
        TRADE_HEDGING = 5,
        TRADE_HEDGED = 6,
        TRADE_HEDGE_EXIT = 7,
        TRADE_CLOSING = 8,
        TRADE_CLOSED = 9
    }
    internal class Breakout
    {
        public int Id { get; set; }
        public AccountPairClient AccountPairClient { get; set; }
        public BreakoutType Type { get; set; }
        public List<AccountGroup> AccountGroups { get; set; }
        public List<Transaction> Transactions { get; set; }
        public int MagicNumber { get; set; }
        public double FirstSize { get; set; }
        public double InputSize { get; set; }
        public bool TPMaxActive { get; set; }
        public double TPMaxValue { get; set; }
        public bool TPMinActive { get; set; }
        public double TPMinValue { get; set; }
        public bool TSLActive { get; set; }
        public decimal TSLValue { get; set; }
        public double SLMax { get; set; }
        public decimal BoxUpPrice { get; set; }
        public decimal BoxDownPrice { get; set; }

        public Breakout()
        {
            AccountGroups = new();
            Transactions = new();
        }

        public override string ToString()
        {
            return String.Format("Id: {0}, Pair: [{1}], Account: [{2}], Type: {3}, First Size: {4}, Input Size: {5}, TP Max: [Active: {6}, Value: {7}], Tp Min: [Active: {8}, Value: {9}], TSL: [Active: {10}, Value: {11}], SL Max: {12}, Box: [Up: {13}, Down: {14}]",
                    Id.ToString(),
                    AccountPairClient.Pair.ToString(),
                    AccountPairClient.Account.ToString(),
                    Type.ToString(),
                    FirstSize.ToString(),
                    InputSize.ToString(),
                    TPMaxActive.ToString(),
                    TPMaxValue.ToString(),
                    TPMinActive.ToString(),
                    TPMinValue.ToString(),
                    TSLActive.ToString(),
                    TSLValue.ToString(),
                    SLMax.ToString(),
                    BoxUpPrice.ToString(),
                    BoxDownPrice.ToString()
                );
        }
    }
}
