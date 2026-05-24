using Microsoft.Data.Sqlite;

namespace AnzoneCore.Data;

public class AnzoneDb : System.IDisposable
{
    private readonly SqliteConnection _conn;
    public AnzoneDb(string connectionString)
    {
        _conn = new SqliteConnection(connectionString);
        _conn.Open();
    }

    public SqliteConnection Connection => _conn;

    public void EnsureCreated()
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS whitelist (
                image_path TEXT PRIMARY KEY,
                display_name TEXT NOT NULL,
                added_at_utc INTEGER NOT NULL);
            CREATE TABLE IF NOT EXISTS logs (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                timestamp_utc INTEGER NOT NULL,
                type TEXT NOT NULL,
                image_path TEXT,
                detail TEXT NOT NULL);";
        cmd.ExecuteNonQuery();
    }

    public void Dispose()
    {
        _conn.Dispose();
        SqliteConnection.ClearAllPools();
    }
}
