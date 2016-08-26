using SmartCOM3Lib;
using Collect.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Timers;
using System.Threading;
using System.Threading.Tasks;
using static Collect.Enums;

namespace Collect.Controllers
{
    class DataManager
    {
        DbConnector dbCon;
        Dictionary<string, List<DayTrade>> trades;
        Dictionary<string, DayVolume> volumes;
        string date;
        System.Threading.Timer timerForTrades, timerForVolumes;
        int updateInterval = 5000;

        Dictionary<string, List<DayTrade>> unsavedTrades;
        Dictionary<string, Dictionary<string, DayVolume>> unsavedVolumes;
        bool connectedToDatabase = true;
        System.Timers.Timer connectToDatabaseTimer;
        // Интервал, через который происходит попытка переподключения к базе данных
        double tryConnectInterval = 10000;
        System.Timers.Timer dayEndTimer;

        LogManager logManager;

        public DataManager()
        {
            trades = new Dictionary<string, List<DayTrade>>();
            volumes = new Dictionary<string, DayVolume>();
            unsavedTrades = new Dictionary<string, List<DayTrade>>();
            unsavedVolumes = new Dictionary<string, Dictionary<string, DayVolume>>();
            timerForTrades = new System.Threading.Timer(new TimerCallback(UpdateTradesData), null, Timeout.Infinite, Timeout.Infinite);
            timerForVolumes = new System.Threading.Timer(new TimerCallback(UpdateVolumesData), null, Timeout.Infinite, Timeout.Infinite);
            date = FormDate();
            dbCon = new DbConnector(date);
            StartTransferingData();

            dayEndTimer = new System.Timers.Timer();
            DateTime endDayTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 19, 00, 00);
            try
            {
                dayEndTimer.Interval = (endDayTime - DateTime.Now).TotalMilliseconds;
                dayEndTimer.Elapsed += EndDayHandler;
                dayEndTimer.AutoReset = false;
                dayEndTimer.Start();
            }
            catch
            {

            }

            logManager = new LogManager();
        }

        string FormDate()
        {
            return DateTime.Now.ToString("yyMMdd");
        }

        public void AddData(string security, DateTime time, decimal price, int volume, Direction direction, bool minVolume)
        {
            if (minVolume)
                trades[security].Add(new DayTrade(time, price, volume, direction));

            string minute = time.ToString("HHmm");
            DayVolume vol = volumes[security];
            if (minute != vol.Minute)
            {
                TransferVolumesToDatabase(true, minute, security);
                if (direction == Direction.Buy)
                    volumes[security] = new DayVolume(minute, volume, 0);
                else
                    volumes[security] = new DayVolume(minute, 0, volume);
            }
            else
            {
                if (direction == Direction.Buy)
                    vol.VolumeBuy += volume;
                else
                    vol.VolumeSell += volume;
            }
        }

        public async void TransferTradesToDatabase()
        {
            string[] securities = trades.Keys.ToArray();
            for (int i = 0; i < securities.Length; i++)
            {
                string sec = securities[i];
                List<DayTrade> t = trades[sec];
                trades[sec] = new List<DayTrade>();
                if (connectedToDatabase)
                {
                    if (await dbCon.InsertDayTradesData(sec, t))
                        return;
                }
                foreach (DayTrade dt in t)
                {
                    if (!unsavedTrades.ContainsKey(sec))
                        unsavedTrades.Add(sec, new List<DayTrade>());
                    unsavedTrades[sec].Add(dt);
                }
                ConnectionLost();
            }
        }

        async void TransferUnsavedTradesToDatabase()
        {
            string[] securities = unsavedTrades.Keys.ToArray();
            for (int i = 0; i < securities.Length; i++)
            {
                List<DayTrade> t = unsavedTrades[securities[i]];
                if (await dbCon.InsertDayTradesData(securities[i], t))
                    unsavedTrades.Remove(securities[i]);
                else
                {
                    connectedToDatabase = false;
                    ConnectionLost();
                    return;
                }
            }
        }

