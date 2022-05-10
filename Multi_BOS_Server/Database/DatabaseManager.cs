using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data;
using MySql.Data.MySqlClient;
using Multi_BOS_Server.Trade;
using Multi_BOS_Server.Socket;

namespace Multi_BOS_Server.Database
{
    internal class DatabaseManager
    {
        BrokerManager brokerManager = BreakoutManager.brokerManager;
        PairManager pairManager = BreakoutManager.pairManager;
        AccountManager accountManager = BreakoutManager.accountManager;

        MySqlConnection con;
        MySqlCommand cmd;
        DatabaseBase db;

        string connectionString;

        public void Initialize(string host, string port, string databaseName, string user, string password)
        {
            db = new DatabaseBase()
            {
                Host = host,
                Port = port,
                DatabaseName = databaseName,
                User = user,
                Password = password
            };
        }

        public void Start()
        {
            try
            {
                connectionString = String.Format(
                    // "Server={0};Database={1};Uid={2};Pwd={3};MultipleActiveResultSets=True",
                    "Server={0};Database={1};Uid={2};Pwd={3}",
                    db.Host,
                    db.DatabaseName,
                    db.User,
                    db.Password
                    );
                con = new MySqlConnection(connectionString);
                con.Open();

                string debug = String.Format("Database ({0}/{1}) Connection is successful!",
                    db.Host,
                    db.DatabaseName
                    );
                Utils.SendLog(LoggerService.LoggerType.SUCCESS, debug);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Database Error: " + ex.Message);
            }
        }

        public bool IsConnected()
        {
            try
            {
                if(con == null) return false;
                if (con.State == ConnectionState.Open) return true;
                con.Open();
                return true;
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"Can not open connection ! ErrorCode: {ex.ErrorCode} Error: {ex.Message}");
                Console.WriteLine("Database Connection: False");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Can not open connection ! Error: {ex.Message}");
                Console.WriteLine("Database Connection: False");
                return false;
            }
        }

        #region Broker
        public Broker? GetBroker(int brokerId)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "SELECT * FROM brokers WHERE id=@id";
                    cmd.Parameters.AddWithValue("id", brokerId);

