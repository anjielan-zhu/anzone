using System.Collections.Generic;
using System.Windows.Forms;
using AnzoneCore.Ipc;

namespace AnzoneTray;

public class LoginForm : Form
{
    private readonly TextBox _pw = new() { UseSystemPasswordChar = true, Dock = DockStyle.Top };
    public string? Token { get; private set; }

    public LoginForm(PipeClient client)
    {
        Text = L.T("login_title"); Width = 320; Height = 140;
        var btn = new Button { Text = L.T("login_btn"), Dock = DockStyle.Bottom };
        btn.Click += (_, _) =>
        {
            var r = client.Send(new IpcRequest("Login", null, new Dictionary<string,string>{{"password", _pw.Text}}));
            if (r.Success) { Token = r.Payload; DialogResult = DialogResult.OK; Close(); }
            else MessageBox.Show(L.T("login_fail"));
        };
        Controls.Add(_pw); Controls.Add(btn);
    }
}
