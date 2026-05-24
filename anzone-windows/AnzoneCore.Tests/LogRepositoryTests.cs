using System.IO;
using AnzoneCore.Data;
using AnzoneCore.Model;
using Xunit;

public class LogRepositoryTests : System.IDisposable
{
    private readonly string _file = Path.Combine(Path.GetTempPath(), $"anzlog_{System.Guid.NewGuid():N}.db");
    private readonly AnzoneDb _db;
    private readonly LogRepository _repo;

    public LogRepositoryTests()
    {
        _db = new AnzoneDb($"Data Source={_file}");
        _db.EnsureCreated();
        _repo = new LogRepository(_db);
    }

    public void Dispose() { _db.Dispose(); if (File.Exists(_file)) File.Delete(_file); }

    [Fact] public void RecordPersists()
    {
        _repo.Record(LogType.ProcessBlocked, @"C:\x\x.exe", "blocked x");
        var all = _repo.GetRecent(10);
        Assert.Single(all);
        Assert.Equal(LogType.ProcessBlocked, all[0].Type);
    }

    [Fact] public void RecentOrderedDesc()
    {
        _repo.Record(LogType.ServiceStart, null, "start");
        _repo.Record(LogType.AdminLogin, null, "login");
        var all = _repo.GetRecent(10);
        Assert.Equal(LogType.AdminLogin, all[0].Type);
    }

    [Fact] public void ClearEmpties()
    {
        _repo.Record(LogType.ServiceStart, null, "s");
        _repo.Clear();
        Assert.Empty(_repo.GetRecent(10));
    }
}
