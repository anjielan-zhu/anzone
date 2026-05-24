using System.Collections.Generic;
using System.Text.Json;
using System.Windows.Forms;
using AnzoneCore.Ipc;
using AnzoneCore.Model;

namespace AnzoneTray;

public class WhitelistForm : Form
{
    private readonly PipeClient _client; private readonly string _token;
    private readonly ListBox _list = new() { Dock = DockStyle.Fill };

    public WhitelistForm(PipeClient client, string token)
    {
        _client = client; _token = token;
        Text = L.T("whitelist_title"); Width = 520; Height = 420;
        var add = new Button { Text = L.T("add_exe"), Dock = DockStyle.Bottom };
        var del = new Button { Text = L.T("remove_sel"), Dock = DockStyle.Bottom };
        add.Click += (_, _) => AddExe();
        del.Click += (_, _) => RemoveSelected();
        Controls.Add(_list); Controls.Add(del); Controls.Add(add);
        Reload();
    }

    private void Reload()
    {
        _list.Items.Clear();
        var r = _client.Send(new IpcRequest("ListWhitelist", null, new()));
        if (r.Success && r.Payload != null)
            foreach (var e in JsonSerializer.Deserialize<List<WhitelistEntry>>(r.Payload)!)
                _list.Items.Add($"{e.DisplayName}  |  {e.ImagePath}");
    }

    private void AddExe()
    {
        using var dlg = new OpenFileDialog { Filter = L.T("file_filter") };
        if (dlg.ShowDialog() != DialogResult.OK) return;
        _client.Send(new IpcRequest("AddWhitelist", _token,
            new Dictionary<string,string>{{"path", dlg.FileName},{"name", System.IO.Path.GetFileName(dlg.FileName)}}));
        Reload();
    }

    private void RemoveSelected()
    {
        if (_list.SelectedItem is not string s) return;
        var path = s.Split("|")[1].Trim();
        _client.Send(new IpcRequest("RemoveWhitelist", _token, new Dictionary<string,string>{{"path", path}}));
        Reload();
    }
}
