using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Multi_BOS_Server.Database;
using Multi_BOS_Server.Socket;

namespace Multi_BOS_Server.Trade
{
    internal class TransactionManager
    {
        List<Transaction> transactions = new();

        void Controller()
        {
            while(true)
            {
                foreach(Transaction transaction in transactions.ToList())
                {
                    Breakout? breakout = BreakoutManager.Get(transaction.BreakoutId);
                    if (breakout == null)
                    {
                        string debug = String.Format("TransactionManager (Controller) --> Breakout is not found. Transaction Id: {0}",
                            transaction.Id.ToString()
                            );
                        Utils.SendLog(LoggerService.LoggerType.WARNING, debug);

                        continue;
                    }

                    if (transaction.OpenInfo == false && transaction.Orders.Count == transaction.RequiredOrderCount && transaction.Orders.All(_order => _order.Process == OrderProcess.IN_PROCESS))
                    {
                        // [todo] BOS'a tüm orderlar işlemde bilgisi gönder. Tüm orderların toplam hacmi receive volume olarak.
                        AccountPairClient? accountPairClient = BreakoutManager.tradeSocketManager.GetAccountPairClient(breakout.AccountPairClient.Account, breakout.AccountPairClient.Pair, breakout.MagicNumber);
                        if (accountPairClient == null)
                        {
                            string debug = String.Format("TransactionManager (Controller) --> AccountPairClient not found. Transaction Id: {0}",
                                transaction.Id.ToString()
                                );
                            Utils.SendLog(LoggerService.LoggerType.WARNING, debug);

                            continue;
                        }

                        double volume = transaction.Orders.Sum(_order => _order.Volume);
                        if (transaction.OrderType == OrderType.SELL) volume *= -1; 

                        string request = String.Format("\"router\":\"{0}\",\"breakout_id\":\"{1}\",\"multi_volume\":\"{2}\"",
                            "set_multi_volume",
                            breakout.Id.ToString(),
                            volume.ToString().Replace(',', '.')
                            );

                        BreakoutManager.tradeSocketManager.Send(accountPairClient.Client, request);

                        string debug2 = String.Format("TransactionManager (Controller) --> Order open info is sended. Transaction Id: {0}",
                                transaction.Id.ToString()
                                );
                        Utils.SendLog(LoggerService.LoggerType.DEBUG, debug2);

                        transaction.OpenInfo = true;
                    }
                    else if(transaction.ClosedInfo == false && transaction.Orders.Count == transaction.RequiredOrderCount && transaction.Orders.All(_order => _order.Process == OrderProcess.CLOSED))
                    {
                        // [todo] BOS'a tüm orderlar kapandı diye bilgi gönder. En son ki profit bilgisini de gönder.
                        // [todo] BOS'a tamam bilgisi dönerse eğer transactions'tan kaldır. Dönmezse kaldırma.
                        // [todo] transactions.Remove(transaction);
                    }
                    else if(transaction.Orders.Count == transaction.RequiredOrderCount && !transaction.Orders.All(_order => _order.Process == OrderProcess.IN_PROCESS))
                    {
                        double sumProfit = transaction.Orders.Sum(_order => _order.Profit + _order.Commission + _order.Swap);

                        if(sumProfit != transaction.LastSumProfit)
                        {
                            AccountPairClient? accountPairClient = BreakoutManager.orderSocketManager.GetAccountPairClient(breakout.AccountPairClient.Account, breakout.AccountPairClient.Pair, breakout.MagicNumber);
                            if (accountPairClient == null)
                            {
                                string debug = String.Format("TransactionManager (Controller) --> AccountPairClient not found. Transaction Id: {0}",
                                    transaction.Id.ToString()
                                    );
                                Utils.SendLog(LoggerService.LoggerType.WARNING, debug);

                                continue;
                            }

                            transaction.LastSumProfit = sumProfit;

                            sumProfit += breakout.HistoricalTransactionProfit;

                            string request = String.Format("\"router\":\"{0}\",\"breakout_id\":\"{1}\",\"profit\":\"{2}\"",
                                "multi_orders_profit",
                                breakout.Id.ToString(),
                                sumProfit.ToString().Replace(',','.')
                                );

                            BreakoutManager.orderSocketManager.Send(accountPairClient.Client, request);
                        }
                    }
                }

                Thread.Sleep(1000);
            }
        }

        public void ControllerStart()
        {
            new Thread(new ThreadStart(Controller)).Start();
        }

        public void AddTransaction(Transaction transaction)
        {
            lock (transactions)
            {
                if (!transactions.Exists(_transaction => _transaction.Id == transaction.Id))
                {
                    transactions.Add(transaction);

                    string debug = String.Format("TransactionManager (AddTransaction): {0}",
                        transaction.ToString()
                    );
                    Utils.SendLog(LoggerService.LoggerType.DEBUG, debug);
                }
            }
        }

