using System.IO;
using System.IO.Pipes;
using AnzoneCore.Ipc;

namespace AnzoneTray;

public class PipeClient
{
    private const string PipeName = "anzone-control";

    public IpcResponse Send(IpcRequest req)
    {
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut);
            client.Connect(3000);
            using var writer = new StreamWriter(client, leaveOpen: true) { AutoFlush = true };
            using var reader = new StreamReader(client, leaveOpen: true);
            writer.WriteLine(IpcCodec.Serialize(req));
            var line = reader.ReadLine();
            return line is null ? IpcResponse.Fail("no response") : IpcCodec.DeserializeResponse(line);
        }
        catch (System.Exception ex) { return IpcResponse.Fail($"not connected: {ex.Message}"); }
    }
}
