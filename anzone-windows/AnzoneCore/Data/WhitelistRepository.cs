using System.Collections.Generic;
using AnzoneCore.Model;
using AnzoneCore.Util;

namespace AnzoneCore.Data;

public class WhitelistRepository
{
    private readonly AnzoneDb _db;
    public WhitelistRepository(AnzoneDb db) => _db = db;

    public void Add(string imagePath, string displayName)
    {
        var norm = PathNormalizer.Normalize(imagePath);
        using var cmd = _db.Connection.CreateCommand();
        cmd.CommandText = @"INSERT OR REPLACE INTO whitelist(image_path, display_name, added_at_utc)
                            VALUES ($p, $n, $t)";
        cmd.Parameters.AddWithValue("$p", norm);
        cmd.Parameters.AddWithValue("$n", displayName);
        cmd.Parameters.AddWithValue("$t", System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        cmd.ExecuteNonQuery();
    }

    public void Remove(string imagePath)
    {
        using var cmd = _db.Connection.CreateCommand();
        cmd.CommandText = "DELETE FROM whitelist WHERE image_path = $p";
        cmd.Parameters.AddWithValue("$p", PathNormalizer.Normalize(imagePath));
        cmd.ExecuteNonQuery();
    }

    public bool Contains(string normalizedPath)
    {
        using var cmd = _db.Connection.CreateCommand();
        cmd.CommandText = "SELECT EXISTS(SELECT 1 FROM whitelist WHERE image_path = $p)";
        cmd.Parameters.AddWithValue("$p", PathNormalizer.Normalize(normalizedPath));
        return System.Convert.ToInt64(cmd.ExecuteScalar()) == 1;
    }

    public List<WhitelistEntry> GetAll()
    {
        var list = new List<WhitelistEntry>();
        using var cmd = _db.Connection.CreateCommand();
        cmd.CommandText = "SELECT image_path, display_name, added_at_utc FROM whitelist ORDER BY display_name";
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new WhitelistEntry(r.GetString(0), r.GetString(1), r.GetInt64(2)));
        return list;
    }

    public HashSet<string> GetNormalizedSet()
    {
        var set = new HashSet<string>();
        foreach (var e in GetAll()) set.Add(e.ImagePath);
        return set;
    }
}
