using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using AnzoneCore.Ipc;

namespace AnzoneService;

public class PipeServer
{
    private const string PipeName = "anzone-control";
    private readonly CommandHandler _handler;
    public PipeServer(CommandHandler handler) => _handler = handler;

    public async Task RunAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            using var server = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 1,
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            try
            {
                await server.WaitForConnectionAsync(ct);
                using var reader = new StreamReader(server, leaveOpen: true);
                using var writer = new StreamWriter(server, leaveOpen: true) { AutoFlush = true };
                var line = await reader.ReadLineAsync();
                if (line is null) continue;
                IpcResponse resp;
                try { resp = _handler.Handle(IpcCodec.DeserializeRequest(line)); }
                catch (Exception ex) { resp = IpcResponse.Fail(ex.Message); }
                await writer.WriteLineAsync(IpcCodec.Serialize(resp));
            }
            catch (OperationCanceledException) { break; }
            catch { /* drop bad client, keep serving */ }
        }
    }
}
