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
    public class SQLiteDatabaseHanlder
    {
        ConcurrentQueue<QuerryInfo> queryQueue = new ConcurrentQueue<QuerryInfo>();
        private SqliteConnection SqliteConnection = null;

        public SQLiteDatabaseHanlder() : base()
        {
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
        public bool updateDatabase(QuerryInfo querry)
        {
            if (this.SqliteConnection.State != System.Data.ConnectionState.Open)
            {
                SqliteConnection.Open();
            }
            string querryString = "";
            switch (querry.targetTable)
            {
                case "BANKS":
                    querryString = QuerryTemplates.UPDATE_BANK;
                    break;
            }


            int rowsChanged;
            using (var cmd = new SqliteCommand(querryString, SqliteConnection))
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
        public bool deleteFromDatabase(QuerryInfo querry)
        {
            if (this.SqliteConnection.State != System.Data.ConnectionState.Open)
            {
                SqliteConnection.Open();
            }
            string querryString = "";
            switch (querry.targetTable)
            {
                case "BANKS":
                    querryString = QuerryTemplates.DELETE_BANK;
                    break;           
            }


            int rowsChanged;
            using (var cmd = new SqliteCommand(querryString, SqliteConnection))
            {
                foreach (var pair in querry.parameters)
                {
                    cmd.Parameters.AddWithValue(pair.Key, pair.Value);
                }
                rowsChanged = cmd.ExecuteNonQuery();
            }

            return rowsChanged > 0;

        }

        public bool insertToDatabase(QuerryInfo querry)
        {
            if (this.SqliteConnection.State != System.Data.ConnectionState.Open)
            {
                SqliteConnection.Open();
            }
            string querryString = "";
            switch (querry.targetTable)
            {
                case "BANKS":
                    querryString = QuerryTemplates.INSERT_BANK;
                    break;
            }


            int rowsChanged;
            using (var cmd = new SqliteCommand(querryString, SqliteConnection))
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
