using System.Collections.Generic;
using System.Text.Json;

namespace AnzoneCore.Ipc;

public record IpcRequest(string Op, string? Token, Dictionary<string, string> Args);

public record IpcResponse(bool Success, string? Payload, string? Error)
{
    public static IpcResponse Ok(string? payload = null) => new(true, payload, null);
    public static IpcResponse Fail(string error) => new(false, null, error);
}

public static class IpcCodec
{
    public static string Serialize(IpcRequest r) => JsonSerializer.Serialize(r);
    public static string Serialize(IpcResponse r) => JsonSerializer.Serialize(r);
    public static IpcRequest DeserializeRequest(string json) =>
        JsonSerializer.Deserialize<IpcRequest>(json)!;
    public static IpcResponse DeserializeResponse(string json) =>
        JsonSerializer.Deserialize<IpcResponse>(json)!;
}
