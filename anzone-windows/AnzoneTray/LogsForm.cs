using System.Collections.Generic;
using System.Text.Json;
using System.Windows.Forms;
using AnzoneCore.Ipc;
using AnzoneCore.Model;

namespace AnzoneTray;

public class LogsForm : Form
{
    public LogsForm(PipeClient client, string token)
    {
        Text = L.T("logs_title"); Width = 640; Height = 480;
        var box = new ListBox { Dock = DockStyle.Fill, HorizontalScrollbar = true };
        Controls.Add(box);
        var r = client.Send(new IpcRequest("GetLogs", token, new()));
        if (r.Success && r.Payload != null)
            foreach (var e in JsonSerializer.Deserialize<List<LogEntry>>(r.Payload)!)
                box.Items.Add($"{e.Type}  {e.Detail}  {e.ImagePath}");
    }
}
