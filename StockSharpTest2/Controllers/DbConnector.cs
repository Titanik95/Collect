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

        // Создание дневной таблицы сделок
        public async Task<bool> CreateDayTradesTable(string tableName)
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
                command.CommandText = Properties.Resources.ProcName_CreateDayTradesTable;
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@Date", date);
                command.Parameters.AddWithValue("@TableName", tableName);
                try
                {
                    await command.ExecuteNonQueryAsync();
                }
                catch
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Создание дневной таблицы объемов
        /// </summary>
        /// <param name="tableName"> Код ценной бумаги</param>
        /// <returns></returns>
        public async Task<bool> CreateDayVolumesTable(string tableName)
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
                command.CommandText = Properties.Resources.ProcName_CreateDayVolumesTable;
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@Date", date);
                command.Parameters.AddWithValue("@TableName", tableName);
                try
                {
                    await command.ExecuteNonQueryAsync();
                }
                catch
                {
                    return false;
                }
            }
            return true;
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
                    await CreateDayTradesTable(tableName);
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

                await CreateDayVolumesTable(tableName);

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
        /// Обновление данных дневных объемов
        /// </summary>
        /// <param name="tableName"> Код ценной бумаги </param>
        /// <param name="volume"> Объект, описывающий объем за минуту </param>
        /// <returns></returns>
        public async Task<int> UpdateDayVolumesData(string tableName, DayVolume volume)
        {
            int rowsAffected = 0;
            CancellationTokenSource cts = new CancellationTokenSource(cancellationTokenTime);
            using (SqlConnection con = new SqlConnection(Common.GetConnectionString(dataStorage)))
            {
                try
                {
                    await con.OpenAsync(cts.Token);
                }
                catch
                {
                    return -1;
                }

                SqlCommand command = con.CreateCommand();
                command.CommandText = Properties.Resources.ProcName_UpdateDayVolume;
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

        public async Task<string> LastMinute(string tableName)
        {
            using (SqlConnection con = new SqlConnection(Common.GetConnectionString(dataStorage)))
            {
                try
                {
                    await con.OpenAsync();
                }
                catch
                {
                    return "";
                }

                SqlCommand command = con.CreateCommand();
                command.CommandText = Properties.Resources.ProcName_LastMinute;
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@Date", date);
                command.Parameters.AddWithValue("@TableName", tableName);
                try
                {
                    var r = command.ExecuteReaderAsync();
                    var result = r.Result;
                    if (!result.HasRows)
                    {
                        return "";
                    }
                    else
                    {
                        await result.ReadAsync();
                        return result[0] as string;
                    }
                }
                catch
                {
                    return "";
                }
            }
        }

        public async Task<bool> ContainsMinute(string tableName, string minute)
        {
            CancellationTokenSource cts = new CancellationTokenSource(cancellationTokenTime);
            using (SqlConnection con = new SqlConnection(Common.GetConnectionString(dataStorage)))
            {
                try
                {
                    con.Open();
                }
                catch
                {
                    return false;
                }

                SqlCommand command = con.CreateCommand();
                command.CommandText = Properties.Resources.ProcName_ContainsMinute;
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@Date", date);
                command.Parameters.AddWithValue("@TableName", tableName);
                command.Parameters.AddWithValue("@Minute", minute);
                try
                {
                    var r = command.ExecuteReaderAsync();
                    var result = r.Result;
                    if (!result.HasRows)
                    {
                        return false;
                    }
                    else
                    {
                        await result.ReadAsync();
                        if (result.GetInt32(0) == 1)
                            return true;
                        else
                            return false;
                    }
                }
                catch
                {
                    return false;
                }
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