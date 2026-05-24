using System.IO;
using AnzoneCore.Auth;
using Xunit;

public class AuthServiceTests : System.IDisposable
{
    private readonly string _file = Path.Combine(Path.GetTempPath(), $"anzcfg_{System.Guid.NewGuid():N}.json");
    private readonly AuthService _auth;

    public AuthServiceTests() => _auth = new AuthService(new ConfigStore(_file));
    public void Dispose() { if (File.Exists(_file)) File.Delete(_file); }

    [Fact] public void FirstRunTrueInitially() => Assert.True(_auth.IsFirstRun());

    [Fact] public void SetPasswordThenVerify()
    {
        _auth.SetAdminPassword("pw1");
        Assert.False(_auth.IsFirstRun());
        Assert.True(_auth.Verify("pw1"));
        Assert.False(_auth.Verify("nope"));
    }

    [Fact] public void ChangePassword()
    {
        _auth.SetAdminPassword("old");
        _auth.SetAdminPassword("new");
        Assert.False(_auth.Verify("old"));
        Assert.True(_auth.Verify("new"));
    }

    [Fact] public void PausedFlagPersists()
    {
        var store = new ConfigStore(_file);
        store.Paused = true;
        Assert.True(new ConfigStore(_file).Paused);
    }
}
