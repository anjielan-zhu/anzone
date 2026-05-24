using AnzoneCore.Util;
using Xunit;

public class PathNormalizerTests
{
    [Fact] public void LowercasesAndTrims() =>
        Assert.Equal(@"c:\program files\app\a.exe",
            PathNormalizer.Normalize(@"  C:\Program Files\App\A.EXE "));

    [Fact] public void NullOrEmptyReturnsEmpty()
    {
        Assert.Equal("", PathNormalizer.Normalize(null));
        Assert.Equal("", PathNormalizer.Normalize("   "));
    }

    [Fact] public void IsUnderWindowsDir()
    {
        Assert.True(PathNormalizer.IsUnderWindows(@"C:\Windows\System32\svchost.exe"));
        Assert.False(PathNormalizer.IsUnderWindows(@"C:\Program Files\App\a.exe"));
    }
}
