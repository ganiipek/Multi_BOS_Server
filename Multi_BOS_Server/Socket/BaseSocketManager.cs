using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Multi_BOS_Server.Database;
using Multi_BOS_Server.Trade;
using System.Globalization;

namespace Multi_BOS_Server.Socket
{
    internal class BaseSocketManager
    {
        TcpListener client;
        BaseSocket baseSocket = new();
        List<TcpClient> broadcastListeners = new List<TcpClient>();

        List<AccountPairClient> accountPairClientList = new();

        public void Initialize(dynamic host, int port, int bufferSize)
        {
            baseSocket.Host = host;
            baseSocket.Port = port;
            baseSocket.BufferSize = bufferSize;
        }

        public void Start()
        {
            client = new TcpListener(baseSocket.Host, baseSocket.Port);
            client.Start();

            string debug = String.Format("Socket is starting! {0}:{1} is listening...",
                    baseSocket.Host.ToString(),
                    baseSocket.Port
                    );
            Utils.SendLog(LoggerService.LoggerType.SUCCESS, debug);

            while(true)
            {
                while (!client.Pending())
                {
                    Thread.Sleep(1000);
                }
                MetaConnectionThread newconnection = new MetaConnectionThread();
                newconnection.threadListner = client;
                newconnection.bufferSize = baseSocket.BufferSize;
                Thread newthread = new Thread(new ThreadStart(newconnection.HandleConnection));
                newthread.Start();
            }
        }

        public void Send(TcpClient client, string data)
        {
            byte[] byteData = new byte[baseSocket.BufferSize];
            byteData = Encoding.ASCII.GetBytes("{" + data + "}");
            client.GetStream().Write(byteData, 0, byteData.Length);
        }

        public void AddBroadcastListeners(TcpClient client)
        {
            lock(broadcastListeners)
            {
                if(!broadcastListeners.Exists(_client => _client == client))
                {
                    broadcastListeners.Add(client);
                }
            }
        }

        public List<TcpClient>? GetBroadcastListeners()
        {
            return broadcastListeners;
        }

        public void UpdateBroadcast(string data)
        {
            Broadcast(data);
            //new Thread(() => Broadcast(data)).Start();
        }

        void Broadcast(string data)
        { // {"type":"register_broadcast","socket":"price"} 
            foreach (TcpClient client in broadcastListeners)
            {
                Send(client, data);
            }
        }

        public AccountPairClient? GetAccountPairClient(Account account, Pair pair, int magicNumber)
        {
            AccountPairClient? accountPairClient = accountPairClientList.Find(_accountPair => (_accountPair.Account.Id == account.Id) && (_accountPair.Pair.Id == pair.Id) && (_accountPair.MagicNumber == magicNumber));
            
            if(accountPairClient != null)
            {
                return accountPairClient;
            }
            
            return null;
        }

        public AccountPairClient? GetAccountPairClient(TcpClient client)
        {
            AccountPairClient? accountPairClient = accountPairClientList.Find(_accountPair => _accountPair.Client == client);

            if (accountPairClient != null)
            {
                return accountPairClient;
            }

            return null;
        }

        public void AddAccountPairClient(AccountPairClient accountPairClient)
        {
            lock(accountPairClientList)
            {
                accountPairClientList.Add(accountPairClient);

                string debug = String.Format("Socket registration successful. ({0}:{1}). Account Id: {2}, Pair Id: {3}, Magic Number: {4}",
                        baseSocket.Host.ToString(),
                        baseSocket.Port.ToString(),
                        accountPairClient.Account.Id.ToString(),
                        accountPairClient.Pair.Id.ToString(),
                        accountPairClient.MagicNumber.ToString()
                        );
                Utils.SendLog(LoggerService.LoggerType.DEBUG, debug);
            }
        }

        public void AddAccountPairClient(Account account, Pair pair, TcpClient client, int magicNumber)
        {
            AccountPairClient accountPairClient = new()
            {
                Account = account,
                Pair = pair,
                Client = client,
                MagicNumber = magicNumber
            };

            AddAccountPairClient(accountPairClient);
        }

