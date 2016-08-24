using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

using Collect.Models;
using Collect.Controllers;
using Collect.Views;
using System.Windows.Controls;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Windows.Input;
using System.Timers;

namespace Collect
{
    public partial class MainWindow : Window
    {
        // Сервер-менеджер
        ServerManager serverManager;
        // Отслеживаемые инструменты
        ObservableCollection<TrackingSecurity> trackingSecurities;
        // Параметры приложения
        Parameters parameters;
        // Иконка в трее
        System.Windows.Forms.NotifyIcon ni;
        // Окно подключения
        ConnectWindow cw;
        // Таймер переподключения
        Timer autoReconnectTimer;
        // Лог-менеджер
        LogManager logManager;

        int controlToolBarOpenWidth = 160, controlToolBarCloseWidth = 48;

        public MainWindow()
        {
            InitializeComponent();

            addSecurityButton.IsEnabled = false;
            ChangeConnectionStatus(false);
            logManager = new LogManager();

            InitParameters();
            InitServerManager();
            InitTrackingSecurities();
            InitTrayIcon();
            InitTimers();
        }

        void InitTrayIcon()
        {
            ni = new System.Windows.Forms.NotifyIcon();
            ni.Icon = new System.Drawing.Icon("icon.ico");
            ni.Visible = true;
            ni.DoubleClick += (s, e) =>
                {
                    this.Show();
                    this.WindowState = WindowState.Normal;
                };
            ni.ContextMenu = new System.Windows.Forms.ContextMenu();
            var menuItem1 = new System.Windows.Forms.MenuItem("Выход");
            menuItem1.Click += (s, e) => Application.Current.Shutdown();
            ni.ContextMenu.MenuItems.Add(menuItem1);
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == System.Windows.WindowState.Minimized)
                this.Hide();

            base.OnStateChanged(e);
        }

