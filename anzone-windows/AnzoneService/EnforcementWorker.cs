using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AnzoneCore.Auth;
using AnzoneCore.Data;
using AnzoneCore.Enforcement;
using AnzoneCore.Ipc;
using AnzoneCore.Model;
using Microsoft.Extensions.Hosting;

namespace AnzoneService;

public class EnforcementWorker : BackgroundService
{
    private readonly string _dataDir = @"C:\ProgramData\anzone";
    private AnzoneDb _db = null!;
    private WhitelistRepository _whitelist = null!;
    private LogRepository _logs = null!;
    private ConfigStore _config = null!;
    private PipeServer _pipe = null!;
    private readonly ProcessWatcher _watcher = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        System.IO.Directory.CreateDirectory(_dataDir);
        _db = new AnzoneDb($"Data Source={System.IO.Path.Combine(_dataDir, "anzone.db")}");
        _db.EnsureCreated();
        _whitelist = new WhitelistRepository(_db);
        _logs = new LogRepository(_db);
        _config = new ConfigStore(System.IO.Path.Combine(_dataDir, "config.json"));
        var auth = new AuthService(_config);
        var handler = new CommandHandler(_whitelist, _logs, auth, _config, new SessionTokens());
        _pipe = new PipeServer(handler);

        _logs.Record(LogType.ServiceStart, null, "service start");

        _watcher.ProcessStarted += OnProcessStarted;
        _watcher.StoppedUnexpectedly += () =>
        {
            try
            {
                _logs.Record(LogType.ServiceStart, null, "WMI watcher stopped unexpectedly; rebuilding");
                _watcher.Start();
            }
            catch (Exception ex)
            {
                _logs.Record(LogType.ServiceStop, null, $"WMI watcher rebuild failed: {ex.Message}");
            }
        };
        _watcher.Start();

        try { await _pipe.RunAsync(stoppingToken); }
        finally
        {
            _watcher.Dispose();
            _logs.Record(LogType.ServiceStop, null, "service stop");
        }
    }

    private void OnProcessStarted(int pid, string path)
    {
        var decider = new EnforcementDecider(_whitelist.GetNormalizedSet(), _config.Paused);
        var action = decider.Decide(new ProcessInfo(pid, path));
        if (action == EnforcementAction.Allow) return;
        try
        {
            Process.GetProcessById(pid).Kill();
            _logs.Record(action == EnforcementAction.BlockInstaller ? LogType.InstallBlocked : LogType.ProcessBlocked,
                path, $"killed pid {pid}");
        }
        catch (Exception ex) { _logs.Record(LogType.ProcessBlocked, path, $"kill failed: {ex.Message}"); }
    }
}