        public Transaction? GetTransaction(int transactionId)
        {
            return transactions.Find(_transaction => _transaction.Id == transactionId);

        }

        public Transaction? GetTransaction(int breakoutId, OrderBreakoutType breakoutType, int step)
        {
            lock(transactions)
            {
                return transactions.Find(_transaction => (_transaction.BreakoutId == breakoutId) && (_transaction.OrderBreakoutType == breakoutType) && (_transaction.Step == step));
            }
        }

        public Transaction? GetTransaction(int breakoutId, OrderType orderType, OrderBreakoutType orderBreakoutType, int step)
        {
            lock(transactions)
            {
                return transactions.Find(_transaction => 
                    (_transaction.BreakoutId == breakoutId) &&
                    (_transaction.OrderBreakoutType == orderBreakoutType) &&
                    (_transaction.OrderType == orderType) &&
                    (_transaction.Step == step)
                );
            }
        }

        public Dictionary<AccountGroup, double> Allocate(List<AccountGroup> accountGroups, double multiVolume)
        {
            // [todo] Eğer pair.VolumeMin den küçükse yapma
            // [todo] Eğer broker sockete bağlı değil ise atla
            // [todo] Contract Size dikkate alınmıyor

            Console.WriteLine("accountGroups Count: " + accountGroups.Count.ToString());
            var volumes = new Dictionary<AccountGroup, double>();

            foreach (AccountGroup accountGroup in accountGroups.FindAll(_accountGroup => _accountGroup.Master == false))
            {
                if (multiVolume == 0) break;

                if(multiVolume > accountGroup.VolumeMin)
                {
                    volumes.Add(accountGroup, Math.Round(accountGroup.VolumeMin, accountGroup.Pair.VolumeDecimalCount));
                    multiVolume -= accountGroup.VolumeMin;
                }
                else
                {
                    volumes.Add(accountGroup, Math.Round(multiVolume, accountGroup.Pair.VolumeDecimalCount));
                    multiVolume -= multiVolume;
                }
            }

            if(multiVolume > 0)
            {
                double sumPerc = volumes.Keys.Sum(_accountGroup => _accountGroup.AllotedPercantage);
                
                foreach (AccountGroup accountGroup in volumes.Keys)
                {
                    double volume = (accountGroup.AllotedPercantage / sumPerc) * multiVolume;

                    volumes[accountGroup] = Math.Round(volumes[accountGroup] + volume, accountGroup.Pair.VolumeDecimalCount);
                }
            }

            if(multiVolume > volumes.Sum(x => x.Value))
            {
                //AccountGroup accountGroup = accountGroups.Values.Max()
            }

            foreach (AccountGroup accountGroup in volumes.Keys)
            {
                string debug = String.Format("AccountGroupID: {0}  ->  Volume: {1}",
                    accountGroup.ToString(),
                    volumes[accountGroup].ToString()
                );
                Console.WriteLine(debug);
            }

            return volumes;
        }

        public void SendOrders(Transaction transaction, Dictionary<AccountGroup, double> accountGroupVolumes)
        {
            foreach(AccountGroup accountGroup in accountGroupVolumes.Keys)
            {
                AccountPairClient? accountPairClient = BreakoutManager.orderSocketManager.GetAccountPairClient(accountGroup.Account, accountGroup.Pair, -1);

                if(accountPairClient == null)
                {
                    string debug = String.Format("[TransactionManager.SendOrders] accountPairClient is null. Account: ({0}), Pair:({1})",
                        accountGroup.Account.ToString(),
                        accountGroup.Pair.ToString()
                    );
                    Utils.SendLog(LoggerService.LoggerType.WARNING, debug);
                }
                else
                {
                    double volume = accountGroupVolumes[accountGroup];
                    Order order = BreakoutManager.orderManager.OrderCreate(accountPairClient, transaction.OrderType, transaction.OrderBreakoutType, volume);
                    
                    transaction.Orders.Add(order);

                    BreakoutManager.orderManager.SocketSend_OrderSend(order);

                    Thread.Sleep(10);
                }

            }
        }

        Transaction CreateTransaction(Breakout breakout, OrderType orderType, OrderBreakoutType orderBreakoutType, double volume, int step)
        {
            Transaction transaction = new()
            {
                BreakoutId = breakout.Id,
                OrderType = orderType,
                OrderBreakoutType = orderBreakoutType,
                Volume = volume,
                Step = step
            };
            BreakoutManager.databaseManager.AddTransaction(transaction);
            Console.WriteLine(transaction.ToString());

            AddTransaction(transaction);

            Dictionary<AccountGroup, double> accountGroupVolumes = Allocate(breakout.AccountGroups, volume);
            transaction.RequiredOrderCount = accountGroupVolumes.Count;
            SendOrders(transaction, accountGroupVolumes);

            return transaction;
        }
    
