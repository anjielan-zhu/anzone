using System.Collections.Generic;
using System.IO;
using AnzoneCore.Auth;
using AnzoneCore.Data;
using AnzoneCore.Ipc;
using Xunit;

public class CommandHandlerTests : System.IDisposable
{
    private readonly string _db = Path.Combine(Path.GetTempPath(), $"h_{System.Guid.NewGuid():N}.db");
    private readonly string _cfg = Path.Combine(Path.GetTempPath(), $"h_{System.Guid.NewGuid():N}.json");
    private readonly CommandHandler _h;
    private readonly AnzoneDb _conn;

    public CommandHandlerTests()
    {
        _conn = new AnzoneDb($"Data Source={_db}");
        _conn.EnsureCreated();
        var auth = new AuthService(new ConfigStore(_cfg));
        auth.SetAdminPassword("pw");
        _h = new CommandHandler(new WhitelistRepository(_conn), new LogRepository(_conn),
                                auth, new ConfigStore(_cfg), new SessionTokens());
    }
    public void Dispose() { _conn.Dispose(); if (File.Exists(_db)) File.Delete(_db); if (File.Exists(_cfg)) File.Delete(_cfg); }

    private static IpcRequest Req(string op, string? tok = null, Dictionary<string,string>? args = null)
        => new(op, tok, args ?? new());

    [Fact] public void LoginWrongPasswordFails()
        => Assert.False(_h.Handle(Req("Login", null, new(){{"password","bad"}})).Success);

    [Fact] public void LoginThenAddWhitelistWithToken()
    {
        var login = _h.Handle(Req("Login", null, new(){{"password","pw"}}));
        Assert.True(login.Success);
        var token = login.Payload!;
        Assert.True(_h.Handle(Req("AddWhitelist", token, new(){{"path",@"C:\a\a.exe"},{"name","A"}})).Success);
    }

    [Fact] public void MutatingWithoutTokenRejected()
        => Assert.False(_h.Handle(Req("AddWhitelist", "badtoken", new(){{"path",@"C:\a\a.exe"},{"name","A"}})).Success);

    [Fact] public void StatusNeedsNoToken()
        => Assert.True(_h.Handle(Req("GetStatus")).Success);
}
