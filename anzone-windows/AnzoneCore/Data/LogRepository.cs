using System.Collections.Generic;
using AnzoneCore.Model;

namespace AnzoneCore.Data;

public class LogRepository
{
    private readonly AnzoneDb _db;
    public LogRepository(AnzoneDb db) => _db = db;

    public void Record(LogType type, string? imagePath, string detail)
    {
        using var cmd = _db.Connection.CreateCommand();
        cmd.CommandText = @"INSERT INTO logs(timestamp_utc, type, image_path, detail)
                            VALUES ($t, $ty, $p, $d)";
        cmd.Parameters.AddWithValue("$t", System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        cmd.Parameters.AddWithValue("$ty", type.ToString());
        cmd.Parameters.AddWithValue("$p", (object?)imagePath ?? System.DBNull.Value);
        cmd.Parameters.AddWithValue("$d", detail);
        cmd.ExecuteNonQuery();
    }

    public List<LogEntry> GetRecent(int limit)
    {
        var list = new List<LogEntry>();
        using var cmd = _db.Connection.CreateCommand();
        cmd.CommandText = @"SELECT id, timestamp_utc, type, image_path, detail
                            FROM logs ORDER BY id DESC LIMIT $lim";
        cmd.Parameters.AddWithValue("$lim", limit);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new LogEntry(
                r.GetInt64(0), r.GetInt64(1),
                System.Enum.Parse<LogType>(r.GetString(2)),
                r.IsDBNull(3) ? null : r.GetString(3), r.GetString(4)));
        return list;
    }

    public void Clear()
    {
        using var cmd = _db.Connection.CreateCommand();
        cmd.CommandText = "DELETE FROM logs";
        cmd.ExecuteNonQuery();
    }
}
