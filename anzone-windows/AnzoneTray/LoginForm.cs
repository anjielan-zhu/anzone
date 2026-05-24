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
        Text = "anzone admin login"; Width = 320; Height = 140;
        var btn = new Button { Text = "Login", Dock = DockStyle.Bottom };
        btn.Click += (_, _) =>
        {
            var r = client.Send(new IpcRequest("Login", null, new Dictionary<string,string>{{"password", _pw.Text}}));
            if (r.Success) { Token = r.Payload; DialogResult = DialogResult.OK; Close(); }
            else MessageBox.Show("Wrong password or service not running");
        };
        Controls.Add(_pw); Controls.Add(btn);
    }
}
