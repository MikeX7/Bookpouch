using System;
using System.Diagnostics;
using System.IO;
using System.Data.SQLite;

namespace Bookpouch
{
    static class Db
    {
        private const string DbFile = "Library.db";
        private static SQLiteConnection Connection
        {
            get
            {
                CheckFile();

                var parameters = new SQLiteConnectionStringBuilder
                {
                    DataSource = DbFile,
                    Version = 3,
                    JournalMode = SQLiteJournalModeEnum.Wal
                };

                var connection = new SQLiteConnection(parameters.ToString());
                connection.Open();
                
                return connection;
            }
        }

        /// <summary>
        /// Execute supplied query and return the result
        /// </summary>        
        /// <param name="sql">The query as a string</param>
        /// <param name="parameters">Parameters to be inserted into the query</param>
        /// <returns>Result of the query</returns>
        public static SQLiteDataReader Query(string sql, SQLiteParameter[] parameters = null)
        {

            using (var command = new SQLiteCommand(sql, Connection))
            {
                if(parameters != null)
                    command.Parameters.AddRange(parameters);
                
                //Debug.WriteLine(command.CommandText + ": " + parameters[0].Value);    
                try
                {
                    var reader = command.ExecuteReader();
                    Connection.Close();
                    Connection.Dispose();                    
                    return reader;
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Db " + e);
                    DebugConsole.WriteLine("Db: " + e);
                    throw;
                }                
            }            
        }

        /// <summary>
        /// Execute supplied non query
        /// </summary>        
        /// <param name="sql">The query as a string</param>
        /// <param name="parameters">Parameters to be inserted into the query</param>
        public static void NonQuery(string sql, SQLiteParameter[] parameters = null)
        {

            using (var command = new SQLiteCommand(sql, Connection))
            {
                if (parameters != null)
                    command.Parameters.AddRange(parameters);

                //Debug.WriteLine(command.CommandText + ": " + parameters[0].Value);                

                try
                {
                    command.ExecuteNonQuery();
                    Connection.Close();
                    Connection.Dispose();
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Db " + e);
                    DebugConsole.WriteLine("Db: " + e);
                }

                
            }
        }

        public static bool QueryExists(string sql, SQLiteParameter[] parameters = null)
        {
            bool exists;

            using (var command = new SQLiteCommand(sql, Connection))
            {
                if (parameters != null)
                    command.Parameters.AddRange(parameters);

                exists = SQLiteConvert.ToBoolean(command.ExecuteScalar());

                Connection.Close();
                Connection.Dispose();
            }

            return exists;
        }

        private static void CheckFile()
        {
            if (File.Exists(DbFile)) 
                return;            

            SQLiteConnection.CreateFile(DbFile);
            GenerateDbStructure();
        }

        /// <summary>
        /// If the db file is empty, generate table structure 
        /// </summary>
        private static void GenerateDbStructure()
        {
            const string sqlBooks = 
                "CREATE TABLE books (" +
                "Path VARCHAR(255) NOT NULL PRIMARY KEY," +
                "Title VARCHAR(255) NOT NULL," +
                "Author VARCHAR(255)," +
                "Contributor VARCHAR(255)," +
                "Publisher VARCHAR(255)," +
                "Language VARCHAR(10)," +
                "Published DATE," +
                "Description TEXT," +
                "Series VARCHAR(255)," +
                "Coverage VARCHAR(255)," +                
                "MobiType VARCHAR(100)," +
                "Identifier VARCHAR(255)," +
                "Relation VARCHAR(255)," +
                "Size INT NOT NULL," +                
                "Favorite BOOLEAN NOT NULL," +
                "Sync BOOLEAN NOT NULL," +
                "Created DATE NOT NULL," +
                "Cover BLOB" +                      
                ")";

            const string sqlCategories =
                "CREATE TABLE categories (" +
                "Path VARCHAR(255) NOT NULL," +
                "Name VARCHAR(255) NOT NULL," +
                "FromFile BOOLEAN NOT NULL," +                
                "PRIMARY KEY(Path, Name)" +                
                ")";

            const string sqlFilters =
                "CREATE TABLE filters (" +
                "Name VARCHAR(255) NOT NULL PRIMARY KEY," +
                "Parameters BLOB NOT NULL" +                
                ")";

            using (var command = new SQLiteCommand(sqlBooks, Connection))
            {                
                command.ExecuteNonQuery();

                command.CommandText = sqlCategories;
                command.ExecuteNonQuery();

                command.CommandText = sqlFilters;
                command.ExecuteNonQuery();
            }            
        }


    }
}
