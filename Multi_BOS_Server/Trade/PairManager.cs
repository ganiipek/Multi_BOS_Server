using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Multi_BOS_Server.Database;
using Multi_BOS_Server.Socket;

namespace Multi_BOS_Server.Trade
{
    internal class PairManager
    {
        DatabaseManager databaseManager = BreakoutManager.databaseManager;

        BrokerManager brokerManager = BreakoutManager.brokerManager;

        BaseSocketManager tradeSocketManager = BreakoutManager.tradeSocketManager;
        BaseSocketManager priceSocketManager = BreakoutManager.priceSocketManager;

        static List<Pair> pairs = new List<Pair>();

        public void AddPair(Pair pair)
        {
            lock (pairs)
            {
                if (!pairs.Exists(_pair => (_pair.Symbol == pair.Symbol) && (_pair.Broker.Id == pair.Broker.Id)))
                {
                    pairs.Add(pair);

                    string debug = String.Format("New pair added in pairs: {0}",
                        pair.ToString()
                    );
                    Utils.SendLog(LoggerService.LoggerType.DEBUG, debug);
                }
            }
        }

        public Pair? GetPair(int pairId)
        {
            if (pairs.Exists(_pair => _pair.Id == pairId))
            {
                return pairs.Find(_pair => _pair.Id == pairId);
            }
            else
            {
                Pair? pair = BreakoutManager.databaseManager.GetPair(pairId);
                //AddPair(pair);
                return pair;
            }
        }

        public int GetPairId(Pair pair)
        {
            Pair? oldPair = pairs.Find(_pair => (_pair.Symbol == pair.Symbol) && (_pair.Broker.Id == pair.Broker.Id));
            if (oldPair != null) return oldPair.Id;

            Pair? SQLPair = databaseManager.GetPair(pair.Symbol, pair.Broker.Id);
            if (SQLPair == null)
            {
                int newPairId = databaseManager.AddPair(pair);

                return newPairId;
            }
            return SQLPair.Id;
        }

        public void UpdateDB(Pair pair)
        {
            databaseManager.UpdatePair(pair);
        }

        public void Register(TcpClient client, dynamic json_data)
        {
            Pair pair = new()
            {
                Symbol = (string)json_data.symbol,
                Broker = brokerManager.GetBroker((int)json_data.broker_id),
                ContractSize = json_data.cs,
                Digits = json_data.d,
                VolumeMin = json_data.vm,
                VolumeDecimalCount = json_data.vd,
            };
            pair.Id = GetPairId(pair);

            AddPair(pair);
            UpdateDB(pair);

            string request = String.Format("\"router\":\"{0}\",\"symbol_id\":\"{1}\"",
                "register_symbol",
                pair.Id.ToString()
                );

            tradeSocketManager.Send(client, request);
        }

        public bool UpdateTick(TcpClient client, dynamic json_data)
        {
            Pair? pair = GetPair((int)json_data.s_id);
            if(pair != null)
            {
                pair.Ask = (double)json_data.ask;
                pair.Bid = (double)json_data.bid;
                pair.Spread = Convert.ToInt32((pair.Ask - pair.Bid) * Math.Pow(10, pair.Digits));
                pair.TickTime = Utils.UnixTimeStampToDateTime((ulong)json_data.time);
                
                string request = String.Format("\"router\":\"{0}\",\"s_id\":\"{1}\",\"a\":{2}\",\"b\":\"{3}\",\"t\":\"{4}\"",
                    "tick",
                    pair.Id.ToString(),
                    pair.Ask.ToString(),
                    pair.Bid.ToString(),
                    pair.TickTime.ToString()
                    );

                priceSocketManager.UpdateBroadcast(request+"\n");

                return true;
            }
            return false;
        }
    }
}
