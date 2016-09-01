using SmartCOM3Lib;
using Collect.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Timers;
using static Collect.Enums;

namespace Collect.Controllers
{
    class DataManager
    {
		// Объект для работы с БД
        DbConnector dbCon;
		// Все сделки по каждому инструменту (после отправки в БД очищается)
        Dictionary<string, List<DayTrade>> trades;
		// Объем текущей минуты по каждому инструменту (после отправки в БД очищается)
        Dictionary<string, DayVolume> volumes;
		// Таймер для отправки данных в БД
		Timer updateTimer;
		// Период отправки данных в БД
        int updateInterval = 5000;

		// Неотправленные в БД сделки
        Dictionary<string, List<DayTrade>> unsavedTrades;
		// Неотрпавленные в БД объемы
        Dictionary<string, Dictionary<string, DayVolume>> unsavedVolumes;
		// Признак состояния соединения с БД
        bool connectedToDatabase = true;
		// Таймер для автоматического переподключения к БД, если соединение потеряно
        Timer connectToDatabaseTimer;
        // Интервал, через который происходит попытка переподключения к базе данных
        double tryConnectInterval = 10000;
		// Таймер, отсчитывающий время до вызова процедуры переноса данных из дневных таблиц в глобальные
        Timer dayEndTimer;

        LogManager logManager;

        public DataManager()
        {
            trades = new Dictionary<string, List<DayTrade>>();
            volumes = new Dictionary<string, DayVolume>();
            unsavedTrades = new Dictionary<string, List<DayTrade>>();
            unsavedVolumes = new Dictionary<string, Dictionary<string, DayVolume>>();
            dbCon = new DbConnector(DateTime.Now.ToString("yyMMdd"));

			InitUpdateTimer();
			InitDayEndTimer();

            logManager = new LogManager();
        }

		public void AddData(string security, DateTime time, decimal price, double volume, Direction direction, bool minVolume)
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
				//ConnectionLost();
			}
        }

        public async void TransferVolumesToDatabase(bool newMinute = false)
        {
            if (newMinute)
            {
				updateTimer.Stop();
				updateTimer.Start();
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

        public void StartTransferingData()
        {
			updateTimer.Start();
        }

        public void StopTransferingData()
        {
			updateTimer.Stop();
			UpdateData(null, null);
        }

        public void SetDataStorage(DataStorage ds)
        {
            dbCon?.SetDataStorage(ds);
        }

		// Cобытие для контроля отправки данных на сервер БД
		public event Action<string, double> OnUpdateVolumes;

		void InitUpdateTimer()
		{
			updateTimer = new Timer();
			updateTimer.Interval = updateInterval;
			updateTimer.Elapsed += UpdateData;
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

		void EndDayHandler(object sender, ElapsedEventArgs e)
		{
			dbCon.TransferDayData();
			logManager.Log("Вызов процедуры трансфера данных из дненых таблиц");
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

		void TransferUnsavedVolumesToDatabase()
		{

		}

		void UpdateData(object sender, ElapsedEventArgs e)
		{
			TransferTradesToDatabase();
			TransferVolumesToDatabase();
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
	}
}