        public void UpdateAccountPairClient(AccountPairClient newAccountPairClient)
        {
            AccountPairClient? accountPairClient = accountPairClientList.Find(_accountPairClient => (_accountPairClient.Pair.Id == newAccountPairClient.Pair.Id) && (_accountPairClient.Account.Id == newAccountPairClient.Account.Id) && (_accountPairClient.MagicNumber == newAccountPairClient.MagicNumber));
            
            if(accountPairClient == null)
            {
                accountPairClientList.Add(newAccountPairClient);
            }
            else
            {
                lock (accountPairClient)
                {
                    accountPairClient.Client = newAccountPairClient.Client;
                    accountPairClient.MagicNumber = newAccountPairClient.MagicNumber;
                }
            }
        }

        public void UpdateAccountPairClient(Account account, Pair pair, TcpClient client, int magicNumber)
        {
            AccountPairClient? accountPairClient = accountPairClientList.Find(_accountPairClient => (_accountPairClient.Pair.Id == pair.Id) && (_accountPairClient.Account.Id == account.Id) && (_accountPairClient.MagicNumber == magicNumber));

            if (accountPairClient == null)
            {
                AddAccountPairClient(account, pair, client, magicNumber);
            }
            else
            {
                lock(accountPairClient)
                {
                    accountPairClient.Client = client;
                    accountPairClient.MagicNumber = magicNumber;
                }
            }
        }

        public void RemoveAccountPairClient(AccountPairClient accountPairClient)
        {
            lock (accountPairClientList)
            {
                accountPairClientList.Remove(accountPairClient);

                string debug = String.Format("Socket removed successful. ({0}:{1}). Account Id: {2}, Pair Id: {3}",
                        baseSocket.Host.ToString(),
                        baseSocket.Port.ToString(),
                        accountPairClient.Account.Id.ToString(),
                        accountPairClient.Pair.Id.ToString()
                        );
                Utils.SendLog(LoggerService.LoggerType.DEBUG, debug);
            }
        }

        public void RemoveAccountPairClient(TcpClient client)
        {
            AccountPairClient? accountPairClient = accountPairClientList.Find(_accountPairClient => _accountPairClient.Client == client);

            if(accountPairClient != null)
            {
                RemoveAccountPairClient(accountPairClient);
            }
        }

        public bool RegisterSocket(int accountId, int pairId, TcpClient client, int magicNumber)
        {
            Account? account = BreakoutManager.accountManager.GetAccount(accountId);

            if (account == null)
            {
                string debug = String.Format("Failed to register socket ({0}:{1}). Reason: #{2} account not found.",
                    baseSocket.Host.ToString(),
                    baseSocket.Port.ToString(),
                    accountId.ToString()
                    );
                Utils.SendLog(LoggerService.LoggerType.ERROR, debug);

                string request = String.Format("\"router\":\"{0}\",\"register\":\"{1}\",\"reason\":\"Account not found.\"",
                    "register_socket",
                    false.ToString()
                );
                Send(client, request);
                return false;
            }

            Pair? pair = BreakoutManager.pairManager.GetPair(pairId);

            if (pair == null)
            {
                string debug = String.Format("Failed to register socket ({0}:{1}). Reason: #{2} pair not found.",
                    baseSocket.Host.ToString(),
                    baseSocket.Port.ToString(),
                    pairId.ToString()
                    );
                Utils.SendLog(LoggerService.LoggerType.ERROR, debug);

                string request = String.Format("\"router\":\"{0}\",\"register\":\"{1}\",\"reason\":\"Pair not found.\"",
                    "register_socket",
                    false.ToString()
                );
                Send(client, request);
                return false;
            }

            UpdateAccountPairClient(account, pair, client, magicNumber);

            string request2 = String.Format("\"router\":\"{0}\",\"register\":\"{1}\"",
                "register_socket",
                true.ToString()
                );
            Send(client, request2);
            return true;
        }

        public bool RegisterSocket(TcpClient client, dynamic json_data)
        {
            int accountId = (int)json_data.account_id;
            int pairId = (int)json_data.pair_id;
            int magicNumber = (int)json_data.magic_number;

            return RegisterSocket(accountId, pairId, client, magicNumber);
        }
    }

