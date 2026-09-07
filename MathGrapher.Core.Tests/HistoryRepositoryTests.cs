using MathGrapher.Core.Data;
using MathGrapher.Core.Models;
using Microsoft.Data.Sqlite;

namespace MathGrapher.Core.Tests;

public class HistoryRepositoryTests : IDisposable
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"MathGrapher.Tests-{Guid.NewGuid():N}.db");

    public HistoryRepositoryTests()
    {
        DatabaseHelper.Initialize(_databasePath);
    }

    [Fact]
    public void AddRecord_ThenGetHistory_ReturnsRecordsInNewestFirstOrder()
    {
        HistoryRepository.AddRecord("x * x", -2.0, 2.0, 0.1, 2.5);
        HistoryRepository.AddRecord("sqrt(x)", -10.0, 10.0, 0.1, null);

        List<GraphRecord> records = HistoryRepository.GetHistory();

        Assert.Equal(2, records.Count);

        GraphRecord newest = records[0];
        GraphRecord oldest = records[1];

        Assert.Equal("sqrt(x)", newest.Expression);
        Assert.Equal(-10.0, newest.XMin);
        Assert.Equal(10.0, newest.XMax);
        Assert.Equal(0.1, newest.Step);
        Assert.Null(newest.Area);

        Assert.Equal("x * x", oldest.Expression);
        Assert.Equal(2.5, oldest.Area);

        Assert.True(newest.Id > oldest.Id);
        Assert.NotEqual(default, newest.CreatedAt);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        File.Delete(_databasePath);
    }
}
