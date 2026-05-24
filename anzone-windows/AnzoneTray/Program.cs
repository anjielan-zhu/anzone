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
        L.Init();
        ApplicationConfiguration.Initialize();
        var menu = new ContextMenuStrip();
        var icon = new NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Shield,
            Visible = true, Text = L.T("tray_tip"),
            ContextMenuStrip = menu
        };

        menu.Items.Add(L.T("menu_setup"), null, (_, _) => RunSetup());
        menu.Items.Add(L.T("menu_login"), null, (_, _) =>
        {
            using var f = new LoginForm(Client);
            if (f.ShowDialog() == DialogResult.OK) _token = f.Token;
        });
        menu.Items.Add(L.T("menu_whitelist"), null, (_, _) => { if (RequireLogin()) new WhitelistForm(Client, _token!).Show(); });
        menu.Items.Add(L.T("menu_logs"), null, (_, _) => { if (RequireLogin()) new LogsForm(Client, _token!).Show(); });
        menu.Items.Add(L.T("menu_pause"), null, (_, _) => { if (RequireLogin()) Client.Send(new IpcRequest("PauseEnforcement", _token, new())); });
        menu.Items.Add(L.T("menu_resume"), null, (_, _) => { if (RequireLogin()) Client.Send(new IpcRequest("ResumeEnforcement", _token, new())); });
        var langMenu = new ToolStripMenuItem(L.T("menu_language"));
        langMenu.DropDownItems.Add(L.T("lang_system"), null, (_, _) => { L.SetLang("system"); Application.Restart(); });
        langMenu.DropDownItems.Add(L.T("lang_hans"), null, (_, _) => { L.SetLang("zh-Hans"); Application.Restart(); });
        langMenu.DropDownItems.Add(L.T("lang_hant"), null, (_, _) => { L.SetLang("zh-Hant"); Application.Restart(); });
        menu.Items.Add(langMenu);
        menu.Items.Add(L.T("menu_exit"), null, (_, _) => { icon.Visible = false; Application.Exit(); });

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
