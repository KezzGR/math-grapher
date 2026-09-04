using MathGrapher.Core.Models;

namespace MathGrapher.Core.Data
{
    public static class HistoryRepository
    {
        public static void AddRecord(
            string expression,
            double xMin,
            double xMax,
            double step,
            double? area)
        {
            using var connection = DatabaseHelper.GetConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO GraphHistory (Expression, XMin, XMax, Step, Area)
                VALUES ($expression, $xMin, $xMax, $step, $area);
                """;
            command.Parameters.AddWithValue("$expression", expression);
            command.Parameters.AddWithValue("$xMin", xMin);
            command.Parameters.AddWithValue("$xMax", xMax);
            command.Parameters.AddWithValue("$step", step);
            command.Parameters.AddWithValue("$area", (object?)area ?? DBNull.Value);
            command.ExecuteNonQuery();
        }

        public static List<GraphRecord> GetHistory()
        {
            using var connection = DatabaseHelper.GetConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT Id, Expression, XMin, XMax, Step, Area, CreatedAt
                FROM GraphHistory
                ORDER BY CreatedAt DESC, Id DESC;
                """;
            using var reader = command.ExecuteReader();

            var records = new List<GraphRecord>();
            while (reader.Read())
            {
                records.Add(new GraphRecord
                {
                    Id = reader.GetInt32(0),
                    Expression = reader.GetString(1),
                    XMin = reader.GetDouble(2),
                    XMax = reader.GetDouble(3),
                    Step = reader.GetDouble(4),
                    Area = reader.IsDBNull(5) ? null : reader.GetDouble(5),
                    CreatedAt = reader.GetDateTime(6)
                });
            }

            return records;
        }
    }
}
