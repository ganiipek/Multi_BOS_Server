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
    internal class BrokerManager
    {
        DatabaseManager databaseManager = BreakoutManager.databaseManager;
        static BaseSocketManager tradeSocketManager = BreakoutManager.tradeSocketManager;

        static List<Broker> brokers = new List<Broker>();
        
        public void Add(Broker broker)
        {
            lock (brokers)
            {
                if (!brokers.Exists(_broker => (_broker.Name == broker.Name) && (_broker.PlatformId == broker.PlatformId)))
                {
                    brokers.Add(broker);

                    string debug = String.Format("New broker added in brokers: {0}",
                        broker.ToString()
                    );
                    Utils.SendLog(LoggerService.LoggerType.DEBUG, debug);
                }
            }
        }

        public Broker GetBroker(int brokerId)
        {
            lock (brokers)
            {
                if (brokers.Exists(bro => bro.Id == brokerId))
                {
                    return brokers.Find(bro => bro.Id == brokerId);
                }
                else
                {
                    return databaseManager.GetBroker(brokerId);
                }
            }
        }

        int GetBrokerId(Broker broker)
        {
            Broker? oldBroker = brokers.Find(_broker => (_broker.Name == broker.Name) && (_broker.PlatformId == broker.PlatformId));
            if (oldBroker != null) return oldBroker.Id;

            Broker? DbBroker = databaseManager.GetBroker(broker.Name, broker.PlatformId);
            if (DbBroker == null)
            {
                int newBrokerId = databaseManager.AddBroker(broker);

                return newBrokerId;
            }
            return DbBroker.Id;
        }
        
        public void Register(TcpClient client, dynamic json_data)
        {
            Broker broker = new Broker()
            {
                Name = (string)json_data.name,
                PlatformId = (int)json_data.pid
            };

            broker.Id = GetBrokerId(broker);

            Add(broker);

            string request = String.Format("\"router\":\"{0}\",\"broker_id\":\"{1}\"",
                "register_broker",
                broker.Id.ToString()
                );
            tradeSocketManager.Send(client, request);
        }
    }
}
