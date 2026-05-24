using System;
using System.IO;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using AnzoneCore.Ipc;

namespace AnzoneService;

public class PipeServer
{
    private const string PipeName = "anzone-control";
    private readonly CommandHandler _handler;
    public PipeServer(CommandHandler handler) => _handler = handler;

    private static NamedPipeServerStream CreateServerStream()
    {
        var security = new PipeSecurity();
        security.AddAccessRule(new PipeAccessRule(
            new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null),
            PipeAccessRights.ReadWrite, AccessControlType.Allow));
        security.AddAccessRule(new PipeAccessRule(
            new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
            PipeAccessRights.FullControl, AccessControlType.Allow));
        security.AddAccessRule(new PipeAccessRule(
            new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null),
            PipeAccessRights.FullControl, AccessControlType.Allow));
        return NamedPipeServerStreamAcl.Create(PipeName, PipeDirection.InOut, 1,
            PipeTransmissionMode.Byte, PipeOptions.Asynchronous, 0, 0, security);
    }

    public async Task RunAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            using var server = CreateServerStream();
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
