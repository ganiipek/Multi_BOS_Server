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
    internal static class BreakoutManager
    {
        static List<Breakout> breakouts = new();

        static public DatabaseManager databaseManager = new();
        static public BaseSocketManager tradeSocketManager = new();
        static public BaseSocketManager priceSocketManager = new();
        static public BaseSocketManager orderSocketManager = new();

        static public BrokerManager brokerManager = new();
        static public PairManager pairManager = new();
        static public AccountManager accountManager = new();

        static public TransactionManager transactionManager = new();
        static public OrderManager orderManager = new();

        public static void Initialize()
        {
            databaseManager.Initialize(
                "198.244.179.150",
                "3306",
                "multi_bos",
                "bos_c#",
                "Multibos1numara.!"
                );
            databaseManager.Start();

            orderManager.ControllerStart();
            transactionManager.ControllerStart();

            tradeSocketManager.Initialize(IPAddress.Any, 6969, 512);
            new Thread(new ThreadStart(tradeSocketManager.Start)).Start();

            Thread.Sleep(1000);
            priceSocketManager.Initialize(IPAddress.Any, 3131, 512);
            new Thread(new ThreadStart(priceSocketManager.Start)).Start();

            Thread.Sleep(1000);
            orderSocketManager.Initialize(IPAddress.Any, 3169, 512);
            new Thread(new ThreadStart(orderSocketManager.Start)).Start();
        }

        public static Breakout? Get(int id)
        {
            lock (breakouts)
            {
                Breakout? breakout = breakouts.Find(_breakout => _breakout.Id == id);
                if (breakout != null) return breakout;

                Breakout? DBBreakout = databaseManager.GetBreakout(id);
                if (DBBreakout != null) return DBBreakout;

                return null;

            }
        }

        public static void Add(Breakout breakout)
        {
            lock (breakouts)
            {
                if (!breakouts.Exists(_breakout => _breakout.Id == breakout.Id))
                {
                    breakouts.Add(breakout);

                    string debug = String.Format("New breakout added in breakouts: {0}",
                        breakout.ToString()
                    );
                    Utils.SendLog(LoggerService.LoggerType.SUCCESS, debug);
                }
            }
        }

        public static void Remove(Breakout breakout)
        {
            lock (breakouts)
            {
                if (!breakouts.Exists(_breakout => _breakout.Id == breakout.Id))
                {
                    breakouts.Add(breakout);

                    string debug = String.Format("The breakout removed from breakouts: {0}",
                        breakout.ToString()
                    );
                    Utils.SendLog(LoggerService.LoggerType.SUCCESS, debug);
                }
            }
        }

        public static void Register(TcpClient client, dynamic json_data)
        {
            List<AccountGroup> accountGroups = accountManager.GetAccountGroups((int)json_data.account_id, (int)json_data.pair_id);
            
            if (accountGroups.Count == 0)
            {
                string request = String.Format("\"router\":\"{0}\",\"error\":{1},\"message\":\"{2}\"",
                    "get_breakout_id",
                    true,
                    "Account group not found!"
                    );

                tradeSocketManager.Send(client, request);

                string debug = String.Format("Account group not found. PairId: {0}, AccountId: {1}",
                        ((int)json_data.pair_id).ToString(),
                        ((int)json_data.account_id).ToString()
                    );
                Utils.SendLog(LoggerService.LoggerType.WARNING, debug);
            }
            else
            {
                Account? account = accountManager.GetAccount((int)json_data.account_id);
                Pair? pair = pairManager.GetPair((int)json_data.pair_id);
                int magicNumber = (int)json_data.magic_number;

                AccountPairClient? accountPairClient = BreakoutManager.tradeSocketManager.GetAccountPairClient(account, pair, magicNumber);

                Breakout breakout = new()
                {
                    AccountPairClient = accountPairClient,
                    Type = (BreakoutType)json_data.type_id,
                    MagicNumber = magicNumber,
                    FirstSize = (double)json_data.first_size,
                    InputSize = (double)json_data.input_size,
                    TPMaxActive = (bool)json_data.tpmax,
                    TPMaxValue = (double)json_data.tpmax_value,
                    TPMinActive = (bool)json_data.tpmin,
                    TPMinValue = (double)json_data.tpmin_value,
                    TSLActive = (bool)json_data.tsl,
                    TSLValue = (decimal)json_data.tsl_value,
                    SLMax = (double)json_data.slmax,
                    BoxUpPrice = (decimal)json_data.boxup_price,
                    BoxDownPrice = (decimal)json_data.boxdown_price
                };

                int newBreakoutId = databaseManager.AddBreakout(breakout);
                breakout.Id = newBreakoutId;
                breakout.AccountGroups = accountGroups;

                Add(breakout);

                string request = String.Format("\"router\":\"{0}\",\"error\":{1},\"breakout_id\":\"{2}\",\"max_volume\":\"{3}\"",
                    "get_breakout_id",
                    false,
                    breakout.Id.ToString(),
                    breakout.AccountGroups.Find(_accountGroup => _accountGroup.Master == true).VolumeMin.ToString().Replace(',','.')
                    );
                tradeSocketManager.Send(client, request);
            }
        }
    }
}
