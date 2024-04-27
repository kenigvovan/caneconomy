using Microsoft.Data.Sqlite;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Config;

namespace caneconomy.src.db
{
    public class SQLiteDatabaseHanlder: DatabaseHandler
    {
        ConcurrentQueue<QuerryInfo> queryQueue = new ConcurrentQueue<QuerryInfo>();
        private SqliteConnection SqliteConnection = null;
        string INSERT_QUERRY;
        string UPDATE_QUERRY;
        string DELETE_QUERRY;
        public delegate void OnReadAllItems(SqliteConnection sqliteConnection);
        public OnReadAllItems OnReadAction = null;
        public SQLiteDatabaseHanlder(string insert_q, string update_q, string delete_q, string create_table_q, OnReadAllItems OnReadAction) : base()
        {
            INSERT_QUERRY = insert_q;
            UPDATE_QUERRY = update_q;
            DELETE_QUERRY = delete_q;
            this.OnReadAction = OnReadAction;
            string folderPath;
            if (caneconomy.config.PATH_TO_DB_AND_JSON_FILES.Length == 0)
            {
                folderPath = @"" + Path.Combine(GamePaths.ModConfig, caneconomy.config.DB_NAME);
            }
            else
            {
                folderPath = Path.Combine(caneconomy.config.PATH_TO_DB_AND_JSON_FILES, caneconomy.config.DB_NAME);
            }
            folderPath.Replace(@"\\", @"\");

            SqliteConnection = new SqliteConnection(@"Data Source=" + folderPath);
            if (SqliteConnection.State != System.Data.ConnectionState.Open)
            {
                SqliteConnection.Open();
            }

            SqliteCommand com = SqliteConnection.CreateCommand();
            com.CommandText = create_table_q;
            com.ExecuteNonQuery();

            caneconomy.sapi.Event.Timer((() =>
            {
                while (!this.queryQueue.IsEmpty)
                {
                    QuerryInfo query;
                    this.queryQueue.TryDequeue(out query);

                    if (query.action == QuerryType.UPDATE)
                    {
                        updateDatabase(query);
                    }
                    else if (query.action == QuerryType.INSERT)
                    {
                        insertToDatabase(query);
                    }
                    else
                    {
                        deleteFromDatabase(query);
                    }
                }
            }
            ), 0.5);
        }
        public override void readALL()
        {
            if(OnReadAction != null)
            {
                OnReadAction(this.SqliteConnection);
            }
        }
        public override bool updateDatabase(QuerryInfo querry)
        {
            if (this.SqliteConnection.State != System.Data.ConnectionState.Open)
            {
                SqliteConnection.Open();
            }

            int rowsChanged;
            using (var cmd = new SqliteCommand(UPDATE_QUERRY, SqliteConnection))
            {
                foreach (var pair in querry.parameters)
                {
                    cmd.Parameters.AddWithValue(pair.Key, pair.Value);
                }
                rowsChanged = cmd.ExecuteNonQuery();
            }
            if (rowsChanged == 0)
            {
                querry.action = QuerryType.INSERT;
                queryQueue.Enqueue(querry);
            }
            return true;
        }
        public override bool deleteFromDatabase(QuerryInfo querry)
        {
            if (this.SqliteConnection.State != System.Data.ConnectionState.Open)
            {
                SqliteConnection.Open();
            }

            int rowsChanged;
            using (var cmd = new SqliteCommand(DELETE_QUERRY, SqliteConnection))
            {
                foreach (var pair in querry.parameters)
                {
                    cmd.Parameters.AddWithValue(pair.Key, pair.Value);
                }
                rowsChanged = cmd.ExecuteNonQuery();
            }

            return rowsChanged > 0;

        }
        public override bool insertToDatabase(QuerryInfo querry)
        {
            if (this.SqliteConnection.State != System.Data.ConnectionState.Open)
            {
                SqliteConnection.Open();
            }

            int rowsChanged;
            using (var cmd = new SqliteCommand(INSERT_QUERRY, SqliteConnection))
            {
                foreach (var pair in querry.parameters)
                {
                    cmd.Parameters.AddWithValue(pair.Key, pair.Value);
                }
                rowsChanged = cmd.ExecuteNonQuery();
            }

            return rowsChanged > 0;
        }
    }
}
