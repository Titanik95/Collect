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
		// Сервер менеджер
		ServerManager serverManager;
		// Окно подключения
		ConnectWindow cw;
		// Отслеживаемые инструменты
		ObservableCollection<TrackingSecurity> trackingSecurities;
		// Таймер переподключения
		Timer autoReconnectTimer;
		// Лог-менеджер
		LogManager logManager;
		// Параметры
		Parameters parameters;

		// Подключен ли к серверу
		public bool IsConnected { get; set; }

		public MainController(MainWindow m)
		{
			mainWindow = m;
			serverManager = new ServerManager();
			trackingSecurities = new ObservableCollection<TrackingSecurity>();
			logManager = new LogManager();

			InitParameters();
			InitServerManager();
			InitTrackingSecurities();
			InitTimers();
		}

		/// <summary>
		/// Инициализация отслеживаемых инструментов из файла
		/// </summary>
		async void InitTrackingSecurities()
		{
			trackingSecurities = new ObservableCollection<TrackingSecurity>();

			using (FileStream fs = new FileStream(Properties.Resources.SecuritiesFileName, FileMode.OpenOrCreate))
			{
				BinaryFormatter bf = new BinaryFormatter();

				try
				{
					var ts = ((ObservableCollection<TrackingSecurity>)bf.Deserialize(fs));
					foreach (var s in ts)
					{
						trackingSecurities.Add(s);
						await serverManager.AddTrackingSecurity(s);
					}
				}
				catch (Exception ex)
				{
					logManager.Log("Ошибка десериализации инструментов", ex.Message);
				}
			}
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
			serverManager.SetDataStorage(parameters.DataStorage);
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


		public ObservableCollection<TrackingSecurity> GetTrackingSecurities()
		{
			return trackingSecurities;
		}

		public List<Security> GetSecuritiesList()
		{
			return serverManager.GetSecuritiesList();
		}

		public async Task<bool> AddSecurity(Security security)
		{
			bool result = false;
			TrackingSecurity ts = new TrackingSecurity(security, 0, 0, true, 2000);
			if (await serverManager.AddTrackingSecurity(ts))
			{
				trackingSecurities.Add(ts);
				result = true;
			}
			else
				result = false;

			return result;
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

		public void RemoveTrackingSecurity(TrackingSecurity ts)
		{
			trackingSecurities.Remove(ts);
			serverManager.RemoveTrackingSecurity(ts.Security.Code);
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
			serverManager?.SetDataStorage(parameters.DataStorage);
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
			IsConnected = true;
		}

		void OnServerDisconnect(string reason)
		{
			if (reason != "Disconnected by user" && parameters.AutoReconnect)
				autoReconnectTimer.Start();
			logManager.Log("Отключен от сервера", reason);
			OnServerConnectEvent(false);
			IsConnected = false;
		}

		void OnReconnectTimer(object sender, ElapsedEventArgs e)
		{
			autoReconnectTimer.Interval = parameters.AutoReconnectTime * 1000;
			if (parameters.AutoReconnect)
				ConnectToServer();
			logManager.Log("Автоматическое переподключение к серверу");
		}

		public event Action<bool> OnServerConnectEvent; 
	}
}
