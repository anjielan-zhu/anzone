using System.IO;
using AnzoneCore.Data;
using Xunit;

public class WhitelistRepositoryTests : System.IDisposable
{
    private readonly string _file = Path.Combine(Path.GetTempPath(), $"anzwl_{System.Guid.NewGuid():N}.db");
    private readonly AnzoneDb _db;
    private readonly WhitelistRepository _repo;

    public WhitelistRepositoryTests()
    {
        _db = new AnzoneDb($"Data Source={_file}");
        _db.EnsureCreated();
        _repo = new WhitelistRepository(_db);
    }

    public void Dispose() { _db.Dispose(); if (File.Exists(_file)) File.Delete(_file); }

    [Fact] public void AddThenContainsNormalized()
    {
        _repo.Add(@"C:\Program Files\Foo\Foo.EXE", "Foo");
        Assert.True(_repo.Contains(@"c:\program files\foo\foo.exe"));
        Assert.False(_repo.Contains(@"c:\program files\bar\bar.exe"));
    }

    [Fact] public void RemoveRevokes()
    {
        _repo.Add(@"C:\a\a.exe", "A");
        _repo.Remove(@"C:\A\A.EXE");
        Assert.False(_repo.Contains(@"c:\a\a.exe"));
    }

    [Fact] public void GetAllAndNormalizedSet()
    {
        _repo.Add(@"C:\a\a.exe", "A");
        _repo.Add(@"C:\b\b.exe", "B");
        Assert.Equal(2, _repo.GetAll().Count);
        Assert.Contains(@"c:\a\a.exe", _repo.GetNormalizedSet());
    }
}
