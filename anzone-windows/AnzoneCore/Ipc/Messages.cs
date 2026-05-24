using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AnzoneCore.Ipc;

public record IpcRequest(string Op, string? Token, Dictionary<string, string> Args);

public interface IIpcResponse
{
    bool Success { get; }
    string? Payload { get; }
    string? Error { get; }
}

public sealed class IpcResponse : IIpcResponse
{
    [JsonConstructor]
    public IpcResponse(bool success, string? payload, string? errorMsg)
    {
        _success = success;
        _payload = payload;
        _error = errorMsg;
    }

    private readonly bool _success;
    private readonly string? _payload;
    private readonly string? _error;

    // JSON property names must match constructor parameter names (case-insensitive)
    public bool Success => _success;
    public string? Payload => _payload;
    [JsonPropertyName("ErrorMsg")]
    public string? ErrorMsg => _error;

    bool IIpcResponse.Success => _success;
    string? IIpcResponse.Payload => _payload;
    string? IIpcResponse.Error => _error;

    public static IpcResponse Ok(string? payload = null) => new(true, payload, null);
    public static IpcResponse Error(string error) => new(false, null, error);
}

public static class IpcCodec
{
    public static string Serialize(IpcRequest r) => JsonSerializer.Serialize(r);
    public static string Serialize(IpcResponse r) => JsonSerializer.Serialize(r);
    public static IpcRequest DeserializeRequest(string json) =>
        JsonSerializer.Deserialize<IpcRequest>(json)!;
    public static IIpcResponse DeserializeResponse(string json) =>
        JsonSerializer.Deserialize<IpcResponse>(json)!;
}
