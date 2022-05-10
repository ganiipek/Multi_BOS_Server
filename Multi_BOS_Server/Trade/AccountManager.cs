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
    internal class AccountManager
    {
        DatabaseManager databaseManager = BreakoutManager.databaseManager;
        BrokerManager brokerManager = BreakoutManager.brokerManager;

        BaseSocketManager tradeSocketManager = BreakoutManager.tradeSocketManager;
        BaseSocketManager priceSocketManager = BreakoutManager.priceSocketManager;

        static List<Account> accounts = new();
        static List<AccountGroup> accountGroups = new();

        public Account? GetAccount(int accountId)
        {
            lock (accounts)
            {
                if (accounts.Exists(_account => _account.Id == accountId))
                {
                    return accounts.Find(_account => _account.Id == accountId);
                }
                else
                {
                    return databaseManager.GetAccount(accountId);
                }
            }
        }

        public int GetAccountId(Account account)
        {
            Account? oldAccount = accounts.Find(_account => _account.Id == account.Id);
            if (oldAccount != null) return oldAccount.Id;

            Account? SQLAccount = databaseManager.GetAccount(account.Broker.Id, account.Number);
            if (SQLAccount == null)
            {
                int accountId = databaseManager.AddAccount(account);

                return accountId;
            }
            return SQLAccount.Id;
        }

        public void AddAccount(Account account)
        {
            lock (accounts)
            {
                if (!accounts.Exists(_account => _account.Id == account.Id))
                {
                    accounts.Add(account);

                    string debug = String.Format("New account added in accounts: {0}",
                        account.ToString()
                    );
                    Utils.SendLog(LoggerService.LoggerType.DEBUG, debug);
                }
            }
        }

        public void Register(TcpClient client, dynamic json_data)
        {
            Account account = new()
            {
                Broker = brokerManager.GetBroker((int) json_data.broker),
                Owner = (string) json_data.owner,
                Number = (int) json_data.number,
                TerminalTradeAllowed = (bool)json_data.tta,
                TradeExpertAllowed = (bool)json_data.ate,
                TradeAllowed = (bool)json_data.ata,
                Balance = (double)json_data.b
            };

            account.Id = GetAccountId(account);

            AddAccount(account);

            string request = String.Format("\"router\":\"{0}\",\"account_id\":\"{1}\"",
                "register_account",
                account.Id.ToString()
                );
            tradeSocketManager.Send(client, request);
        }

        public List<AccountGroup> GetAccountGroups(int accountId, int pairId)
        {
            return databaseManager.GetAccountGroups(accountId, pairId);
        }

        //public void AddAccountGroup(AccountGroup accountGroup)
        //{
        //    lock(accountGroups)
        //    {
        //        accountGroups.Add(accountGroup);
        //    }
        //}

        //public void RemoveAccountGroup(AccountGroup accountGroup)
        //{
        //    lock(accountGroups)
        //    {
        //        accountGroups.Remove(accountGroup);
        //    }
        //}
    }
}
