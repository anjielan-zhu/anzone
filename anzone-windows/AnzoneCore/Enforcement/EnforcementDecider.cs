using System.Collections.Generic;
using System.IO;
using System.Linq;
using AnzoneCore.Util;

namespace AnzoneCore.Enforcement;

public enum EnforcementAction { Allow, BlockProcess, BlockInstaller }

public class EnforcementDecider
{
    private static readonly string[] CriticalNames =
        { "winlogon.exe", "csrss.exe", "services.exe", "svchost.exe", "lsass.exe",
          "wininit.exe", "smss.exe", "explorer.exe", "dwm.exe", "fontdrvhost.exe",
          "anzoneservice.exe", "anzonetray.exe" };

    private static readonly string[] InstallerNames = { "msiexec.exe" };

    private readonly HashSet<string> _whitelist;   // normalized paths
    private readonly bool _paused;

    public EnforcementDecider(HashSet<string> normalizedWhitelist, bool paused)
    {
        _whitelist = normalizedWhitelist;
        _paused = paused;
    }

    public EnforcementAction Decide(ProcessInfo p)
    {
        var norm = PathNormalizer.Normalize(p.ImagePath);
        var name = Path.GetFileName(norm);

        if (CriticalNames.Contains(name)) return EnforcementAction.Allow;        // (1)
        if (_paused) return EnforcementAction.Allow;                             // (2)
        if (IsInstaller(name)) return EnforcementAction.BlockInstaller;          // (3)
        if (PathNormalizer.IsUnderWindows(norm)) return EnforcementAction.Allow; // (4)
        if (_whitelist.Contains(norm)) return EnforcementAction.Allow;           // (5)
        return EnforcementAction.BlockProcess;                                   // (6)
    }

    private static bool IsInstaller(string fileName)
    {
        if (InstallerNames.Contains(fileName)) return true;
        return fileName.StartsWith("setup") || fileName.Contains("install");
    }
}
