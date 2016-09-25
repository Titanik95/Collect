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
using System.Windows.Threading;

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
		// Открыто ли окно подключения к серверу
		bool isConnectWindowOpen;

		// Подключен ли к серверу
		public bool IsConnected { get { return serverManager.IsConnected; } }



		public MainController(MainWindow m)
		{
			mainWindow = m;
			serverManager = new ServerManager();
			dataManager = new DataManager();
			logManager = new LogManager();
			cw = new ConnectWindow();

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
				dataManager.AddSecurity(ts.Key);
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
			serverManager.OnTradeReceive += OnTradeReceive;
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

		public void GetTrackingSecurities()
		{
			foreach (var ts in trackingSecurities.Values)
				OnTrackingSecurityAdd(ts);
		}

		public bool AddSecurity(Security security)
		{
			if (trackingSecurities.ContainsKey(security.Code))
				return false;

			TrackingSecurity ts = new TrackingSecurity(security, 0, 0, 1000);
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
			if (!isConnectWindowOpen)
			{
				cw.Dispatcher.Invoke(() =>
				{
					cw = new ConnectWindow();
					cw.Owner = mainWindow;
					cw.Show();
				});
				isConnectWindowOpen = true;
			}
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
				ShowConnectWindow();
			}
			else
				serverManager.Disconnect();
		}

		// Закрыть окно подключения к серверу
		public void CloseConnectWindow()
		{
			cw?.Close();
			isConnectWindowOpen = false;
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
			logManager.Log("Подключен к серверу " + parameters.ServerType.ToString());
			OnServerConnectEvent(true);
			foreach (var ts in trackingSecurities)
				serverManager.StartListenSecurity(ts.Key);
			autoReconnectTimer.Stop();
			// Начать передачу данных в БД
			dataManager.StartTransferingData();
		}

		void OnServerDisconnect(string reason)
		{
			if (reason != "Disconnected by user" && parameters.AutoReconnect)
			{
				parameters.ServerType = Enums.ToServerType(((int)parameters.ServerType + 1) % 4);
				autoReconnectTimer.Start();
			}
			logManager.Log("Отключен от сервера " + parameters.ServerType.ToString(), reason);
			OnServerConnectEvent(false);
			// Прекратить передачу данных в БД
			dataManager.StopTransferingData();
		}

		void OnTradeReceive(string security, DateTime time, double price, double volume, Enums.Direction direction)
		{
			TrackingSecurity ts = trackingSecurities[security];
            double vol = volume / ts.Security.LotSize;
			ts.VolumeReceived += vol;
			bool minVolume = vol >= ts.MinimumVolume ? true : false;
            dataManager.AddData(security, time, (decimal)price, vol, direction, minVolume);
		}

		void OnReconnectTimer(object sender, ElapsedEventArgs e)
		{
			logManager.Log("Автоматическое переподключение к серверу " + parameters.ServerType.ToString());
			autoReconnectTimer.Interval = parameters.AutoReconnectTime * 1000;
			if (parameters.AutoReconnect)
				ConnectToServer();
		}

		void OnVolumesUpdate(string security, double volumeSent)
		{
			if (trackingSecurities.ContainsKey(security))
				trackingSecurities[security].VolumeSent += volumeSent;
		}

		public event Action<bool> OnServerConnectEvent;

		public event Action<TrackingSecurity> OnTrackingSecurityAdd;
    }
}
