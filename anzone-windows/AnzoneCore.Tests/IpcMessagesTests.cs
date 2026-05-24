using AnzoneCore.Ipc;
using Xunit;

public class IpcMessagesTests
{
    [Fact] public void RequestRoundTrips()
    {
        var req = new IpcRequest("AddWhitelist", "tok",
            new System.Collections.Generic.Dictionary<string,string>{{"path",@"C:\a.exe"},{"name","A"}});
        var json = IpcCodec.Serialize(req);
        var back = IpcCodec.DeserializeRequest(json);
        Assert.Equal("AddWhitelist", back.Op);
        Assert.Equal("tok", back.Token);
        Assert.Equal(@"C:\a.exe", back.Args["path"]);
    }

    [Fact] public void ResponseRoundTrips()
    {
        var resp = IpcResponse.Ok("{\"x\":1}");
        var json = IpcCodec.Serialize(resp);
        var back = IpcCodec.DeserializeResponse(json);
        Assert.True(back.Success);
        Assert.Equal("{\"x\":1}", back.Payload);
    }

    [Fact] public void ErrorResponse()
    {
        var back = IpcCodec.DeserializeResponse(IpcCodec.Serialize(IpcResponse.Fail("bad")));
        Assert.False(back.Success);
        Assert.Equal("bad", back.Error);
    }
}
