using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Multi_BOS_Server.Socket;
using Multi_BOS_Server.Database;

namespace Multi_BOS_Server.Trade
{
    internal class OrderManager
    {
        static DatabaseManager databaseManager = BreakoutManager.databaseManager;

        static BrokerManager brokerManager = BreakoutManager.brokerManager;
        static PairManager pairManager = BreakoutManager.pairManager;
        static AccountManager accountManager = BreakoutManager.accountManager;

        static BaseSocketManager tradeSocketManager = BreakoutManager.tradeSocketManager;
        static BaseSocketManager priceSocketManager = BreakoutManager.priceSocketManager;
        static BaseSocketManager orderSocketManager = BreakoutManager.orderSocketManager;

        static List<Order> orders = new();

        public void AddOrder(Order order)
        {
            lock (orders)
            {
                if (!orders.Exists(_order => _order.Id == order.Id))
                {
                    orders.Add(order);

                    string debug = String.Format("OrderManager (AddOrder): {0}",
                        order.ToSummary()
                    );
                    Utils.SendLog(LoggerService.LoggerType.DEBUG, debug);
                }
            }
        }

        public Order? GetOrder(int orderId)
        {
            lock (orders)
            {
                return orders.Find(_order => _order.Id == orderId);
            }
        }

        public void RemoveOrder(Order order)
        {
            lock (orders)
            {
                orders.Remove(order);

                string debug = String.Format("OrderManager (RemoveOrder): {0}",
                        order.ToSummary()
                    );
                Utils.SendLog(LoggerService.LoggerType.DEBUG, debug);
            }
        }

        void Controller()
        {
            string debug = "Order Manager Controller Starting...";
            Utils.SendLog(LoggerService.LoggerType.SUCCESS, debug);

            while (true)
            {
                foreach(Order order in orders.ToList())
                {
                    if(order.Process == OrderProcess.SEND_OPEN)
                    {
                        if (order.Error != OrderError.NOT_ERROR)
                        {
                            string debug2 = String.Format("OrderManager (Controller) --> There was an error in the order. Send again. ({0})",
                                order.ToSummary()
                            );
                            Utils.SendLog(LoggerService.LoggerType.WARNING, debug2);

                            SocketSend_OrderSend(order);
                        }
                        else if((DateTime.Now - order.LastControl).TotalSeconds >= 1)
                        {
                            SocketSend_OrderInfo(order);
                        }
                    }
                    else if(order.Process == OrderProcess.IN_PROCESS)
                    {
                        if((DateTime.Now - order.LastControl).TotalSeconds >= 10)
                        {
                            order.LastControl = DateTime.Now;

                            string debug2 = String.Format("OrderManager (Controller) --> The order has not been heard from for more than 10 seconds. It's being checked. ({0})",
                                order.ToSummary()
                            );
                            Utils.SendLog(LoggerService.LoggerType.WARNING, debug2);

                            SocketSend_OrderInfo(order);
                        }
                    }
                    else if(order.Process == OrderProcess.SEND_CLOSE)
                    {
                        if (order.Error != OrderError.NOT_ERROR)
                        {
                            string debug2 = String.Format("OrderManager (Controller) --> There was an error in the order. Close again. ({0})",
                                order.ToSummary()
                            );
                            Utils.SendLog(LoggerService.LoggerType.WARNING, debug2);

                            SocketSend_OrderClose(order);
                        }
                        else if((DateTime.Now - order.LastControl).TotalSeconds >= 2)
                        {
                            order.LastControl = DateTime.Now;

                            string debug2 = String.Format("OrderManager (Controller) --> The order has not been heard from for more than 2 seconds. Close again. ({0})",
                                order.ToSummary()
                            );
                            Utils.SendLog(LoggerService.LoggerType.WARNING, debug2);

                            SocketSend_OrderClose(order);
                        }
                    }
                    else if (order.Process == OrderProcess.CLOSED)
                    {
                        order.LastControl = DateTime.Now;

                        string debug2 = String.Format("OrderManager (Controller) --> Order is successfully closed. It is removed from the list.  ({0})",
                                order.ToSummary()
                            );
                        Utils.SendLog(LoggerService.LoggerType.DEBUG, debug2);

                        RemoveOrder(order);
                    }
                }
                //Console.WriteLine("orders count: " + orders.Count.ToString());
                Thread.Sleep(1000);
            }
        }