        /// <summary>
        /// Инициализация отслеживаемых инструментов из файла
        /// </summary>
        async void InitTrackingSecurities()
        {
            trackingSecurities = new ObservableCollection<TrackingSecurity>();
            securitiesDataGrid.ItemsSource = trackingSecurities;

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

            addSecurityButton.IsEnabled = true;
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

        void OnReconnectTimer(object sender, ElapsedEventArgs e)
        {
            autoReconnectTimer.Interval = parameters.AutoReconnectTime * 1000;
            if (parameters.AutoReconnect)
                Dispatcher.Invoke(() => ConnectToServer());
            logManager.Log("Автоматическое переподключение к серверу");
            
            
        }

        /// <summary>
        /// Обработка события подключения к серверу
        /// </summary>
        void OnServerConnect()
        {
            Dispatcher.Invoke(() => ChangeConnectionStatus(true));
            Dispatcher.Invoke(() => connectButton.IsEnabled = true);
            logManager.Log("Подключен к серверу");
        }

        /// <summary>
        /// Обработка события отключения от сервера
        /// </summary>
        /// <param name="reason"></param>
        void OnServerDisconnect(string reason)
        {
            if (reason != "Disconnected by user" && parameters.AutoReconnect)
                autoReconnectTimer.Start();
            Dispatcher.Invoke(() => ChangeConnectionStatus(false));
            Dispatcher.Invoke(() => connectButton.IsEnabled = true);
            logManager.Log("Отключен от сервера", reason);
        }

        void ShowAndHideButtons(string showButton, object sender)
        {
            Button b = ((FrameworkElement)((Button)sender).Parent).FindName(showButton) as Button;
            if (b != null)
                b.Visibility = Visibility.Visible;
            (sender as Button).Visibility = Visibility.Collapsed;
        }

        void ConnectToServer()
        {
            autoReconnectTimer.Stop();
            connectButton.IsEnabled = false;
            serverManager?.Connect(parameters.Login, parameters.Password, parameters.ServerType);
            cw = new ConnectWindow();
            cw.Owner = this;
            cw.Show();
        }

        #region Public
        public List<Security> GetSecurities()
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

        public void ChangeConnectionStatus(bool connected)
        {
            if (connected)
            {
                connectButtonText.Text = "Отключиться";
                connectionStatusTextBlock.Text = "Подключен к серверу";
                connectionStatusTextBlock.Foreground = new SolidColorBrush(Colors.Green);
            }
            else
            {
                connectButtonText.Text = "Подключиться";
                connectionStatusTextBlock.Text = "Отключен от сервера";
                connectionStatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
            }
            if (cw != null)
                cw.Close();
        }

        public void UpdateParameters()
        {
            serverManager?.SetDataStorage(parameters.DataStorage);
        }

        #endregion

        #region UIHandlers

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            serverManager.CleanUp();
            ni.Visible = false;

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

        private void connectButton_Click(object sender, RoutedEventArgs e)
        {
            if (!serverManager.IsConnected)
            {
                ConnectToServer();
            }
            else
                serverManager.Disconnect();
        }

        private void controlToolBar_Loaded(object sender, RoutedEventArgs e)
        {
            ToolBar toolBar = sender as ToolBar;
            var overflowGrid = toolBar.Template.FindName("OverflowGrid", toolBar) as FrameworkElement;
            if (overflowGrid != null)
            {
                overflowGrid.Visibility = Visibility.Collapsed;
            }

            var mainPanelBorder = toolBar.Template.FindName("MainPanelBorder", toolBar) as FrameworkElement;
            if (mainPanelBorder != null)
            {
                mainPanelBorder.Margin = new Thickness(0);
            }
        }

        private void AddSecurityButton_Click(object sender, RoutedEventArgs e)
        {
            SecurityPicker sp = new SecurityPicker(this);
            sp.ShowDialog();
        }

        private void pauseButton_Click(object sender, RoutedEventArgs e)
        {
            ((TrackingSecurity)securitiesDataGrid.SelectedItem).Tracking = false;
        }

        private void playButton_Click(object sender, RoutedEventArgs e)
        {
            ((TrackingSecurity)securitiesDataGrid.SelectedItem).Tracking = true;
        }

        private void closeButton(object sender, RoutedEventArgs e)
        {
            TrackingSecurity ts = (TrackingSecurity)securitiesDataGrid.SelectedItem;
            trackingSecurities.Remove(ts);
            serverManager.RemoveTrackingSecurity(ts.Security.Code);
        }

        private void pauseAllTrackingsButton_Click(object sender, RoutedEventArgs e)
        {
            ShowAndHideButtons("startAllTrackingsButton", sender);
            foreach (TrackingSecurity ts in trackingSecurities)
                ts.Tracking = false;
        }

        private void startAllTrackingsButton_Click(object sender, RoutedEventArgs e)
        {
            ShowAndHideButtons("pauseAllTrackingsButton", sender);
            foreach (TrackingSecurity ts in trackingSecurities)
                ts.Tracking = true;
        }

        private void closeAllTrackingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (trackingSecurities.Count == 0)
                return;
            var result = MessageBox.Show(this, "Все отслеживания будут удалены", "Предупреждение", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                foreach (TrackingSecurity ts in trackingSecurities)
                    serverManager.RemoveTrackingSecurity(ts.Security.Code);
                trackingSecurities.Clear();
            }
        }

        private void controlToolBar_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            controlToolBar.Width = controlToolBarCloseWidth;
            controlToolBarTray.Width = controlToolBarCloseWidth;
        }

        private void parametersButton_Click(object sender, RoutedEventArgs e)
        {
            ParametersWindow pw = new ParametersWindow(parameters, this);
            pw.ShowDialog();

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (parameters.AutoConnect)
                ConnectToServer();
        }

        private void CloseCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void CommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void MenuItemExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void MenuItemOpen_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Normal;
        }

        private void controlToolBar_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            controlToolBar.Width = controlToolBarOpenWidth;
            controlToolBarTray.Width = controlToolBarOpenWidth;
        }
        #endregion
    }
}
