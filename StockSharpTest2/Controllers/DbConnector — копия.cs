using StockSharpTest.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace StockSharpTest
{
    class DbConnector
    {
        string connectionString;
        string date;

        public DbConnector(string date, string connectionString)
        {
            this.date = date;
            this.connectionString = connectionString;
        }

        public DbConnector(string date)
        {
            this.date = date;
            connectionString = Common.GetConnectionString();
        }

        public void CreateDayTradesTable(string tableName)
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();

                SqlCommand command = con.CreateCommand();
                command.CommandText = Properties.Resources.ProcName_CreateDayTradesTable;
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@Date", date);
                command.Parameters.AddWithValue("@TableName", tableName);
                command.ExecuteNonQuery();
            }
        }

        public void CreateDayVolumesTable(string tableName)
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();

                SqlCommand command = con.CreateCommand();
                command.CommandText = Properties.Resources.ProcName_CreateDayVolumesTable;
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@Date", date);
                command.Parameters.AddWithValue("@TableName", tableName);
                command.ExecuteNonQuery();
            }
        }

        public void InsertDayTradesData(string tableName, List<DayTrade> trades)
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();

                CreateDayTradesTable(tableName);

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
                    command.ExecuteNonQuery();
                }
            }
        }

        public async void InsertDayVolumesData(string tableName, DayVolume volume)
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();

                CreateDayVolumesTable(tableName);

                SqlCommand command = con.CreateCommand();
                command.CommandText = Properties.Resources.ProcName_InsertDayVolumesData;
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@Date", date);
                command.Parameters.AddWithValue("@TableName", tableName);
                command.Parameters.AddWithValue("@Minute", volume.Minute);
                command.Parameters.AddWithValue("@VolumeBuy", volume.VolumeBuy);
                command.Parameters.AddWithValue("@VolumeSell", volume.VolumeSell);
                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task<int> UpdateDayVolumesData(string tableName, DayVolume volume)
        {
            int rowsAffected = 0;
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();

                SqlCommand command = con.CreateCommand();
                command.CommandText = Properties.Resources.ProcName_UpdateDayVolume;
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@Date", date);
                command.Parameters.AddWithValue("@TableName", tableName);
                command.Parameters.AddWithValue("@Minute", volume.Minute);
                command.Parameters.AddWithValue("@VolumeBuy", volume.VolumeBuy);
                command.Parameters.AddWithValue("@VolumeSell", volume.VolumeSell);
                rowsAffected = await command.ExecuteNonQueryAsync();
            }
            return rowsAffected;
        }

        public async void TransferDayData()
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();

                SqlCommand command = con.CreateCommand();
                command.CommandText = Properties.Resources.ProcName_TransferDayTables;
                command.CommandType = CommandType.StoredProcedure;
                await command.ExecuteNonQueryAsync();
            }
        }

        public string LastMinute(string tableName, out int volumeBuy, out int volumeSell)
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();

                SqlCommand command = con.CreateCommand();
                command.CommandText = Properties.Resources.ProcName_LastMinute;
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@Date", date);
                command.Parameters.AddWithValue("@TableName", tableName);
                var result = command.ExecuteReader();
                volumeBuy = 0; volumeSell = 0;
                if (!result.HasRows)
                {
                    return "";
                }
                else
                {
                    result.Read();
                    volumeBuy = (int)result[1];
                    volumeSell = (int)result[2];
                    return result[0] as string;
                }
            }
        }
    }
}
