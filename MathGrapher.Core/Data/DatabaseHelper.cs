using Microsoft.Data.Sqlite;

namespace MathGrapher.Core.Data
{
    public static class DatabaseHelper
    {
        private static string? _connectionString;

        public static void Initialize(string databasePath)
        {
            _connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = databasePath
            }.ToString();

            using SqliteConnection connection = GetConnection();
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE IF NOT EXISTS GraphHistory (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Expression TEXT NOT NULL,
                    XMin REAL NOT NULL,
                    XMax REAL NOT NULL,
                    Step REAL NOT NULL,
                    Area REAL NULL,
                    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
                );
                """;
            command.ExecuteNonQuery();
        }

        public static SqliteConnection GetConnection()
        {
            if (string.IsNullOrEmpty(_connectionString))
            {
                throw new InvalidOperationException(
                    "Строка подключения не установлена. Вызовите Initialize.");
            }

            var connection = new SqliteConnection(_connectionString);
            connection.Open();
            return connection;
        }
    }
}
