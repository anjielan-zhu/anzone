using System;
using System.Windows.Forms;
using AnzoneCore.Ipc;

namespace AnzoneTray;

static class Program
{
    private static readonly PipeClient Client = new();
    private static string? _token;

    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        var menu = new ContextMenuStrip();
        var icon = new NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Shield,
            Visible = true, Text = "anzone control",
            ContextMenuStrip = menu
        };

        menu.Items.Add("Admin login", null, (_, _) =>
        {
            using var f = new LoginForm(Client);
            if (f.ShowDialog() == DialogResult.OK) _token = f.Token;
        });
        menu.Items.Add("Whitelist", null, (_, _) => { if (RequireLogin()) new WhitelistForm(Client, _token!).Show(); });
        menu.Items.Add("Logs", null, (_, _) => { if (RequireLogin()) new LogsForm(Client, _token!).Show(); });
        menu.Items.Add("Pause enforcement", null, (_, _) => { if (RequireLogin()) Client.Send(new IpcRequest("PauseEnforcement", _token, new())); });
        menu.Items.Add("Resume enforcement", null, (_, _) => { if (RequireLogin()) Client.Send(new IpcRequest("ResumeEnforcement", _token, new())); });
        menu.Items.Add("Exit tray", null, (_, _) => { icon.Visible = false; Application.Exit(); });

        Application.Run();
    }

    private static bool RequireLogin()
    {
        if (_token != null) return true;
        using var f = new LoginForm(Client);
        if (f.ShowDialog() == DialogResult.OK) { _token = f.Token; return true; }
        return false;
    }
}
