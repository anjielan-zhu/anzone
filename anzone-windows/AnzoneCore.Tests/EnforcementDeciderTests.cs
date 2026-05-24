using System.Collections.Generic;
using AnzoneCore.Enforcement;
using AnzoneCore.Util;
using Xunit;

public class EnforcementDeciderTests
{
    private static EnforcementDecider Make(bool paused, params string[] whitelist)
    {
        var set = new HashSet<string>();
        foreach (var w in whitelist) set.Add(PathNormalizer.Normalize(w));
        return new EnforcementDecider(set, paused);
    }

    private static ProcessInfo P(string path) => new(1234, path);

    [Fact] public void CriticalSystemAlwaysAllowed()
    {
        var d = Make(false);
        Assert.Equal(EnforcementAction.Allow, d.Decide(P(@"C:\Windows\System32\winlogon.exe")));
        Assert.Equal(EnforcementAction.Allow, d.Decide(P(@"C:\Windows\explorer.exe")));
    }

    [Fact] public void PausedAllowsEverything()
    {
        var d = Make(true);
        Assert.Equal(EnforcementAction.Allow, d.Decide(P(@"C:\Program Files\Foo\foo.exe")));
        Assert.Equal(EnforcementAction.Allow, d.Decide(P(@"C:\Windows\System32\msiexec.exe")));
    }

    [Fact] public void InstallerBlockedWhenEnforcing()
    {
        var d = Make(false);
        Assert.Equal(EnforcementAction.BlockInstaller, d.Decide(P(@"C:\Windows\System32\msiexec.exe")));
        Assert.Equal(EnforcementAction.BlockInstaller, d.Decide(P(@"C:\Users\x\Downloads\setup.exe")));
        Assert.Equal(EnforcementAction.BlockInstaller, d.Decide(P(@"C:\Users\x\Downloads\AppInstaller.exe")));
    }

    [Fact] public void WindowsBinariesAllowed()
    {
        var d = Make(false);
        Assert.Equal(EnforcementAction.Allow, d.Decide(P(@"C:\Windows\System32\notepad.exe")));
    }

    [Fact] public void WhitelistedAllowed()
    {
        var d = Make(false, @"C:\Program Files\Foo\foo.exe");
        Assert.Equal(EnforcementAction.Allow, d.Decide(P(@"C:\Program Files\Foo\FOO.EXE")));
    }

    [Fact] public void NonWhitelistedBlocked()
    {
        var d = Make(false, @"C:\Program Files\Foo\foo.exe");
        Assert.Equal(EnforcementAction.BlockProcess, d.Decide(P(@"C:\Program Files\Bar\bar.exe")));
    }
}
