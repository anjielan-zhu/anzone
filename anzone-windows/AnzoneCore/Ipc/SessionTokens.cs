using System;
using System.Collections.Concurrent;

namespace AnzoneCore.Ipc;

public class SessionTokens
{
    private readonly ConcurrentDictionary<string, DateTime> _tokens = new();
    private readonly TimeSpan _ttl = TimeSpan.FromMinutes(15);

    public string Issue()
    {
        var tok = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        _tokens[tok] = DateTime.UtcNow;
        return tok;
    }

    public bool IsValid(string? token)
    {
        if (token is null || !_tokens.TryGetValue(token, out var issued)) return false;
        if (DateTime.UtcNow - issued > _ttl) { _tokens.TryRemove(token, out _); return false; }
        return true;
    }
}