        public void ControllerStart()
        {
            new Thread(new ThreadStart(Controller)).Start();
        }

        public void UpdateOrderSendedPriceAndTime(int id, DateTime time, decimal price)
        {
            databaseManager.UpdateOrderSendedPriceAndTime(id, time, price);
        }

        public void UpdateOrderSendedPriceAndTime(TcpClient client, dynamic json_data)
        {
            int id = (int)json_data.order_id;
            DateTime time = (DateTime)json_data.time;
            decimal price = (decimal)json_data.price;

            UpdateOrderSendedPriceAndTime(id, time, price);
        }

        public void UpdateOrderOpenPriceAndTime(int id, DateTime time, decimal price)
        {
            databaseManager.UpdateOrderOpenPriceAndTime(id, time, price);
        }

        public void UpdateOrderOpenPriceAndTime(TcpClient client, dynamic json_data)
        {
            int id = (int)json_data.order_id;
            DateTime time = (DateTime)json_data.time;
            decimal price = (decimal)json_data.price;

            UpdateOrderOpenPriceAndTime(id, time, price);
        }

        public void UpdateOrderClosedPriceAndTime(int id, DateTime time, decimal price)
        {
            databaseManager.UpdateOrderClosedPriceAndTime(id, time, price);
        }

        public void UpdateOrderClosedPriceAndTime(TcpClient client, dynamic json_data)
        {
            int id = (int)json_data.order_id;
            DateTime time = (DateTime)json_data.time;
            decimal price = (decimal)json_data.price;

            UpdateOrderClosedPriceAndTime(id, time, price);
        }

        public void UpdateOrderProfitSwapCommission(int id, double profit, double swap, double commission)
        {
            databaseManager.UpdateOrderProfitSwapCommission(id, profit, swap, commission);
        }

        public void UpdateOrderProfitSwapCommission(TcpClient client, dynamic json_data)
        {
            int id = (int)json_data.order_id;
            double profit = (double)json_data.profit;
            double swap = (double)json_data.swap;
            double commission = (double)json_data.commission;

            UpdateOrderProfitSwapCommission(id, profit, swap, commission);
        }

        public void UpdateOrderVolume(int id, double volume)
        {
            databaseManager.UpdateOrderVolume(id, volume);
        }

        public void UpdateOrderVolume(TcpClient client, dynamic json_data)
        {
            int id = (int)json_data.order_id;
            double volume = (double)json_data.volume;

            UpdateOrderVolume(id, volume);
        }

        public void UpdateOrderType(int id, int type_id)
        {
            databaseManager.UpdateOrderType(id, type_id);
        }

        public void UpdateOrderType(TcpClient client, dynamic json_data)
        {
            int id = (int)json_data.order_id;
            int type_id = (int)json_data.type_id;

            UpdateOrderType(id, type_id);
        }

        public void UpdateOrderProcess(int id, int process_id)
        {
            databaseManager.UpdateOrderProcess(id, process_id);
        }

        public void UpdateOrderProcess(TcpClient client, dynamic json_data)
        {
            int id = (int)json_data.order_id;
            int process_id = (int)json_data.process_id;

            UpdateOrderProcess(id, process_id);
        }

        public void Register(TcpClient client, dynamic json_data)
        {
            Order order = new()
            {
                Type = (OrderType) json_data.type_id,
                Process = (OrderProcess) json_data.process_id,
                BreakoutType = (OrderBreakoutType) json_data.breakout_type_id,
                Error = (OrderError) json_data.error_id,
                Ticket = (int) json_data.ticket,
                SendedTime = Utils.UnixTimeStampToDateTime(json_data.sended_time),
                SendedPrice = (decimal) json_data.sended_price,
                OpenTime = Utils.UnixTimeStampToDateTime(json_data.open_time),
                OpenPrice = (decimal)json_data.open_price,
                Volume = (double) json_data.volume,
                Commission = (double) json_data.commission,
                Swap = (double) json_data.swap,
                Profit = (double) json_data.profit
            };

            if((decimal)json_data.closed_price > 0)
            {
                order.ClosedTime = Utils.UnixTimeStampToDateTime(json_data.closed_time);
                order.ClosedPrice = (decimal)json_data.closed_price;
            }

            int newOrderId = databaseManager.AddOrder(order);
            order.Id = newOrderId;

            string request = String.Format("\"router\":\"{0}\",\"ticket\":\"{1}\",\"order_id\":\"{2}\"",
                "register_order",
                order.Ticket.ToString(),
                order.Id.ToString()
                );

            orderSocketManager.Send(client, request);

            databaseManager.AddOrder(order);
        }

