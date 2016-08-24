using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Collect.Models;
using System.Windows.Media;
using System.Timers;
using System.Threading;

namespace Collect.Views
{
    public partial class SecurityPicker : Window
    {
        Dictionary<string, string> exchangeList;
        Dictionary<string, string> typeList;
        string[] columnHeaders = { "Тикер", "Биржа", "Тип" };
        List<Security> securities;
        bool comboBoxesInitialized;
        MainWindow mainWindow;

        System.Timers.Timer popupTimer;
        double popupTimerOpenTime = 1500;
        CancellationTokenSource cts;

        public SecurityPicker(MainWindow mw)
        {
            Owner = mw;
            mainWindow = mw;

            InitializeComponent();
            InitializeFields();
            InitializeSecuritiesGrid();
            InitializePopupTimer();
            cts = new CancellationTokenSource();
        }

        void InitializePopupTimer()
        {
            popupTimer = new System.Timers.Timer();
            popupTimer.Elapsed += PopupClose;
            popupTimer.Interval = popupTimerOpenTime;
            popupTimer.AutoReset = false;
        }

        void InitializeFields()
        {
            exchangeList = new Dictionary<string, string>();
            exchangeList.Add("", "Все");
            exchangeList.Add("EQ", "Фондовый рынок MOEX");
            exchangeList.Add("RTS_FUT", "Срочный рынок MOEX");
            exchangeList.Add("CETS", "Валютный рынок MOEX");
            exchangeList.Add("SPBEX", "Фондовый рынок СПБ");
            exchangeList.Add("CME", "International futures market");
            exchangeList.Add("LSE", "Фондовый рынок LSE");
            exchangeList.Add("ABN", "ABN");
            exchangeList.Add("RUSIDX", "Индексы России");

            exchangeComboBox.ItemsSource = exchangeList;
            exchangeComboBox.DisplayMemberPath = "Value";
            exchangeComboBox.SelectedValuePath = "Key";
            exchangeComboBox.SelectedIndex = 0;

            typeList = new Dictionary<string, string>();
            typeList.Add("", "Все"); typeList.Add("FUT", "Фьючерс"); typeList.Add("SPBEX", "SPBEX");
            typeList.Add("TQBR", "Акции MOEX"); typeList.Add("ST", "ST"); typeList.Add("EQNE", "EQNE");
            typeList.Add("TQDE", "TQDE"); typeList.Add("CETS", "CETS"); typeList.Add("ADR", "ADR");
            typeList.Add("OPTM", "OPTM"); typeList.Add("CUR", "CUR"); typeList.Add("IDX", "IDX");
            typeList.Add("TQNE", "TQNE"); typeList.Add("OPT", "OPT"); typeList.Add("TQTF", "TQTF");
            typeList.Add("EQNL", "EQNL"); typeList.Add("TQIF", "TQIF"); typeList.Add("EQOB", "EQOB");
            typeList.Add("EQDB", "EQDB"); typeList.Add("EQNO", "EQNO"); typeList.Add("EQNB", "EQNB");
            typeList.Add("EQOS", "EQOS"); typeList.Add("EQEU", "EQEU"); typeList.Add("TQNL", "TQNL");
            typeList.Add("TQLV", "TQLV");

            typeComboBox.ItemsSource = typeList;
            typeComboBox.DisplayMemberPath = "Value";
            typeComboBox.SelectedValuePath = "Key";
            typeComboBox.SelectedIndex = 0;

            comboBoxesInitialized = true;
        }

        void InitializeSecuritiesGrid()
        {
            securities = mainWindow.GetSecurities();
            securitiesDataGrid.ItemsSource = securities.ToArray();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            string searchString = searchTextBox.Text;
            securitiesDataGrid.ItemsSource = securities.FindAll((s) => s.Code.Contains(searchString));
        }

        private void searchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            searchTextBox.SelectAll();
        }

        void PrepareSearch()
        {
            cts.Cancel();
            cts = new CancellationTokenSource();
            securitiesDataGrid.ItemsSource = null;
            securitiesDataGrid.Items.Clear();
            securitiesDataGrid.Items.Refresh();
            string searchString = searchTextBox.Text,
                exch = exchangeComboBox.SelectedValue.ToString(),
                type = typeComboBox.SelectedValue.ToString();
            ThreadPool.QueueUserWorkItem((w) => Search(searchString, exch, type, cts.Token));
        }

        void Search(string searchString, string exchange, string type, CancellationToken ct)
        {
            foreach (Security s in securities)
            {
                if (ct.IsCancellationRequested)
                    break;
                if (searchString == "" && exchange != "" && type != "")
                    Dispatcher.Invoke(() => securitiesDataGrid.Items.Add(s));
                else if ((s.Code.Contains(searchString) || s.ShortName.Contains(searchString)) &&
                            s.SecExchName.Contains(exchange) && s.Type.Contains(type))
                    Dispatcher.Invoke(() => securitiesDataGrid.Items.Add(s));
            }
        }

        private void searchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            PrepareSearch();
        }

        private void exchangeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBoxesInitialized)
                PrepareSearch();
        }

        private void typeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBoxesInitialized)
                PrepareSearch();
        }

        private async void addSecurityButton_Click(object sender, RoutedEventArgs e)
        {
            if (securitiesDataGrid.SelectedItems.Count == 0)
                return;
            int securitiesAdded = 0;
            foreach (Security s in securitiesDataGrid.SelectedItems)
            {
                if (await mainWindow.AddSecurity(s))
                    securitiesAdded++;
            }
            addSecurityStatusPopup.IsOpen = false;
            if (securitiesAdded == 0)
            {
                string multiAddition = securitiesDataGrid.SelectedItems.Count == 1 ? "" : "ы";
                popupBackground.Background = new SolidColorBrush(Color.FromRgb(0xFF, 0xA4, 0xA4));
                addSecurityResultText.Text = string.Format("Тикер{0} с таким кодом уже добавлен{0}", multiAddition);
            }
            else
            {
                string multiAddition = securitiesAdded == 1 ? "" : securitiesAdded < 5 ? "а" : "ов";
                popupBackground.Background = new SolidColorBrush(Color.FromRgb(0x88, 0xFF, 0x12));
                addSecurityResultText.Text = string.Format("Добавлено: {0} тикер{1}", securitiesAdded, multiAddition);
            }
            PopupOpen();
        }

        void PopupOpen()
        {
            addSecurityStatusPopup.IsOpen = true;
            popupTimer.Stop();
            popupTimer.Start();
        }

        void PopupClose(object sender, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(() => addSecurityStatusPopup.IsOpen = false);
        }
    }
}