    internal class MetaConnectionThread
    {
        public TcpListener threadListner;
        public int bufferSize;
        public void HandleConnection()
        {
            int recv;
            byte[] data = new byte[bufferSize];
            TcpClient client = threadListner.AcceptTcpClient();
            client.NoDelay = true;

            NetworkStream ns = client.GetStream();

            while (true)
            {
                data = new byte[bufferSize];

                do
                {
                    recv = ns.Read(data, 0, data.Length);
                }
                while (ns.DataAvailable);

                if (recv == 0) break;

                string data_str = Encoding.ASCII.GetString(data).Split('}')[0] + "}";

                //Console.ForegroundColor = ConsoleColor.Yellow;
                //Console.WriteLine(DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss.fff") + " --> " + data_str);
                //Console.ResetColor();


                dynamic data_json = null;

                try
                {
                    data_json = JsonConvert.DeserializeObject(data_str);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("\n[ERROR] SocketReceive Json: " + ex.Message.ToString());
                    Console.WriteLine(data_str);
                }


                if (data_json != null) newJSONData(client, data_json);

            }
            ns.Close();
            client.Close();

            BreakoutManager.tradeSocketManager.RemoveAccountPairClient(client);
            BreakoutManager.priceSocketManager.RemoveAccountPairClient(client);
            BreakoutManager.orderSocketManager.RemoveAccountPairClient(client);
        }

        public void newJSONData(TcpClient client, dynamic json_data)
        {
            if (Utils.DatabaseIsConnected())
            {
                // Trade Socket
                if(json_data.type == "register_broadcast")
                {
                    if(json_data.socket == "trade")
                    {
                        BreakoutManager.tradeSocketManager.AddBroadcastListeners(client);
                    }
                    else if(json_data.socket == "price")
                    {
                        BreakoutManager.priceSocketManager.AddBroadcastListeners(client);
                    }
                    else if (json_data.socket == "order")
                    {

                    }
                }
                else if (json_data.type == "register_broker")
                {
                    BreakoutManager.brokerManager.Register(client, json_data);
                }
                else if (json_data.type == "register_account")
                {
                    BreakoutManager.accountManager.Register(client, json_data);
                }
                else if (json_data.type == "register_symbol")
                {
                    BreakoutManager.pairManager.Register(client, json_data);
                }
                else if (json_data.type == "register_socket")
                {
                    if (json_data.socket == "trade")
                    {
                        BreakoutManager.tradeSocketManager.RegisterSocket(client, json_data);
                    }
                    else if (json_data.socket == "order")
                    {
                        BreakoutManager.orderSocketManager.RegisterSocket(client, json_data);
                    }
                    else if (json_data.socket == "price")
                    {
                        BreakoutManager.priceSocketManager.RegisterSocket(client, json_data);
                    }
                }
                else if (json_data.type == "get_breakout_id")
                {
                    BreakoutManager.Register(client, json_data);
                }
                else if (json_data.type == "send_transaction")
                {
                    BreakoutManager.transactionManager.SendTransaction(client, json_data);
                }
                else if (json_data.type == "close_transaction")
                {
                    BreakoutManager.transactionManager.CloseTransaction(client, json_data);
                }
                else if (json_data.type == "close_hedge_transaction")
                {
                    BreakoutManager.transactionManager.CloseHedge(client, json_data);
                }
                else if (json_data.type == "close_breakout")
                {
                    BreakoutManager.Close(client, json_data);
                }
                else if (json_data.type == "breakout_hedge_in")
                {
                    BreakoutManager.HedgeIn(client, json_data);
                }

                #region Price Socket
                else if (json_data.type == "update_tick")
                {
                    BreakoutManager.pairManager.UpdateTick(client, json_data);
                }
                #endregion

                #region Order Socket
                else if (json_data.type == "order_send")
                {
                    BreakoutManager.orderManager.SocketReceive_OrderSend(client, json_data);
                }
                else if (json_data.type == "order_info_ticket")
                {
                    BreakoutManager.orderManager.SocketReceive_OrderInfoByTicket(client, json_data);
                }
                else if (json_data.type == "order_info_id")
                {
                    BreakoutManager.orderManager.SocketReceive_OrderInfoByTicket(client, json_data);
                }
                else if (json_data.type == "order_info_update")
                {
                    BreakoutManager.orderManager.SocketReceive_OrderInfoUpdate(client, json_data);
                }
                
                #endregion
            }
        }
    }
}
