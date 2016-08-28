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
            dbCon = new DbConnector(FormDate());

			InitDayEndTimer();

            logManager = new LogManager();
        }

		void InitDayEndTimer()
		{
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
		}

        string FormDate()
        {
            return DateTime.Now.ToString("yyMMdd");
        }

        public void AddData(string security, DateTime time, decimal price, int volume, Direction direction, bool minVolume)
        {
            if (minVolume)
                trades[security].Add(new DayTrade(time, price, volume, direction));

            string currentMinute = time.ToString("HHmm");
            DayVolume vol = volumes[security];
            if (currentMinute != vol.Minute)
            {
                TransferVolumesToDatabase(true);
				vol.Minute = currentMinute;
				vol.VolumeBuy = vol.VolumeSell = 0;
            }
			if (direction == Direction.Buy)
				vol.VolumeBuy += volume;
			else
				vol.VolumeSell += volume;
		}

        public async void TransferTradesToDatabase()
        {
            foreach (var sec in trades.Keys.ToArray())
			{
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

        // Cобытие для контроля отправки данных на сервер БД
        public event Action<string, int> OnUpdateVolumes;

        public async void TransferVolumesToDatabase(bool newMinute = false)
        {
            if (newMinute)
            {
                timerForVolumes.Change(updateInterval, updateInterval);
            }
			foreach (var sec in volumes.Keys.ToArray())
            {
                DayVolume dv = volumes[sec];
                int result = 0;
                if (connectedToDatabase)
                {
					result = await dbCon.InsertDayVolumesData(sec, dv);
                    if (result == -1)
                    {
                        connectedToDatabase = false;
                        ConnectionLost();
                    }
                    if (result > 0)
                        OnUpdateVolumes(sec, dv.VolumeBuy + dv.VolumeSell);
                }
                if (!connectedToDatabase)
                {
                    if (!unsavedVolumes.ContainsKey(sec))
                        unsavedVolumes.Add(sec, new Dictionary<string, DayVolume>());
                    if (!unsavedVolumes[sec].ContainsKey(dv.Minute))
                        unsavedVolumes[sec].Add(dv.Minute, new DayVolume(dv.Minute, 0, 0));
                    unsavedVolumes[sec][dv.Minute].VolumeBuy += dv.VolumeBuy;
                    unsavedVolumes[sec][dv.Minute].VolumeSell += dv.VolumeSell;
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
            volumes.Add(security, new DayVolume("1000", 0, 0));
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
            timerForTrades.Change(0, Timeout.Infinite);
            timerForVolumes.Change(0, Timeout.Infinite);
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
