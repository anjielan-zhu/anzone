using System.Collections.Generic;
using System.Windows.Forms;
using AnzoneCore.Ipc;

namespace AnzoneTray;

public class SetupForm : Form
{
    private readonly TextBox _pw = new() { UseSystemPasswordChar = true, Dock = DockStyle.Top };
    public string? Token { get; private set; }

    public SetupForm(PipeClient client)
    {
        Text = L.T("setup_title"); Width = 360; Height = 160;
        var btn = new Button { Text = L.T("setup_btn"), Dock = DockStyle.Bottom };
        var lbl = new Label { Text = L.T("setup_hint"), Dock = DockStyle.Bottom, Height = 24 };
        btn.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(_pw.Text)) { MessageBox.Show(L.T("pw_empty")); return; }
            var r = client.Send(new IpcRequest("SetupAdmin", null,
                new Dictionary<string,string>{{"password", _pw.Text}}));
            if (r.Success) { Token = r.Payload; DialogResult = DialogResult.OK; Close(); }
            else MessageBox.Show(r.Error ?? L.T("setup_fail"));
        };
        Controls.Add(_pw); Controls.Add(lbl); Controls.Add(btn);
    }
}
