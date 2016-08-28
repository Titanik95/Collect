using Collect.Models;
using Collect.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;

namespace Collect.Controllers
{
    public class MainController
    {
		// Главное окно
		Window mainWindow;
		// Менеджер сервера брокера
		ServerManager serverManager;
		// Менеджер данных
		DataManager dataManager;
		// Окно подключения
		ConnectWindow cw;
		// Список отслеживаемых инструментов
		Dictionary<string, TrackingSecurity> trackingSecurities;
		// Таймер переподключения
		Timer autoReconnectTimer;
		// Лог-менеджер
		LogManager logManager;
		// Параметры
		Parameters parameters;

		// Подключен ли к серверу
		public bool IsConnected { get { return serverManager.IsConnected; } }

		public MainController(MainWindow m)
		{
			mainWindow = m;
			serverManager = new ServerManager();
			dataManager = new DataManager();
			logManager = new LogManager();

			InitParameters();
			InitServerManager();
			InitTrackingSecurities();
			InitTimers();

			dataManager.OnUpdateVolumes += OnVolumesUpdate;
			dataManager.SetDataStorage(parameters.DataStorage);
		}

		/// <summary>
		/// Инициализация отслеживаемых инструментов из файла
		/// </summary>
		void InitTrackingSecurities()
		{
			using (FileStream fs = new FileStream(Properties.Resources.SecuritiesFileName, FileMode.OpenOrCreate))
			{
				BinaryFormatter bf = new BinaryFormatter();

				try
				{
					trackingSecurities = ((Dictionary<string, TrackingSecurity>)bf.Deserialize(fs));
				}
				catch (Exception ex)
				{
					trackingSecurities = new Dictionary<string, TrackingSecurity>();
					logManager.Log("Ошибка десериализации инструментов", ex.Message);
				}
			}
			foreach (var ts in trackingSecurities)
				OnTrackingSecurityAdd(ts.Value);
		}

		/// <summary>
		/// Инициализация параметров из файла
		/// </summary>
		void InitParameters()
		{
            using (FileStream fs = new FileStream(Properties.Resources.ParamatersFileName, FileMode.OpenOrCreate))
			{
				if (fs != null)
				{
					BinaryFormatter bf = new BinaryFormatter();
					try
					{
						parameters = (Parameters)bf.Deserialize(fs);
					}
					catch
					{
						parameters = new Parameters();
					}
				}
				else
					parameters = new Parameters();
			}
		}

		/// <summary>
		/// Инициализация сервер-менеджера
		/// </summary>
		void InitServerManager()
		{
			serverManager = new ServerManager();
			serverManager.OnConnect += OnServerConnect;
			serverManager.OnDisconnect += OnServerDisconnect;
		}

		/// <summary>
		/// Инициализация таймера переподключения
		/// </summary>
		void InitTimers()
		{
			autoReconnectTimer = new Timer();
			autoReconnectTimer.Interval = parameters.AutoReconnectTime * 1000;
			autoReconnectTimer.AutoReset = false;
			autoReconnectTimer.Elapsed += OnReconnectTimer;
		}

		public List<Security> GetSecuritiesList()
		{
			return serverManager.GetSecuritiesList();
		}

		public bool AddSecurity(Security security)
		{
			if (trackingSecurities.ContainsKey(security.Code))
				return false;

			TrackingSecurity ts = new TrackingSecurity(security, 0, 1000);
			trackingSecurities.Add(security.Code, ts);
			// Если подключены к серверу, то начать прослушивать данный инструмент
			serverManager.StartListenSecurity(security.Code);
			dataManager.AddSecurity(security.Code);
			// Уведомление главного окна о добавлении нового отслеживаемого инструмента
			OnTrackingSecurityAdd(ts);

			return true;
		}

		public void ShowConnectWindow()
		{
			if (cw == null)
				cw = new ConnectWindow();
			cw.Close();
			cw.Owner = mainWindow;
			cw.ShowDialog();
		}

		public void CloseConnectWindow()
		{
			cw?.Close();
		}

		public void CleanUpServerManager()
		{
			serverManager.CleanUp();
		}

		public void ConnectToServer()
		{
			if (!serverManager.IsConnected)
			{
				autoReconnectTimer.Stop();
				serverManager?.Connect(parameters.Login, parameters.Password, parameters.ServerType);
				cw?.Close();
				cw = new ConnectWindow();
				cw.Owner = mainWindow;
				cw.Show();
			}
			else
				serverManager.Disconnect();
		}

		public void RemoveTrackingSecurity(string security)
		{
			trackingSecurities.Remove(security);
			dataManager.RemoveSecurity(security);
		}

		public void OpenSecurityPickerWindow()
		{
			SecurityPicker sp = new SecurityPicker(this);
			sp.ShowDialog();
		}

		public List<Security> GetSecurities()
		{
			return serverManager.GetSecuritiesList();
		}

		public void AutoConnect()
		{
			if (parameters.AutoConnect)
				ConnectToServer();
		}

		public void SaveTrackingSecurities()
		{
			using (FileStream fs = new FileStream(Properties.Resources.SecuritiesFileName, FileMode.OpenOrCreate))
			{
				BinaryFormatter bf = new BinaryFormatter();
				try
				{
					bf.Serialize(fs, trackingSecurities);
				}
				catch (Exception ex)
				{
					logManager.Log("Ошибка сериализации отслеживаемых инструментов", ex.Message);
				}
			}
		}

		public void UpdateParameters()
		{
			dataManager.SetDataStorage(parameters.DataStorage);
		}

		public void OpenParametersWindow()
		{
			ParametersWindow pw = new ParametersWindow(parameters, this);
			pw.ShowDialog();
		}

		void OnServerConnect()
		{
			logManager.Log("Подключен к серверу");
			OnServerConnectEvent(true);
			foreach (var ts in trackingSecurities)
				serverManager.StartListenSecurity(ts.Key);
			// Начать передачу данных в БД
			dataManager.StartTransferingData();
		}

		void OnServerDisconnect(string reason)
		{
			if (reason != "Disconnected by user" && parameters.AutoReconnect)
				autoReconnectTimer.Start();
			logManager.Log("Отключен от сервера", reason);
			OnServerConnectEvent(false);
			// Прекратить передачу данных в БД
			dataManager.StopTransferingData();
		}

		void OnTradeReceive(string security, DateTime time, double price, double volume, Enums.Direction direction)
		{
			TrackingSecurity ts = trackingSecurities[security];
            int vol = (int)volume / ts.Security.LotSize;
			bool minVolume = vol >= ts.MinimumVolume ? true : false;
            dataManager.AddData(security, time, (decimal)price, vol, direction, minVolume);
		}

		void OnReconnectTimer(object sender, ElapsedEventArgs e)
		{
			autoReconnectTimer.Interval = parameters.AutoReconnectTime * 1000;
			if (parameters.AutoReconnect)
				ConnectToServer();
			logManager.Log("Автоматическое переподключение к серверу");
		}

		void OnVolumesUpdate(string security, int volumeSent)
		{
			if (trackingSecurities.ContainsKey(security))
				trackingSecurities[security].VolumeSent += volumeSent;
		}

		public event Action<bool> OnServerConnectEvent;

		public event Action<TrackingSecurity> OnTrackingSecurityAdd;
    }
}