        public Order OrderCreate(AccountPairClient accountPairClient, OrderType type, OrderBreakoutType breakoutType, double volume)
        {
            Order order = new()
            {
                AccountPairClient = accountPairClient,
                Ticket = -1,
                Type = type,
                Process = OrderProcess.PREPARED,
                BreakoutType = breakoutType,
                Error = OrderError.NOT_ERROR,
                SendedTime = DateTime.MinValue,
                SendedPrice = 0,
                OpenTime = DateTime.MinValue,
                OpenPrice = 0,
                ClosedTime = DateTime.MinValue,
                ClosedPrice = 0,
                Volume = volume,
                Commission = 0,
                Swap = 0,
                Profit = 0
            };
            int newOrderId = databaseManager.AddOrder(order);
            order.Id = newOrderId;

            AddOrder(order);

            return order;
        }

        public void OrderClose(Order order)
        {
            lock(orders)
            {
                if(!orders.Exists(_order => _order == order))
                {
                    orders.Add(order);
                }
                SocketSend_OrderClose(order);
            }
        }

        public void SocketSend_OrderSend(Order order)
        {
            string request = String.Format("\"router\":\"{0}\",\"order_id\":\"{1}\",\"trade_type\":\"{2}\",\"volume\":\"{3}\"",
                "order_send",
                order.Id.ToString(),
                ((int) order.Type).ToString(),
                order.Volume.ToString().Replace(',', '.')
                );

            BreakoutManager.orderSocketManager.Send(order.AccountPairClient.Client, request);

            order.Process = OrderProcess.SEND_OPEN;
            order.SendedTime = DateTime.Now;

            string debug = String.Format("OrderManager (SocketSend_OrderSend) --> {0}",
                        order.ToSummary()
                    );
            Utils.SendLog(LoggerService.LoggerType.DEBUG, debug);
        }

        public void SocketSend_OrderInfo(Order order)
        {
            string request;

            if (order.Ticket == -1)
            {
                request = String.Format("\"router\":\"{0}\",\"order_id\":\"{1}\"",
                    "order_info_id",
                    order.Id.ToString()
                );
            }
            else
            {
                request = String.Format("\"router\":\"{0}\",\"order_id\":\"{1}\",\"order_ticket\":\"{2}\"",
                    "order_info_ticket",
                    order.Id.ToString(),
                    order.Ticket.ToString()
                );
            }

            BreakoutManager.orderSocketManager.Send(order.AccountPairClient.Client, request);
        }

        void SocketSend_OrderClose(Order order)
        {
            string request;

            if (order.Ticket == -1)
            {
                request = String.Format("\"router\":\"{0}\",\"order_id\":\"{1}\"",
                    "order_close_id",
                    order.Id.ToString()
                );
            }
            else
            {
                request = String.Format("\"router\":\"{0}\",\"order_id\":\"{1}\",\"order_ticket\":\"{2}\"",
                    "order_close_ticket",
                    order.Id.ToString(),
                    order.Ticket.ToString()
                );
            }

            BreakoutManager.orderSocketManager.Send(order.AccountPairClient.Client, request);

            order.Process = OrderProcess.SEND_CLOSE;

            string debug = String.Format("OrderManager (SocketSend_OrderClose) --> {0}",
                        order.ToSummary()
                    );
            Utils.SendLog(LoggerService.LoggerType.DEBUG, debug);
        }