        public void SendTransaction(TcpClient client, dynamic json_data)
        {
            int breakoutId = (int)json_data.breakout_id;
            // double bosVolume = (double)json_data.bos_volume;
            double multiVolume = (double)json_data.multi_volume;
            int step = (int)json_data.step;
            OrderType orderType = (OrderType)json_data.order_type;
            OrderBreakoutType orderBreakoutType = (OrderBreakoutType)json_data.order_breakout_type;

            Breakout? breakout = BreakoutManager.Get(breakoutId);
            if (breakout == null)
            {
                string debug = String.Format("Send Transaction breakout not found! BreakoutId: {0}",
                        breakoutId.ToString()
                    );
                Utils.SendLog(LoggerService.LoggerType.WARNING, debug);
            }
            else
            {
                Transaction? transaction = BreakoutManager.transactionManager.GetTransaction(breakoutId, orderBreakoutType, step);
                if (transaction != null)
                {
                    AccountPairClient? accountPairClient = BreakoutManager.tradeSocketManager.GetAccountPairClient(breakout.AccountPairClient.Account, breakout.AccountPairClient.Pair, breakout.MagicNumber);
                    if (accountPairClient == null)
                    {
                        string debug = String.Format("TransactionManager (Controller) --> AccountPairClient not found. Transaction Id: {0}",
                            transaction.Id.ToString()
                            );
                        Utils.SendLog(LoggerService.LoggerType.WARNING, debug);
                    }
                    else
                    {
                        if (transaction.OpenInfo == false && transaction.Orders.Count == transaction.RequiredOrderCount && transaction.Orders.All(_order => _order.Process == OrderProcess.IN_PROCESS))
                        {
                            double volume = transaction.Orders.Sum(_order => _order.Volume);
                            if (transaction.OrderType == OrderType.SELL) volume *= -1;

                            string request = String.Format("\"router\":\"{0}\",\"breakout_id\":\"{1}\",\"multi_volume\":\"{2}\"",
                                "set_multi_volume",
                                breakout.Id.ToString(),
                                volume.ToString().Replace(',', '.')
                                );

                            BreakoutManager.tradeSocketManager.Send(accountPairClient.Client, request);

                            string debug2 = String.Format("BreakoutManager (Controller) --> Transaction is already opened. Info sending... Transaction: {0}",
                                    transaction.ToString()
                                    );
                            Utils.SendLog(LoggerService.LoggerType.DEBUG, debug2);
                            transaction.OpenInfo = true;
                        }
                    }
                }
                else
                {
                    CreateTransaction(breakout, orderType, orderBreakoutType, multiVolume, step);
                }
            }
        }

        public void CloseTransaction(Transaction transaction)
        {
            if (transaction != null)
            {
                string debug = String.Format("TransactionManager (CloseTransaction) --> Transaction is closing... Transaction Id: {0}",
                            transaction.Id.ToString()
                            );
                Utils.SendLog(LoggerService.LoggerType.WARNING, debug);

                foreach (Order order in transaction.Orders)
                {
                    BreakoutManager.orderManager.OrderClose(order);
                }
            }
        }

        public void CloseTransaction(TcpClient client, dynamic json_data)
        {
            int breakout_id = (int)json_data.breakout_id;
            int step = (int)json_data.step;
            OrderBreakoutType orderBreakoutType = (OrderBreakoutType)json_data.order_breakout_type;

            Breakout? breakout = BreakoutManager.Get(breakout_id);
            if(breakout == null)
            {
                string debug = String.Format("TransactionManager (CloseTransaction) --> Breakout is not found. Breakout Id: {0}",
                            breakout_id.ToString()
                            );
                Utils.SendLog(LoggerService.LoggerType.WARNING, debug);
            }
            else
            {
                Transaction? transaction = GetTransaction(breakout_id, orderBreakoutType, step);
                if(transaction == null)
                {
                    string debug = String.Format("TransactionManager (CloseTransaction) --> Transaction is not found. Breakout Id: {0}, Step: {1},Order Breakout Type: {2}",
                            breakout_id.ToString(),
                            step.ToString(),
                            orderBreakoutType.ToString()
                            );
                    Utils.SendLog(LoggerService.LoggerType.WARNING, debug);
                }
                else
                {
                    CloseTransaction(transaction);
                }
            }
        }
    }
}
