using SmartCOM3Lib;
using Collect.Models;
using Collect.Others;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Collect.Controllers
{
    class ServerManager
    {
		StServerClass server;

        // Список всех инструментов
        List<Security> securities;

        bool isConnected;
        bool securitiesRecieved;

        public ServerManager()
        {
			securities = new List<Security>();
			InitializeServer();
        }

        void InitializeServer()
        {
            server = new StServerClass();

            server.Connected += Handler_Connected;
            server.Disconnected += Handler_Disconnected;
            server.AddSymbol += Handler_NewSecurities;
            server.AddTick += Handler_NewTrades;
        }

        #region Handlers

        void Handler_Connected()
        {
            isConnected = true;
            if (!securitiesRecieved)
                server.GetSymbols();

            OnConnect();
        }

        void Handler_Disconnected(string reason)
        {
            isConnected = false;

            OnDisconnect(reason);
        }

        void Handler_NewSecurities(int row, int nrows, string symbol, string shortName, string longName,
            string type, int decimals, int lotSize, double punkt, double step,
            string secExtId, string secExchName, DateTime expiryDate, double daysBeforeExpiry, double strike)
        {
            securities.Add(new Security(symbol, shortName, longName,
                type, decimals, lotSize, punkt, step,
                secExtId, secExchName, expiryDate, daysBeforeExpiry));
            securitiesRecieved = true;
        }

        void Handler_NewTrades(string securityCode, DateTime time,
            double price, double volume, string tradeId, StOrder_Action direction)
        {
			OnTradeReceive(securityCode, time, price, volume, Enums.ToDirection(direction));
        }

        #endregion

        public void Connect(string login, byte[] password, ServerType serverType)
        {
            if (!isConnected)
                ThreadPool.QueueUserWorkItem((w) => 
                    server.connect(SmartComAddresses.GetAddress(serverType), SmartComAddresses.Port,
                    login, Common.DecryptPassword(password)));
        }

        public void Disconnect()
        {
			// Обязательно ли отменять подписку на трейды, если начнем отсоединение
            //string[] keys = trackingSecurities.Keys.ToArray();
            //for (int i = 0; i < keys.Length; i++)
            //    server.CancelTicks(keys[i]);
            if (isConnected)
                ThreadPool.QueueUserWorkItem((w) => server.disconnect());
        }

        public bool IsConnected
        {
            get { return isConnected; }
        }

        public void CleanUp()
        {
            if (server.IsConnected())
            {
                server.AddTick -= Handler_NewTrades;
                server.AddSymbol -= Handler_NewSecurities;
                server.Disconnected -= Handler_Disconnected;
                ThreadPool.QueueUserWorkItem((w) => server.disconnect());
            }
        }

        public List<Security> GetSecuritiesList()
        {
            return securities;
        }

		public void StartListenSecurity(string security)
		{
			if (isConnected)
				server.ListenTicks(security);
		}

        public event Action OnConnect;

        public event Action<string> OnDisconnect;

		public event Action<string, DateTime, double, double, Enums.Direction> OnTradeReceive;
    }
}