        public void SocketReceive_OrderSend(TcpClient client, dynamic json_data)
        {
            int orderId = (int)json_data.order_id;
            Order? order = GetOrder(orderId);

            if(order == null)
            {
                string debug = String.Format("OrderManager (SocketReceive_OrderSend) --> Order '#{0}' is not found!",
                        order.Id.ToString()
                    );
                Utils.SendLog(LoggerService.LoggerType.WARNING, debug);
            }
            else
            {
                if((bool)json_data.error)
                {
                    order.Error = OrderError.ORDER_NOT_FOUND;

                    string debug = String.Format("OrderManager (SocketReceive_OrderSend) --> Order '#{0}' is not opened! Error Code: {1}",
                        order.Id.ToString(),
                        ((int)json_data.code).ToString()
                    );
                    Utils.SendLog(LoggerService.LoggerType.WARNING, debug);
                }
                else
                {
                    order.Ticket = (int)json_data.ticket;
                    order.SendedPrice = (decimal)json_data.sended_price;
                    order.OpenTime = Utils.UnixTimeStampToDateTime((ulong)json_data.open_time);
                    order.OpenPrice = (decimal)json_data.open_price;
                    order.Volume = (double)json_data.volume;
                    order.Commission = (double)json_data.commission;
                    order.Process = OrderProcess.IN_PROCESS;
                    order.LastControl = DateTime.Now;

                    string debug = String.Format("OrderManager (SocketReceive_OrderSend) --> Order is saved. {0}",
                        order.ToString()
                    );
                    Utils.SendLog(LoggerService.LoggerType.DEBUG, debug);
                }
            }
        }

        public void SocketReceive_OrderInfoByTicket(TcpClient client, dynamic json_data)
        {
            int orderId = (int)json_data.order_id;
            Order? order = GetOrder(orderId);

            if (order == null)
            {
                string debug = String.Format("OrderManager (SocketReceive_OrderInfoByTicket) --> Order '#{0}' is not found!",
                        order.Id.ToString()
                    );
                Utils.SendLog(LoggerService.LoggerType.WARNING, debug);
            }
            else
            {
                if ((bool)json_data.error)
                {
                    order.Error = OrderError.ORDER_NOT_FOUND;

                    string debug = String.Format("OrderManager (SocketReceive_OrderInfoByTicket) --> Order '#{0}' is not found by broker!",
                        order.Id.ToString()
                    );
                    Utils.SendLog(LoggerService.LoggerType.WARNING, debug);
                }
                else
                {
                    order.Type = (OrderType)((int)json_data.order_type);
                    order.OpenTime = Utils.UnixTimeStampToDateTime((ulong)json_data.open_time);
                    order.OpenPrice = (decimal)json_data.open_price;
                    order.Commission = (double)json_data.commission;
                    order.Swap = (double)json_data.swap;
                    order.Volume = (double)json_data.volume;
                    order.Profit = (double)json_data.profit;
                    order.LastControl = DateTime.Now;

                    int orderClosedTime = (int)json_data.closed_time;
                    if(orderClosedTime > 0)
                    {
                        order.Process = OrderProcess.CLOSED;
                        order.ClosedTime = Utils.UnixTimeStampToDateTime((ulong)json_data.closed_time);
                        order.ClosedPrice = (decimal)json_data.closed_price;
                    }
                    else
                    {
                        order.Process = OrderProcess.IN_PROCESS;
                    }
                    

                    string debug = String.Format("OrderManager (SocketReceive_OrderInfoByTicket) --> Order is updated. {0}",
                        order.ToString()
                    );
                    Utils.SendLog(LoggerService.LoggerType.DEBUG, debug);
                }
            }
        }

        public void SocketReceive_OrderInfoUpdate(TcpClient client, dynamic json_data)
        {
            int orderId = (int)json_data.order_id;
            Order? order = GetOrder(orderId);

            if (order == null)
            {
                string debug = String.Format("OrderManager (SocketReceive_OrderInfoUpdate) --> Order '#{0}' is not found!",
                        orderId.ToString()
                    );
                Utils.SendLog(LoggerService.LoggerType.WARNING, debug);
            }
            else
            {
                order.Commission = (double)json_data.commission;
                order.Swap = (double)json_data.swap;
                order.Volume = (double)json_data.volume;
                order.Profit = (double)json_data.profit;
                order.LastControl = DateTime.Now;

                //string debug = String.Format("OrderManager (SocketReceive_OrderInfoUpdate) --> Order is updated. {0}",
                //        order.ToString()
                //    );
                //Utils.SendLog(LoggerService.LoggerType.DEBUG, debug);
            }
        }
    }
}
