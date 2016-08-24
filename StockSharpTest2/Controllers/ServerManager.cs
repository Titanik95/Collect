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
        DataManager dataManager;
        StServerClass server;

        // Список всех инструментов
        List<Security> securities;
        // Список отслеживаемых инструментов
        Dictionary<string, TrackingSecurity> trackingSecurities;

        bool isConnected;
        bool securitiesRecieved;

        public ServerManager()
        {
            dataManager = new DataManager();
            securities = new List<Security>();
            trackingSecurities = new Dictionary<string, TrackingSecurity>();
            InitializeServer();
        }

        void InitializeServer()
        {
            server = new StServerClass();

            server.Connected += Handler_Connected;
            server.Disconnected += Handler_Disconnected;
            server.AddSymbol += Handler_NewSecurities;
            server.AddTick += Handler_NewTrades;

            dataManager.OnUpdateVolumes += VolumesUpdate;
        }

        #region Handlers

        void Handler_Connected()
        {
            isConnected = true;
            if (!securitiesRecieved)
                server.GetSymbols();

            string[] keys = trackingSecurities.Keys.ToArray();
            for (int i = 0; i < keys.Length; i++)
                server.ListenTicks(keys[i]);

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
            if (trackingSecurities[securityCode].Tracking)
            {
                bool minVolume = volume / trackingSecurities[securityCode].Security.LotSize >=
                    trackingSecurities[securityCode].MinimumVolume ? true : false;
                dataManager.AddData(securityCode, time, (decimal)price,
                   (int)volume / trackingSecurities[securityCode].Security.LotSize, Enums.ToDirection(direction), minVolume);
                trackingSecurities[securityCode].VolumeRecieved += (int)volume;
            }
        }
        
        void VolumesUpdate(string security, int volumeSent)
        {
            if (trackingSecurities.ContainsKey(security))
                trackingSecurities[security].VolumeSent += volumeSent;
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
            string[] keys = trackingSecurities.Keys.ToArray();
            for (int i = 0; i < keys.Length; i++)
                server.CancelTicks(keys[i]);
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
                dataManager.OnUpdateVolumes -= VolumesUpdate;
                ThreadPool.QueueUserWorkItem((w) => server.disconnect());
            }
        }

        public List<Security> GetSecuritiesList()
        {
            return securities;
        }

        public async Task<bool> AddTrackingSecurity(TrackingSecurity ts)
        {
            if (trackingSecurities.ContainsKey(ts.Security.Code))
                return false;
            else
            {
                if (await dataManager.AddSecurity(ts.Security.Code))
                {
                    if (isConnected)
                        server.ListenTicks(ts.Security.Code);
                    trackingSecurities.Add(ts.Security.Code, ts);
                    return true;
                }
                return false;
            }
        }

        public void RemoveTrackingSecurity(string securityCode)
        {
            trackingSecurities.Remove(securityCode);
            dataManager.RemoveSecurity(securityCode);
        }

        public void UpdateVolumeSent(string security, int volumeSent)
        {
            trackingSecurities[security].VolumeSent += volumeSent;
        }

        public event Action OnConnect;

        public event Action<string> OnDisconnect;

        public void SetDataStorage(DataStorage ds)
        {
            dataManager?.SetDataStorage(ds);
        }
    }
}
