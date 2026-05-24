using System;
using AnzoneCore.Security;

namespace AnzoneCore.Auth;

public class AuthService
{
    private readonly ConfigStore _config;
    public AuthService(ConfigStore config) => _config = config;

    public bool IsFirstRun() => _config.FirstRun;

    public void SetAdminPassword(string password)
    {
        var salt = PasswordHasher.GenerateSalt();
        var hash = PasswordHasher.Hash(password, salt);
        _config.AdminSalt = Convert.ToBase64String(salt);
        _config.AdminHash = Convert.ToBase64String(hash);
        _config.FirstRun = false;
    }

    public bool Verify(string password)
    {
        if (_config.AdminHash is null || _config.AdminSalt is null) return false;
        var salt = Convert.FromBase64String(_config.AdminSalt);
        var hash = Convert.FromBase64String(_config.AdminHash);
        return PasswordHasher.Verify(password, salt, hash);
    }
}
