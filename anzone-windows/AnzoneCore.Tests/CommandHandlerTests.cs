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

    [Fact] public void SetupAdminFailsWhenAlreadyConfigured()
        => Assert.False(_h.Handle(Req("SetupAdmin", null, new(){{"password","x"}})).Success);

    [Fact] public void SetupAdminSucceedsOnFreshFirstRun()
    {
        var db2 = Path.Combine(Path.GetTempPath(), $"h2_{System.Guid.NewGuid():N}.db");
        var cfg2 = Path.Combine(Path.GetTempPath(), $"h2_{System.Guid.NewGuid():N}.json");
        bool success; string? payload;
        using (var conn2 = new AnzoneDb($"Data Source={db2}"))
        {
            conn2.EnsureCreated();
            var freshAuth = new AuthService(new ConfigStore(cfg2)); // no password set -> first run
            var h2 = new CommandHandler(new WhitelistRepository(conn2), new LogRepository(conn2),
                                        freshAuth, new ConfigStore(cfg2), new SessionTokens());
            var r = h2.Handle(Req("SetupAdmin", null, new(){{"password","newpw"}}));
            success = r.Success;
            payload = r.Payload;
        } // conn2 disposed here before file deletion
        Assert.True(success);
        Assert.NotNull(payload); // a session token was issued
        File.Delete(db2); if (File.Exists(cfg2)) File.Delete(cfg2);
    }
}
