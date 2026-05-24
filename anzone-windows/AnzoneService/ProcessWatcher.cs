using System;
using System.Management;

namespace AnzoneService;

public class ProcessWatcher : IDisposable
{
    private ManagementEventWatcher? _watcher;
    private bool _disposed;
    public event Action<int, string>? ProcessStarted;
    public event Action? StoppedUnexpectedly;

    public void Start()
    {
        _watcher = new ManagementEventWatcher(
            new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace"));
        _watcher.EventArrived += (_, e) =>
        {
            try
            {
                var pid = Convert.ToInt32(e.NewEvent.Properties["ProcessID"].Value);
                var path = ResolvePath(pid);
                if (path != null) ProcessStarted?.Invoke(pid, path);
            }
            catch { /* per-event failure must not kill the watcher */ }
        };
        _watcher.Stopped += (_, _) =>
        {
            if (!_disposed) StoppedUnexpectedly?.Invoke();
        };
        _watcher.Start();
    }

    private static string? ResolvePath(int pid)
    {
        try { return System.Diagnostics.Process.GetProcessById(pid).MainModule?.FileName; }
        catch { return null; }
    }

    public void Dispose()
    {
        _disposed = true;
        _watcher?.Stop();
        _watcher?.Dispose();
    }
}
