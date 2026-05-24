using System.IO;
using System.Text.Json;

namespace AnzoneCore.Auth;

public class ConfigStore
{
    private readonly string _path;
    private Data _data;

    private class Data
    {
        public string? AdminHash { get; set; }
        public string? AdminSalt { get; set; }
        public bool Paused { get; set; } = true;     // safe default: paused on fresh install
        public bool FirstRun { get; set; } = true;
    }

    public ConfigStore(string path)
    {
        _path = path;
        _data = File.Exists(_path)
            ? JsonSerializer.Deserialize<Data>(File.ReadAllText(_path)) ?? new Data()
            : new Data();
    }

    private void Save() => File.WriteAllText(_path, JsonSerializer.Serialize(_data));

    public string? AdminHash { get => _data.AdminHash; set { _data.AdminHash = value; Save(); } }
    public string? AdminSalt { get => _data.AdminSalt; set { _data.AdminSalt = value; Save(); } }
    public bool Paused { get => _data.Paused; set { _data.Paused = value; Save(); } }
    public bool FirstRun { get => _data.FirstRun; set { _data.FirstRun = value; Save(); } }
}
