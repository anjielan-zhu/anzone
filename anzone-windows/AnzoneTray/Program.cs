using System;
using System.Collections.Generic;
using System.Text.Json;
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

        menu.Items.Add("Initial setup", null, (_, _) => RunSetup());
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

        if (IsFirstRun()) RunSetup();

        Application.Run();
    }

    private static bool IsFirstRun()
    {
        var r = Client.Send(new IpcRequest("GetStatus", null, new()));
        if (!r.Success || r.Payload is null) return false;
        try
        {
            using var doc = JsonDocument.Parse(r.Payload);
            return doc.RootElement.TryGetProperty("firstRun", out var fr) && fr.GetBoolean();
        }
        catch { return false; }
    }

    private static void RunSetup()
    {
        using var f = new SetupForm(Client);
        if (f.ShowDialog() == DialogResult.OK) _token = f.Token;
    }

    private static bool RequireLogin()
    {
        if (_token != null) return true;
        using var f = new LoginForm(Client);
        if (f.ShowDialog() == DialogResult.OK) { _token = f.Token; return true; }
        return false;
    }
}
