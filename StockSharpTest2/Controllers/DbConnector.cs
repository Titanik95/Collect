using Collect.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Collect
{
    class DbConnector
    {
        DataStorage dataStorage;
        string date;
        int cancellationTokenTime = 10000;

        public DbConnector(string date)
        {
            this.date = date;
        }

        /// <summary>
        /// Вставка данных в дневную таблицу сделок
        /// </summary>
        /// <param name="tableName"> Код ценной бумаги </param>
        /// <param name="trades"> Список сделок </param>
        /// <returns></returns>
        public async Task<bool> InsertDayTradesData(string tableName, List<DayTrade> trades)
        {
            CancellationTokenSource cts = new CancellationTokenSource(cancellationTokenTime);
            using (SqlConnection con = new SqlConnection(Common.GetConnectionString(dataStorage)))
            {
                try
                {
                    await con.OpenAsync(cts.Token);
                }
                catch
                {
                    return false;
                }

                SqlCommand command = con.CreateCommand();
                command.CommandText = Properties.Resources.ProcName_InsertDayTradesData;
                command.CommandType = CommandType.StoredProcedure;
                for (int i = 0; i < trades.Count; i++)
                {
                    DayTrade trade = trades[i];
                    command.Parameters.Clear();
                    command.Parameters.AddWithValue("@Date", date);
                    command.Parameters.AddWithValue("@TableName", tableName);
                    command.Parameters.AddWithValue("@Time", trade.Time);
                    command.Parameters.AddWithValue("@Price", trade.Price);
                    command.Parameters.AddWithValue("@Volume", trade.Volume);
                    command.Parameters.AddWithValue("@Direction", trade.Direction);
                    try
                    {
                        await command.ExecuteNonQueryAsync();
                    }
                    catch
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Вставка данных в дневную таблицу объемов
        /// </summary>
        /// <param name="tableName"> Код ценной бумаги </param>
        /// <param name="volume"> Объект, описывающий объем за минуту </param>
        /// <returns></returns>
        public async Task<int> InsertDayVolumesData(string tableName, DayVolume volume)
        {
            CancellationTokenSource cts = new CancellationTokenSource(cancellationTokenTime);
            int rowsAffected;
            using (SqlConnection con = new SqlConnection(Common.GetConnectionString(dataStorage)))
            {
                try
                {
                    con.Open();
                }
                catch
                {
                    return -1;
                }

                SqlCommand command = con.CreateCommand();
                command.CommandText = Properties.Resources.ProcName_InsertDayVolumesData;
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@Date", date);
                command.Parameters.AddWithValue("@TableName", tableName);
                command.Parameters.AddWithValue("@Minute", volume.Minute);
                command.Parameters.AddWithValue("@VolumeBuy", volume.VolumeBuy);
                command.Parameters.AddWithValue("@VolumeSell", volume.VolumeSell);
                try
                {
                    rowsAffected = await command.ExecuteNonQueryAsync();
                }
                catch
                {
                    return -1;
                }
            }
            return rowsAffected;
        }

        /// <summary>
        /// Вызов встроенной процедуры для передачи данных из днеевной таблицы в годовые
        /// </summary>
        public async void TransferDayData()
        {
            using (SqlConnection con = new SqlConnection(Common.GetConnectionString(dataStorage)))
            {
                await con.OpenAsync();

                SqlCommand command = con.CreateCommand();
                command.CommandText = Properties.Resources.ProcName_TransferDayTables;
                command.CommandType = CommandType.StoredProcedure;
                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task<bool> TryConnectToDatabase(int delay)
        {
            CancellationTokenSource cts = new CancellationTokenSource(delay);
            using (SqlConnection con = new SqlConnection(Common.GetConnectionString(dataStorage)))
            {
                try
                {
                    await con.OpenAsync(cts.Token);
                }
                catch
                {
                    return false;
                }
                return true;
            }
        }

        public void SetDataStorage(DataStorage ds)
        {
            dataStorage = ds;
        }
    }
}