                    using (MySqlDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.HasRows)
                        {
                            while (rdr.Read())
                            {
                                Broker broker = new();
                                broker.Id = rdr.GetInt32(rdr.GetOrdinal("id"));
                                broker.Name = (string)rdr["name"];
                                broker.PlatformId = rdr.GetInt32(rdr.GetOrdinal("platform_id"));

                                return broker;
                            }
                        }
                    }
                }
            }

            return null;
        }

        public Broker? GetBroker(string brokerName, int platformId)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "SELECT * FROM brokers WHERE name=@name AND platform_id=@platform_id";
                    cmd.Parameters.AddWithValue("platform_id", platformId);
                    cmd.Parameters.AddWithValue("name", brokerName);

                    using (MySqlDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.HasRows)
                        {
                            while (rdr.Read())
                            {
                                Broker broker = new Trade.Broker();
                                broker.Id = rdr.GetInt32(rdr.GetOrdinal("id"));
                                broker.Name = (string)rdr["name"];
                                broker.PlatformId = rdr.GetInt32(rdr.GetOrdinal("id"));

                                return broker;
                            }
                        }
                    }
                }
            }
            return null;
        }

        public int AddBroker(Broker broker)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "INSERT INTO brokers (platform_id, name) VALUES (@platform_id, @name); SELECT LAST_INSERT_ID();";
                    cmd.Parameters.AddWithValue("platform_id", broker.PlatformId);
                    cmd.Parameters.AddWithValue("name", broker.Name);

                    cmd.ExecuteNonQuery();
                    int id = (int)cmd.LastInsertedId;

                    broker.Id = id;
                    string debug = String.Format("New broker added in DB: {0}",
                            broker.ToString()
                        );
                    Utils.SendLog(LoggerService.LoggerType.INFO, debug);

                    return id;
                }
            }
        }
        #endregion

        #region Account
        public Account? GetAccount(int accountId)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "SELECT * FROM accounts WHERE id=@id";
                    cmd.Parameters.AddWithValue("id", accountId);

                    using (MySqlDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.HasRows)
                        {
                            while (rdr.Read())
                            {
                                Account account = new Account()
                                {
                                    Id = rdr.GetInt32(rdr.GetOrdinal("id")),
                                    Broker = BreakoutManager.brokerManager.GetBroker(rdr.GetInt32(rdr.GetOrdinal("broker_id"))),
                                    Owner = rdr.GetString(rdr.GetOrdinal("owner")),
                                    Number = rdr.GetInt32(rdr.GetOrdinal("number")),
                                    Balance = rdr.GetDouble(rdr.GetOrdinal("balance")),
                                    TerminalTradeAllowed = rdr.GetBoolean(rdr.GetOrdinal("terminal_trade_allowed")),
                                    TradeExpertAllowed = rdr.GetBoolean(rdr.GetOrdinal("trade_expert_allowed")),
                                    TradeAllowed = rdr.GetBoolean(rdr.GetOrdinal("trade_allowed"))
                                };

                                return account;
                            }
                        }
                    }
                }
            }
            return null;
        }

        public int GetAccountGroupId(int accountId, int pairId)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "SELECT * FROM account_groups WHERE account_id=@account_id AND pair_id=@pair_id AND master=@master LIMIT 1";
                    cmd.Parameters.AddWithValue("account_id", accountId);
                    cmd.Parameters.AddWithValue("pair_id", pairId);
                    cmd.Parameters.AddWithValue("master", 1);

                    using (MySqlDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.HasRows)
                        {
                            while (rdr.Read())
                            {
                                return rdr.GetInt32(rdr.GetOrdinal("group_id"));
                            }
                        }
                    }
                }
            }
            return -1;
        }

        public List<AccountGroup> GetAccountGroups(int accountId, int pairId)
        {
            List<AccountGroup> accountGroups = new();

            int group = GetAccountGroupId(accountId, pairId);

            if(group != -1)
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new())
                    {
                        cmd.Connection = conn;
                        cmd.CommandText = "SELECT * FROM account_groups WHERE group_id=@group_id";
                        cmd.Parameters.AddWithValue("group_id", group);

                        using (MySqlDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.HasRows)
                            {
                                while (rdr.Read())
                                {
                                    AccountGroup accountGroup = new()
                                    {
                                        Id = rdr.GetInt32(rdr.GetOrdinal("id")),
                                        Account = BreakoutManager.accountManager.GetAccount(rdr.GetInt32(rdr.GetOrdinal("account_id"))),
                                        Pair = BreakoutManager.pairManager.GetPair(rdr.GetInt32(rdr.GetOrdinal("pair_id"))),
                                        Group = rdr.GetInt32(rdr.GetOrdinal("group_id")),
                                        Master = rdr.GetBoolean(rdr.GetOrdinal("master")),
                                        VolumeMin = rdr.GetDouble(rdr.GetOrdinal("min_volume")),
                                        AllotedPercantage = rdr.GetDouble(rdr.GetOrdinal("alloted_percantage"))
                                    };

                                    accountGroups.Add(accountGroup);
                                }
                            }
                        }
                    }
                }
            }

            return accountGroups;
        }

        public Account? GetAccount(int brokerId, int number)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "SELECT * FROM accounts WHERE broker_id=@broker_id AND number=@number";
                    cmd.Parameters.AddWithValue("broker_id", brokerId);
                    cmd.Parameters.AddWithValue("number", number);

                    using (MySqlDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.HasRows)
                        {
                            while (rdr.Read())
                            {
                                Account account = new Account()
                                {
                                    Id = rdr.GetInt32(rdr.GetOrdinal("id")),
                                    Broker = BreakoutManager.brokerManager.GetBroker(rdr.GetInt32(rdr.GetOrdinal("broker_id"))),
                                    Owner = rdr.GetString(rdr.GetOrdinal("owner")),
                                    Number = rdr.GetInt32(rdr.GetOrdinal("number")),
                                    Balance = rdr.GetDouble(rdr.GetOrdinal("balance")),
                                    TerminalTradeAllowed = rdr.GetBoolean(rdr.GetOrdinal("terminal_trade_allowed")),
                                    TradeExpertAllowed = rdr.GetBoolean(rdr.GetOrdinal("trade_expert_allowed")),
                                    TradeAllowed = rdr.GetBoolean(rdr.GetOrdinal("trade_allowed"))
                                };

                                return account;
                            }
                        }
                    }
                }
            }

            return null;
        }

        public int AddAccount(Account account)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "INSERT INTO accounts (broker_id, number, owner, balance, trade_allowed, trade_expert_allowed, terminal_trade_allowed) VALUES (@broker_id, @number, @owner, @balance, @trade_allowed, @trade_expert_allowed, @terminal_trade_allowed); SELECT LAST_INSERT_ID();";
                    cmd.Parameters.AddWithValue("broker_id", account.Broker.Id);
                    cmd.Parameters.AddWithValue("number", account.Number);
                    cmd.Parameters.AddWithValue("owner", account.Owner);
                    cmd.Parameters.AddWithValue("balance", account.Balance);
                    cmd.Parameters.AddWithValue("trade_allowed", account.TradeAllowed);
                    cmd.Parameters.AddWithValue("trade_expert_allowed", account.TradeExpertAllowed);
                    cmd.Parameters.AddWithValue("terminal_trade_allowed", account.TerminalTradeAllowed);

                    cmd.ExecuteNonQuery();
                    int id = (int)cmd.LastInsertedId;

                    account.Id = id;
                    string debug = String.Format("New account added in DB: {0}",
                            account.ToString()
                        );
                    Utils.SendLog(LoggerService.LoggerType.INFO, debug);

                    return id;
                }
            }
        }

        public void UpdateAccount(Account account)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "UPDATE accounts SET balance=@balance, trade_allowed=@trade_allowed, trade_expert_allowed=@trade_expert_allowed, terminal_trade_allowed=@terminal_trade_allowed WHERE id=@id";
                    cmd.Parameters.AddWithValue("id", account.Id);
                    cmd.Parameters.AddWithValue("balance", account.Balance);
                    cmd.Parameters.AddWithValue("trade_allowed", account.TradeAllowed);
                    cmd.Parameters.AddWithValue("trade_expert_allowed", account.TradeExpertAllowed);
                    cmd.Parameters.AddWithValue("terminal_trade_allowed", account.TerminalTradeAllowed);
                    cmd.ExecuteNonQuery();

                    string debug = String.Format("The account in the DB has been updated: {0}",
                            account.ToString()
                        );
                    Utils.SendLog(LoggerService.LoggerType.INFO, debug);
                }
            }
        }
        #endregion

        #region Pair
        public Pair? GetPair(string symbol, int broker_id)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "SELECT * FROM pairs p INNER JOIN brokers b ON p.broker_id = b.id WHERE p.broker_id=@broker_id AND p.symbol=@symbol";
                    cmd.Parameters.AddWithValue("broker_id", broker_id);
                    cmd.Parameters.AddWithValue("symbol", symbol);

                    using (MySqlDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.HasRows)
                        {
                            while (rdr.Read())
                            {
                                Pair pair = new Pair()
                                {
                                    Id = rdr.GetInt32(rdr.GetOrdinal("id")),
                                    Symbol = (string)rdr["symbol"],
                                    Broker = new Trade.Broker()
                                    {
                                        Id = rdr.GetInt32(rdr.GetOrdinal("broker_id")),
                                        Name = (string)rdr["name"],
                                        PlatformId = rdr.GetInt32(rdr.GetOrdinal("platform_id"))
                                    },
                                    Digits = rdr.GetInt32(rdr.GetOrdinal("digits")),
                                    ContractSize = rdr.GetInt32(rdr.GetOrdinal("contract_size")),
                                    VolumeMin = rdr.GetDouble(rdr.GetOrdinal("min_volume")),
                                    VolumeDecimalCount = rdr.GetInt32(rdr.GetOrdinal("volume_decimal_count")),
                                };

                                return pair;
                            }
                        }
                    }
                }
            }

            return null;
        }

        public Pair? GetPair(int pairId)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "SELECT * FROM pairs p INNER JOIN brokers b ON p.broker_id = b.id WHERE p.id=@id";
                    cmd.Parameters.AddWithValue("id", pairId);

                    using (MySqlDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.HasRows)
                        {
                            while (rdr.Read())
                            {
                                Pair pair = new Pair()
                                {
                                    Id = rdr.GetInt32(rdr.GetOrdinal("id")),
                                    Symbol = (string)rdr["symbol"],
                                    Broker = new Trade.Broker()
                                    {
                                        Id = rdr.GetInt32(rdr.GetOrdinal("broker_id")),
                                        Name = (string)rdr["name"],
                                        PlatformId = rdr.GetInt32(rdr.GetOrdinal("platform_id"))
                                    },
                                    Digits = rdr.GetInt32(rdr.GetOrdinal("digits")),
                                    ContractSize = rdr.GetInt32(rdr.GetOrdinal("contract_size")),
                                    VolumeMin = rdr.GetDouble(rdr.GetOrdinal("min_volume")),
                                    VolumeDecimalCount = rdr.GetInt32(rdr.GetOrdinal("volume_decimal_count")),
                                };

                                return pair;
                            }
                        }
                    }
                }
            }

            return null;
        }

        public int AddPair(Pair pair)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "INSERT INTO pairs (broker_id, symbol, bid, ask, digits, contract_size, min_volume, volume_decimal_count) VALUES (@broker_id, @symbol, @bid, @ask, @digits, @contract_size, @min_volume, @volume_decimal_count); SELECT LAST_INSERT_ID();";
                    cmd.Parameters.AddWithValue("broker_id", pair.Broker.Id);
                    cmd.Parameters.AddWithValue("symbol", pair.Symbol);
                    cmd.Parameters.AddWithValue("bid", pair.Bid);
                    cmd.Parameters.AddWithValue("ask", pair.Ask);
                    cmd.Parameters.AddWithValue("digits", pair.Digits);
                    cmd.Parameters.AddWithValue("contract_size", pair.ContractSize);
                    cmd.Parameters.AddWithValue("min_volume", pair.VolumeMin);
                    cmd.Parameters.AddWithValue("volume_decimal_count", pair.VolumeDecimalCount);

                    cmd.ExecuteNonQuery();
                    int id = (int)cmd.LastInsertedId;

                    pair.Id = id;
                    string debug = String.Format("New pair added in DB: {0}",
                            pair.ToString()
                        );
                    Utils.SendLog(LoggerService.LoggerType.INFO, debug);

                    return id;
                }
            }
        }

        public void UpdatePair(Pair pair)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "UPDATE pairs SET digits=@digits, contract_size=@contract_size, min_volume=@min_volume, volume_decimal_count=@volume_decimal_count WHERE id=@id";
                    cmd.Parameters.AddWithValue("id", pair.Id);
                    //cmd.Parameters.AddWithValue("bid", pair.Bid);
                    //cmd.Parameters.AddWithValue("ask", pair.Ask);
                    cmd.Parameters.AddWithValue("digits", pair.Digits);
                    cmd.Parameters.AddWithValue("contract_size", pair.ContractSize);
                    cmd.Parameters.AddWithValue("min_volume", pair.VolumeMin);
                    cmd.Parameters.AddWithValue("volume_decimal_count", pair.VolumeDecimalCount);

                    cmd.ExecuteNonQuery();

                    string debug = String.Format("The pair in the DB has been updated: {0}",
                            pair.ToString()
                        );
                    Utils.SendLog(LoggerService.LoggerType.INFO, debug);
                }
            }
        }
        #endregion

        #region Order
        public Order? GetOrder(int orderId)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "SELECT * FROM orders WHERE id=@id";
                    cmd.Parameters.AddWithValue("id", orderId);

                    using (MySqlDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.HasRows)
                        {
                            while (rdr.Read())
                            {
                                Order order = new()
                                {
                                    Id = rdr.GetInt32(rdr.GetOrdinal("id")),
                                    Type = (OrderType)rdr.GetInt32(rdr.GetOrdinal("type_id")),
                                    Process = (OrderProcess)rdr.GetInt32(rdr.GetOrdinal("process_id")),
                                    Error = (OrderError)rdr.GetInt32(rdr.GetOrdinal("error_id")),
                                    Ticket = rdr.GetInt32(rdr.GetOrdinal("ticket")),
                                    SendedTime = rdr.GetDateTime(rdr.GetOrdinal("sended_time")),
                                    SendedPrice = rdr.GetDecimal(rdr.GetOrdinal("sended_price")),
                                    OpenTime = rdr.GetDateTime(rdr.GetOrdinal("open_time")),
                                    OpenPrice = rdr.GetDecimal(rdr.GetOrdinal("open_price")),
                                    ClosedTime = rdr.GetDateTime(rdr.GetOrdinal("closed_time")),
                                    ClosedPrice = rdr.GetDecimal(rdr.GetOrdinal("closed_price")),
                                    Volume = rdr.GetDouble(rdr.GetOrdinal("volume")),
                                    Commission = rdr.GetDouble(rdr.GetOrdinal("commission")),
                                    Swap = rdr.GetDouble(rdr.GetOrdinal("swap")),
                                    Profit = rdr.GetDouble(rdr.GetOrdinal("profit"))
                                };

                                return order;
                            }
                        }
                    }
                }
            }

            return null;
        }

        public int AddOrder(Order order)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "INSERT INTO orders (process_id, type_id, error_id, ticket, sended_price, sended_time, open_price, open_time, closed_price, closed_time, volume, commission, swap, profit) VALUES (@process_id, @type_id, @error_id, @ticket, @sended_price, @sended_time, @open_price, @open_time, @closed_price, @closed_time, @volume, @commission, @swap, @profit); SELECT LAST_INSERT_ID();";
                    cmd.Parameters.AddWithValue("process_id", (int)order.Process);
                    cmd.Parameters.AddWithValue("type_id", (int)order.Type);
                    cmd.Parameters.AddWithValue("error_id", (int)order.Error);
                    cmd.Parameters.AddWithValue("ticket", order.Ticket);
                    cmd.Parameters.AddWithValue("sended_price", order.SendedPrice);
                    cmd.Parameters.AddWithValue("sended_time", order.SendedTime);
                    cmd.Parameters.AddWithValue("open_price", order.OpenPrice);
                    cmd.Parameters.AddWithValue("open_time", order.OpenTime);
                    cmd.Parameters.AddWithValue("closed_price", order.ClosedPrice);
                    cmd.Parameters.AddWithValue("closed_time", order.ClosedTime);
                    cmd.Parameters.AddWithValue("volume", order.Volume);
                    cmd.Parameters.AddWithValue("commission", order.Commission);
                    cmd.Parameters.AddWithValue("swap", order.Swap);
                    cmd.Parameters.AddWithValue("profit", order.Profit);

                    cmd.ExecuteNonQuery();
                    int id = (int)cmd.LastInsertedId;
                    return id;
                }
            }
        }

        public void UpdateOrder(Order order)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "UPDATE orders SET process_id=@process_id, error_id=@error_id, ticket=@ticket, sended_price=@sended_price, sended_time=@sended_time, open_price=@open_price, open_time=@open_time, closed_price=@closed_price, closed_time=@closed_time, volume=@volume, commission=@commission, swap=@swap, profit=@profit WHERE id=@id";
                    cmd.Parameters.AddWithValue("id", order.Id);
                    cmd.Parameters.AddWithValue("process_id", (int)order.Process);
                    cmd.Parameters.AddWithValue("error_id", (int)order.Error);
                    cmd.Parameters.AddWithValue("ticket", order.Ticket);
                    cmd.Parameters.AddWithValue("sended_price", order.SendedPrice);
                    cmd.Parameters.AddWithValue("sended_time", order.SendedTime);
                    cmd.Parameters.AddWithValue("open_price", order.OpenPrice);
                    cmd.Parameters.AddWithValue("open_time", order.OpenTime);
                    cmd.Parameters.AddWithValue("closed_price", order.ClosedPrice);
                    cmd.Parameters.AddWithValue("closed_time", order.ClosedTime);
                    cmd.Parameters.AddWithValue("volume", order.Volume);
                    cmd.Parameters.AddWithValue("commission", order.Commission);
                    cmd.Parameters.AddWithValue("swap", order.Swap);
                    cmd.Parameters.AddWithValue("profit", order.Profit);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void UpdateOrderSendedPriceAndTime(int id, DateTime time, decimal price)
        {
            using (MySqlCommand cmd = new())
            {
                cmd.Connection = con;
                cmd.CommandText = "UPDATE orders SET sended_price=@price, sended_time=@time WHERE id=@id";
                cmd.Parameters.AddWithValue("id", id);
                cmd.Parameters.AddWithValue("price", price);
                cmd.Parameters.AddWithValue("time", time);
                cmd.ExecuteNonQuery();
            }
        }

        public void UpdateOrderOpenPriceAndTime(int id, DateTime time, decimal price)
        {
            using (MySqlCommand cmd = new())
            {
                cmd.Connection = con;
                cmd.CommandText = "UPDATE orders SET open_price=@price, open_time=@time WHERE id=@id";
                cmd.Parameters.AddWithValue("id", id);
                cmd.Parameters.AddWithValue("price", price);
                cmd.Parameters.AddWithValue("time", time);
                cmd.ExecuteNonQuery();
            }
        }

        public void UpdateOrderClosedPriceAndTime(int id, DateTime time, decimal price)
        {
            using (MySqlCommand cmd = new())
            {
                cmd.Connection = con;
                cmd.CommandText = "UPDATE orders SET closed_price=@price, closed_time=@time WHERE id=@id";
                cmd.Parameters.AddWithValue("id", id);
                cmd.Parameters.AddWithValue("price", price);
                cmd.Parameters.AddWithValue("time", time);
                cmd.ExecuteNonQuery();
            }
        }

        public void UpdateOrderProfitSwapCommission(int id, double profit, double swap, double commission)
        {
            using (MySqlCommand cmd = new())
            {
                cmd.Connection = con;
                cmd.CommandText = "UPDATE orders SET profit=@profit, swap=@swap, commission=@commission WHERE id=@id";
                cmd.Parameters.AddWithValue("id", id);
                cmd.Parameters.AddWithValue("profit", profit);
                cmd.Parameters.AddWithValue("swap", swap);
                cmd.Parameters.AddWithValue("commission", commission);
                cmd.ExecuteNonQuery();
            }
        }

        public void UpdateOrderVolume(int id, double volume)
        {
            using (MySqlCommand cmd = new())
            {
                cmd.Connection = con;
                cmd.CommandText = "UPDATE orders SET volume=@volume WHERE id=@id";
                cmd.Parameters.AddWithValue("id", id);
                cmd.Parameters.AddWithValue("volume", volume);
                cmd.ExecuteNonQuery();
            }
        }

        public void UpdateOrderType(int id, int type_id)
        {
            using (MySqlCommand cmd = new())
            {
                cmd.Connection = con;
                cmd.CommandText = "UPDATE orders SET type_id=@type_id WHERE id=@id";
                cmd.Parameters.AddWithValue("id", id);
                cmd.Parameters.AddWithValue("type_id", type_id);
                cmd.ExecuteNonQuery();
            }
        }

        public void UpdateOrderProcess(int id, int process_id)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "UPDATE orders SET process_id=@process_id WHERE id=@id";
                    cmd.Parameters.AddWithValue("id", id);
                    cmd.Parameters.AddWithValue("process_id", process_id);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        #endregion

        #region Breakout
        public Breakout? GetBreakout(int breakoutId)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "SELECT * FROM breakouts WHERE id=@id";
                    cmd.Parameters.AddWithValue("id", breakoutId);

                    using (MySqlDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.HasRows)
                        {
                            while (rdr.Read())
                            {
                                Pair pair = pairManager.GetPair(rdr.GetInt32(rdr.GetOrdinal("pair_id")));
                                Account account = accountManager.GetAccount(rdr.GetInt32(rdr.GetOrdinal("account_id")));
                                int magicNumber = rdr.GetInt32(rdr.GetOrdinal("magic_number"));

                                Breakout breakout = new()
                                {
                                    Id = rdr.GetInt32(rdr.GetOrdinal("id")),
                                    AccountPairClient = BreakoutManager.tradeSocketManager.GetAccountPairClient(account, pair, magicNumber),
                                    Type = (BreakoutType)rdr.GetInt32(rdr.GetOrdinal("type_id")),
                                    MagicNumber = magicNumber,
                                    FirstSize = rdr.GetDouble(rdr.GetOrdinal("first_size")),
                                    InputSize = rdr.GetDouble(rdr.GetOrdinal("input_size")),
                                    TPMaxActive = rdr.GetBoolean(rdr.GetOrdinal("tp_max_active")),
                                    TPMaxValue = rdr.GetDouble(rdr.GetOrdinal("tp_max_value")),
                                    TPMinActive = rdr.GetBoolean(rdr.GetOrdinal("tp_min_active")),
                                    TPMinValue = rdr.GetDouble(rdr.GetOrdinal("tp_min_value")),
                                    TSLActive = rdr.GetBoolean(rdr.GetOrdinal("tsl_active")),
                                    TSLValue = rdr.GetDecimal(rdr.GetOrdinal("tsl_value")),
                                    SLMax = rdr.GetDouble(rdr.GetOrdinal("sl_max")),
                                    BoxUpPrice = rdr.GetDecimal(rdr.GetOrdinal("box_up_price")),
                                    BoxDownPrice = rdr.GetDecimal(rdr.GetOrdinal("box_down_price"))
                                };

                                return breakout;
                            }
                        }
                    }
                }
            }
            return null;
        }

        public int AddBreakout(Breakout breakout)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "INSERT INTO breakouts (pair_id, account_id, type_id, magic_number, first_size, input_size, tp_max_active, tp_max_value, tp_min_active, tp_min_value, tsl_active, tsl_value, sl_max, box_up_price, box_down_price) VALUES (@pair_id, @account_id, @type_id, @magic_number, @first_size, @input_size, @tp_max_active, @tp_max_value, @tp_min_active, @tp_min_value, @tsl_active, @tsl_value, @sl_max, @box_up_price, @box_down_price); SELECT LAST_INSERT_ID();";
                    cmd.Parameters.AddWithValue("pair_id", breakout.AccountPairClient.Pair.Id);
                    cmd.Parameters.AddWithValue("account_id", breakout.AccountPairClient.Account.Id);
                    cmd.Parameters.AddWithValue("type_id", (int)breakout.Type);
                    cmd.Parameters.AddWithValue("magic_number", breakout.MagicNumber);
                    cmd.Parameters.AddWithValue("first_size", breakout.FirstSize);
                    cmd.Parameters.AddWithValue("input_size", breakout.InputSize);
                    cmd.Parameters.AddWithValue("tp_max_active", breakout.TPMaxActive);
                    cmd.Parameters.AddWithValue("tp_max_value", breakout.TPMaxValue);
                    cmd.Parameters.AddWithValue("tp_min_active", breakout.TPMinActive);
                    cmd.Parameters.AddWithValue("tp_min_value", breakout.TPMinValue);
                    cmd.Parameters.AddWithValue("tsl_active", breakout.TSLActive);
                    cmd.Parameters.AddWithValue("tsl_value", breakout.TSLValue);
                    cmd.Parameters.AddWithValue("sl_max", breakout.SLMax);
                    cmd.Parameters.AddWithValue("box_up_price", breakout.BoxUpPrice);
                    cmd.Parameters.AddWithValue("box_down_price", breakout.BoxDownPrice);

                    cmd.ExecuteNonQuery();
                    int id = (int)cmd.LastInsertedId;
                    return id;
                }
            }
        }

        public void UpdateBreakout(Breakout breakout)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "UPDATE breakouts SET type_id=@type_id, tp_max_active=@tp_max_active, tp_max_value=@tp_max_value, tp_min_active=@tp_min_active, tp_min_value=@tp_min_value, tsl_active=@tsl_active, tsl_value=@tsl_value, sl_max=@sl_max, box_up_price=@box_up_price, box_down_price=@box_down_price WHERE id=@id";
                    cmd.Parameters.AddWithValue("id", breakout.Id);
                    cmd.Parameters.AddWithValue("type_id", (int)breakout.Type);
                    cmd.Parameters.AddWithValue("tp_max_active", breakout.TPMaxActive);
                    cmd.Parameters.AddWithValue("tp_max_value", breakout.TPMaxValue);
                    cmd.Parameters.AddWithValue("tp_min_active", breakout.TPMinActive);
                    cmd.Parameters.AddWithValue("tp_min_value", breakout.TPMinValue);
                    cmd.Parameters.AddWithValue("tsl_active", breakout.TSLActive);
                    cmd.Parameters.AddWithValue("tsl_value", breakout.TSLValue);
                    cmd.Parameters.AddWithValue("sl_max", breakout.SLMax);
                    cmd.Parameters.AddWithValue("box_up_price", breakout.BoxUpPrice);
                    cmd.Parameters.AddWithValue("box_down_price", breakout.BoxDownPrice);

                    cmd.ExecuteNonQuery();
                }
            }
        }
        #endregion

        #region Transaction
        public int AddTransaction(Transaction transaction)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "INSERT INTO transactions (breakout_id, step) VALUES (@breakout_id, @step); SELECT LAST_INSERT_ID();";
                    cmd.Parameters.AddWithValue("breakout_id", transaction.BreakoutId);
                    cmd.Parameters.AddWithValue("step", transaction.Step);

                    cmd.ExecuteNonQuery();
                    int id = (int)cmd.LastInsertedId;

                    transaction.Id = id;
                    string debug = String.Format("New pair transaction in DB: {0}",
                            transaction.ToString()
                        );
                    Utils.SendLog(LoggerService.LoggerType.INFO, debug);

                    return id;
                }
            }
        }
        #endregion
    }
}