        // Сервер-менеджер подписывается на событие для контроля отправки данных на сервер БД
        public event Action<string, int> OnUpdateVolumes;

        public async void TransferVolumesToDatabase(bool newMinute = false, string minute = "", string sec = "")
        {
            string[] keys = volumes.Keys.ToArray();
            if (newMinute)
            {
                timerForVolumes.Change(updateInterval, updateInterval);
            }
            for (int i = 0; i < keys.Length; i++)
            {
                string security = keys[i];
                if (!volumes.ContainsKey(security))
                    continue;
                DayVolume dv = volumes[security];
                int result = 0;
                if (connectedToDatabase)
                {
                    if (await dbCon.ContainsMinute(security, dv.Minute))
                        result = await dbCon.UpdateDayVolumesData(security, dv);
                    else
                        result = await dbCon.InsertDayVolumesData(security, dv);
                    if (result == -1)
                    {
                        connectedToDatabase = false;
                        ConnectionLost();
                    }
                    if (result > 0)
                        OnUpdateVolumes(security, dv.VolumeBuy + dv.VolumeSell);
                }
                if (!connectedToDatabase)
                {
                    if (!unsavedVolumes.ContainsKey(security))
                        unsavedVolumes.Add(security, new Dictionary<string, DayVolume>());
                    if (!unsavedVolumes[security].ContainsKey(dv.Minute))
                        unsavedVolumes[security].Add(dv.Minute, new DayVolume(dv.Minute, 0, 0));
                    unsavedVolumes[security][dv.Minute].VolumeBuy += dv.VolumeBuy;
                    unsavedVolumes[security][dv.Minute].VolumeSell += dv.VolumeSell;
                }
                dv.VolumeBuy = 0;
                dv.VolumeSell = 0;
            }
        }

        void TransferUnsavedVolumesToDatabase()
        {

        }

        public void AddSecurity(string security)
        {
            trades.Add(security, new List<DayTrade>());
            string lastMinute = await dbCon.LastMinute(security);
            volumes.Add(security, new DayVolume(lastMinute, 0, 0));
        }

        public void RemoveSecurity(string security)
        {
            if (trades.ContainsKey(security))
                trades.Remove(security);
            if (volumes.ContainsKey(security))
                volumes.Remove(security);
        }

        void UpdateTradesData(object state)
        {
            TransferTradesToDatabase();
        }

        void UpdateVolumesData(object state)
        {
            TransferVolumesToDatabase();
        }

        public void StartTransferingData()
        {
            timerForTrades.Change(updateInterval, updateInterval);
            timerForVolumes.Change(updateInterval, updateInterval);
        }

        public void StopTransferingData()
        {
            timerForTrades.Change(Timeout.Infinite, Timeout.Infinite);
            timerForVolumes.Change(Timeout.Infinite, Timeout.Infinite);
        }

        void ConnectionLost()
        {
            connectedToDatabase = false;
            if (connectToDatabaseTimer == null)
            {
                connectToDatabaseTimer = new System.Timers.Timer();
                connectToDatabaseTimer.Interval = tryConnectInterval;
                connectToDatabaseTimer.Elapsed += TryConnectToDatabase;
            }
            connectToDatabaseTimer.Start();
        }

        async void TryConnectToDatabase(object sender, ElapsedEventArgs e)
        {
            if (await dbCon.TryConnectToDatabase((int)tryConnectInterval - 5000))
            {
                connectedToDatabase = true;
                connectToDatabaseTimer.Stop();
                TransferUnsavedTradesToDatabase();
                TransferUnsavedVolumesToDatabase();
            }
        }

        public void SetDataStorage(DataStorage ds)
        {
            dbCon?.SetDataStorage(ds);
        }

        void EndDayHandler(object sender, ElapsedEventArgs e)
        {
            dbCon.TransferDayData();
            logManager.Log("Вызов процедуры трансфера данных из дненых таблиц");
        }
    }
}
