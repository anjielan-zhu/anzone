using System.Text.Json;
using AnzoneCore.Auth;
using AnzoneCore.Data;
using AnzoneCore.Model;

namespace AnzoneCore.Ipc;

public class CommandHandler
{
    private readonly WhitelistRepository _whitelist;
    private readonly LogRepository _logs;
    private readonly AuthService _auth;
    private readonly ConfigStore _config;
    private readonly SessionTokens _tokens;

    public CommandHandler(WhitelistRepository whitelist, LogRepository logs,
        AuthService auth, ConfigStore config, SessionTokens tokens)
    {
        _whitelist = whitelist; _logs = logs; _auth = auth; _config = config; _tokens = tokens;
    }

    public IpcResponse Handle(IpcRequest req)
    {
        switch (req.Op)
        {
            case "Login":
                if (_auth.Verify(req.Args.GetValueOrDefault("password", "")))
                { _logs.Record(LogType.AdminLogin, null, "admin login"); return IpcResponse.Ok(_tokens.Issue()); }
                _logs.Record(LogType.LoginFailed, null, "login failed");
                return IpcResponse.Fail("invalid credentials");

            case "SetupAdmin":
                if (!_auth.IsFirstRun()) return IpcResponse.Fail("already configured");
                _auth.SetAdminPassword(req.Args.GetValueOrDefault("password", ""));
                _logs.Record(LogType.AdminLogin, null, "initial admin setup");
                return IpcResponse.Ok(_tokens.Issue());

            case "GetStatus":
                return IpcResponse.Ok(JsonSerializer.Serialize(new {
                    paused = _config.Paused, whitelistCount = _whitelist.GetAll().Count,
                    firstRun = _auth.IsFirstRun() }));

            case "ListWhitelist":
                return IpcResponse.Ok(JsonSerializer.Serialize(_whitelist.GetAll()));

            case "GetLogs":
                if (!Auth(req)) return IpcResponse.Fail("unauthorized");
                return IpcResponse.Ok(JsonSerializer.Serialize(_logs.GetRecent(200)));
        }

        // mutating ops below require a valid token
        if (!Auth(req)) return IpcResponse.Fail("unauthorized");
        switch (req.Op)
        {
            case "AddWhitelist":
                _whitelist.Add(req.Args["path"], req.Args.GetValueOrDefault("name", ""));
                _logs.Record(LogType.WhitelistChange, req.Args["path"], "added");
                return IpcResponse.Ok();
            case "RemoveWhitelist":
                _whitelist.Remove(req.Args["path"]);
                _logs.Record(LogType.WhitelistChange, req.Args["path"], "removed");
                return IpcResponse.Ok();
            case "ClearLogs":
                _logs.Clear(); return IpcResponse.Ok();
            case "PauseEnforcement":
                _config.Paused = true; _logs.Record(LogType.EnforcementPaused, null, "paused"); return IpcResponse.Ok();
            case "ResumeEnforcement":
                _config.Paused = false; _logs.Record(LogType.EnforcementResumed, null, "resumed"); return IpcResponse.Ok();
            case "ChangeAdminPassword":
                _auth.SetAdminPassword(req.Args["newPassword"]); return IpcResponse.Ok();
            default:
                return IpcResponse.Fail($"unknown op {req.Op}");
        }
    }

    private bool Auth(IpcRequest req) => _tokens.IsValid(req.Token);
}
