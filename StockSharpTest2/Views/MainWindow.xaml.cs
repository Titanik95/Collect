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
		// Главный контроллер
		MainController main;
		
		bool isConnected;

        // Иконка в трее
        System.Windows.Forms.NotifyIcon ni;

        int controlToolBarOpenWidth = 160, controlToolBarCloseWidth = 48;

        public MainWindow()
        {
            InitializeComponent();

			main = new MainController(this);
            ChangeConnectionStatus(false);
			securitiesDataGrid.ItemsSource = main.GetTrackingSecurities();

			main.OnServerConnectEvent += ChangeConnectionStatus;

            InitTrayIcon();
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
                Hide();

            base.OnStateChanged(e);
        }

        void OnServerConnect()
        {
			connectButtonText.Text = "Отключиться";
			connectionStatusTextBlock.Text = "Подключен к серверу";
			connectionStatusTextBlock.Foreground = new SolidColorBrush(Colors.Green);
			connectButton.IsEnabled = true;
			addSecurityButton.IsEnabled = true;
			isConnected = true;
        }

        void OnServerDisconnect()
        {
			connectButtonText.Text = "Подключиться";
			connectionStatusTextBlock.Text = "Отключен от сервера";
			connectionStatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
			isConnected = false;
        }

        #region Public

        public void ChangeConnectionStatus(bool connected)
        {
			Dispatcher.Invoke(() =>
			{
				if (connected)
					OnServerConnect();
				else
					OnServerDisconnect();
				main.CloseConnectWindow();
			});
        }

        #endregion

        #region UIHandlers

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
			main.CleanUpServerManager();
            ni.Visible = false;
			main.SaveTrackingSecurities();
        }

        private void connectButton_Click(object sender, RoutedEventArgs e)
        {
			main.ConnectToServer();
            if (!main.IsConnected)
				connectButton.IsEnabled = false;
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
			main.OpenSecurityPickerWindow();
        }

        private void closeButton(object sender, RoutedEventArgs e)
        {
            TrackingSecurity ts = (TrackingSecurity)securitiesDataGrid.SelectedItem;
			main.RemoveTrackingSecurity(ts);
        }

        private void controlToolBar_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            controlToolBar.Width = controlToolBarCloseWidth;
            controlToolBarTray.Width = controlToolBarCloseWidth;
        }

        private void parametersButton_Click(object sender, RoutedEventArgs e)
        {
			main.OpenParametersWindow();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
			main.AutoConnect();
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
