# anzone-windows 企业管控客户端 MVP 实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 构建一个 user-mode Windows 服务（C#/.NET 8）+ WinForms 托盘管理程序：监听进程创建、终止白名单外程序、拦截安装器、本地日志，管理员可暂停管控。全 Windows 版本通用（含 Home），不依赖组策略/AppLocker。

**Architecture:** 三工程方案——`AnzoneCore`（共享类库，纯逻辑，可单测）+ `AnzoneService`（Worker Service，LocalSystem，WMI 监听 + 拦截 + 命名管道 server）+ `AnzoneTray`（WinForms 托盘，命名管道 client）。数据存 `C:\ProgramData\anzone\`（SQLite + config.json）。拦截为反应式（WMI 事件 → Process.Kill）。

**Tech Stack:** C# / .NET 8、Worker Service、WinForms（net8.0-windows）、System.Management（WMI）、Microsoft.Data.Sqlite、System.IO.Pipes（命名管道）、System.Text.Json、xUnit。

参考设计：`docs/superpowers/specs/2026-05-24-anzone-windows-design.md`

---

## 环境（关键）

- **dotnet host 固定路径**：`C:\Program Files\dotnet\dotnet.exe`（SDK 8.0.421 已装，WindowsDesktop 运行时已装）。harness 进程 PATH 不含 dotnet，**所有命令用完整路径调用**：`& 'C:\Program Files\dotnet\dotnet.exe' ...`
- Shell：Windows PowerShell（链式用 `;`，非 `&&`）。
- 工程根：`C:\myproject\anzone-windows`。Git 仓库在 `C:\myproject`，分支 `feature/anzone-windows`，从 `C:\myproject` 提交。
- `.gitignore` 已含 `**/build/`；需补 `bin/`、`obj/`（Task 0 处理）。

---

## 文件结构

```
anzone-windows/
├── anzone-windows.sln
├── AnzoneCore/                      (classlib, net8.0 — 纯逻辑, 可单测)
│   ├── AnzoneCore.csproj
│   ├── Security/PasswordHasher.cs
│   ├── Util/PathNormalizer.cs
│   ├── Model/WhitelistEntry.cs
│   ├── Model/LogEntry.cs            (含 LogType enum)
│   ├── Enforcement/ProcessInfo.cs
│   ├── Enforcement/EnforcementDecider.cs   ← 拦截决策核心
│   ├── Data/AnzoneDb.cs             (SQLite 连接/建表)
│   ├── Data/WhitelistRepository.cs
│   ├── Data/LogRepository.cs
│   ├── Auth/ConfigStore.cs          (config.json: 密码哈希/paused/first_run)
│   ├── Auth/AuthService.cs
│   └── Ipc/Messages.cs              (请求/响应契约 + JSON)
├── AnzoneService/                   (worker, net8.0-windows)
│   ├── AnzoneService.csproj
│   ├── Program.cs                   (Host + DI + 服务安装)
│   ├── ProcessWatcher.cs            (WMI Win32_ProcessStartTrace)
│   ├── EnforcementWorker.cs         (BackgroundService: 接 watcher → decider → kill)
│   └── PipeServer.cs                (命名管道 server + 命令分发)
├── AnzoneTray/                      (WinForms, net8.0-windows)
│   ├── AnzoneTray.csproj
│   ├── Program.cs                   (NotifyIcon + 菜单)
│   ├── PipeClient.cs
│   ├── LoginForm.cs / WhitelistForm.cs / LogsForm.cs
├── AnzoneCore.Tests/                (xUnit, net8.0)
│   └── *.cs
├── scripts/install-service.ps1 / uninstall-service.ps1
└── docs/DEPLOYMENT.md
```

---

## 测试约定
- `AnzoneCore.Tests`（xUnit）覆盖 Core 纯逻辑：决策器、仓库、鉴权、路径规范化、IPC 序列化。本机可跑：`& 'C:\Program Files\dotnet\dotnet.exe' test`。
- WMI 监听、Process.Kill、命名管道端到端、服务生命周期、SCM 恢复、托盘 UI → 手动验收（Task 13），无法 JVM/CI 单测。
- 每个 task 末尾 commit（conventional commits）。

---

## Task 0: 解决方案脚手架

**Files:** 创建 sln + 4 个工程 + .gitignore 追加。

- [ ] **Step 1: 创建解决方案与工程**

从 `C:\myproject\anzone-windows`（先 `New-Item -ItemType Directory`）运行：
```
& 'C:\Program Files\dotnet\dotnet.exe' new sln -n anzone-windows
& 'C:\Program Files\dotnet\dotnet.exe' new classlib -n AnzoneCore -f net8.0
& 'C:\Program Files\dotnet\dotnet.exe' new worker -n AnzoneService -f net8.0
& 'C:\Program Files\dotnet\dotnet.exe' new winforms -n AnzoneTray -f net8.0
& 'C:\Program Files\dotnet\dotnet.exe' new xunit -n AnzoneCore.Tests -f net8.0
```
删除每个新工程自带的 `Class1.cs` / `UnitTest1.cs` 占位文件。

- [ ] **Step 2: 设定 Windows TFM + 引用关系**

把 `AnzoneService/AnzoneService.csproj` 和 `AnzoneTray/AnzoneTray.csproj` 的 `<TargetFramework>` 改为 `net8.0-windows`。`AnzoneTray.csproj` 确保有 `<UseWindowsForms>true</UseWindowsForms>` 和 `<OutputType>WinExe</OutputType>`。

加引用与包：
```
& 'C:\Program Files\dotnet\dotnet.exe' sln add AnzoneCore AnzoneService AnzoneTray AnzoneCore.Tests
& 'C:\Program Files\dotnet\dotnet.exe' add AnzoneService reference AnzoneCore
& 'C:\Program Files\dotnet\dotnet.exe' add AnzoneTray reference AnzoneCore
& 'C:\Program Files\dotnet\dotnet.exe' add AnzoneCore.Tests reference AnzoneCore
& 'C:\Program Files\dotnet\dotnet.exe' add AnzoneCore package Microsoft.Data.Sqlite --version 8.0.8
& 'C:\Program Files\dotnet\dotnet.exe' add AnzoneService package System.Management --version 8.0.0
& 'C:\Program Files\dotnet\dotnet.exe' add AnzoneService package Microsoft.Extensions.Hosting.WindowsServices --version 8.0.1
```

- [ ] **Step 3: 追加 .gitignore**

在 `C:\myproject\.gitignore` 末尾追加：
```
# .NET
anzone-windows/**/bin/
anzone-windows/**/obj/
```

- [ ] **Step 4: 验证构建 + 空测试**
```
& 'C:\Program Files\dotnet\dotnet.exe' build C:\myproject\anzone-windows\anzone-windows.sln
& 'C:\Program Files\dotnet\dotnet.exe' test C:\myproject\anzone-windows\anzone-windows.sln
```
Expected: Build succeeded；test 运行（0 个测试或被删占位后 0 passed）。

- [ ] **Step 5: Commit**（从 `C:\myproject`）
```
git add .gitignore anzone-windows
git commit -m "chore: scaffold anzone-windows .NET solution"
```

---

## Task 1: PasswordHasher（PBKDF2，与 Spec B 同算法）

**Files:** Create `AnzoneCore/Security/PasswordHasher.cs`; Test `AnzoneCore.Tests/PasswordHasherTests.cs`.

- [ ] **Step 1: 写失败测试**

`AnzoneCore.Tests/PasswordHasherTests.cs`:
```csharp
using AnzoneCore.Security;
using Xunit;

public class PasswordHasherTests
{
    [Fact]
    public void SameInputsProduceSameHash()
    {
        var salt = PasswordHasher.GenerateSalt();
        Assert.Equal(PasswordHasher.Hash("secret123", salt), PasswordHasher.Hash("secret123", salt));
    }

    [Fact]
    public void DifferentSaltDiffersHash()
    {
        Assert.NotEqual(
            PasswordHasher.Hash("secret", PasswordHasher.GenerateSalt()),
            PasswordHasher.Hash("secret", PasswordHasher.GenerateSalt()));
    }

    [Fact]
    public void VerifyAcceptsCorrect()
    {
        var salt = PasswordHasher.GenerateSalt();
        var hash = PasswordHasher.Hash("correct", salt);
        Assert.True(PasswordHasher.Verify("correct", salt, hash));
    }

    [Fact]
    public void VerifyRejectsWrong()
    {
        var salt = PasswordHasher.GenerateSalt();
        var hash = PasswordHasher.Hash("correct", salt);
        Assert.False(PasswordHasher.Verify("wrong", salt, hash));
    }
}
```

- [ ] **Step 2: 运行确认失败**
```
& 'C:\Program Files\dotnet\dotnet.exe' test C:\myproject\anzone-windows --filter PasswordHasherTests
```
Expected: 编译失败（PasswordHasher 未定义）。

- [ ] **Step 3: 实现** `AnzoneCore/Security/PasswordHasher.cs`:
```csharp
using System.Security.Cryptography;

namespace AnzoneCore.Security;

public static class PasswordHasher
{
    private const int Iterations = 100_000;
    private const int KeyLength = 32;   // 256-bit
    private const int SaltLength = 16;

    public static byte[] GenerateSalt() => RandomNumberGenerator.GetBytes(SaltLength);

    public static byte[] Hash(string password, byte[] salt) =>
        Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeyLength);

    public static bool Verify(string password, byte[] salt, byte[] expected) =>
        CryptographicOperations.FixedTimeEquals(Hash(password, salt), expected);
}
```

- [ ] **Step 4: 运行确认通过**（同 Step 2 命令）Expected: 4 passed。
- [ ] **Step 5: Commit**
```
git add anzone-windows/AnzoneCore/Security/PasswordHasher.cs anzone-windows/AnzoneCore.Tests/PasswordHasherTests.cs
git commit -m "feat: add PBKDF2 password hasher (core)"
```

---

## Task 2: PathNormalizer

**Files:** Create `AnzoneCore/Util/PathNormalizer.cs`; Test `AnzoneCore.Tests/PathNormalizerTests.cs`.

- [ ] **Step 1: 写失败测试**
```csharp
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
```

- [ ] **Step 2: 运行确认失败**（`--filter PathNormalizerTests`）。
- [ ] **Step 3: 实现** `AnzoneCore/Util/PathNormalizer.cs`:
```csharp
namespace AnzoneCore.Util;

public static class PathNormalizer
{
    public static string Normalize(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "";
        return path.Trim().Replace('/', '\\').ToLowerInvariant();
    }

    public static bool IsUnderWindows(string? path)
    {
        var n = Normalize(path);
        return n.StartsWith(@"c:\windows\");
    }
}
```

- [ ] **Step 4: 运行确认通过**（3 passed）。
- [ ] **Step 5: Commit**
```
git add anzone-windows/AnzoneCore/Util/PathNormalizer.cs anzone-windows/AnzoneCore.Tests/PathNormalizerTests.cs
git commit -m "feat: add path normalizer (core)"
```

---

## Task 3: 领域模型 + LogType

**Files:** Create `AnzoneCore/Model/WhitelistEntry.cs`, `AnzoneCore/Model/LogEntry.cs`, `AnzoneCore/Enforcement/ProcessInfo.cs`.

- [ ] **Step 1: 实现（无独立测试——纯数据；下游 task 的测试覆盖）**

`AnzoneCore/Model/WhitelistEntry.cs`:
```csharp
namespace AnzoneCore.Model;

public record WhitelistEntry(string ImagePath, string DisplayName, long AddedAtUtc);
```

`AnzoneCore/Model/LogEntry.cs`:
```csharp
namespace AnzoneCore.Model;

public enum LogType
{
    ProcessBlocked, InstallBlocked, AdminLogin, LoginFailed,
    WhitelistChange, EnforcementPaused, EnforcementResumed, ServiceStart, ServiceStop
}

public record LogEntry(long Id, long TimestampUtc, LogType Type, string? ImagePath, string Detail);
```

`AnzoneCore/Enforcement/ProcessInfo.cs`:
```csharp
namespace AnzoneCore.Enforcement;

public record ProcessInfo(int Pid, string ImagePath);
```

- [ ] **Step 2: 构建验证**
```
& 'C:\Program Files\dotnet\dotnet.exe' build C:\myproject\anzone-windows\AnzoneCore
```
Expected: succeeded。

- [ ] **Step 3: Commit**
```
git add anzone-windows/AnzoneCore/Model anzone-windows/AnzoneCore/Enforcement/ProcessInfo.cs
git commit -m "feat: add domain models and LogType"
```

---

## Task 4: EnforcementDecider（拦截决策核心）

**Files:** Create `AnzoneCore/Enforcement/EnforcementDecider.cs`; Test `AnzoneCore.Tests/EnforcementDeciderTests.cs`.

决策顺序（重要）：① 关键系统进程→Allow ② 已暂停→Allow ③ 命中安装器→BlockInstaller ④ C:\Windows 下→Allow ⑤ 白名单→Allow ⑥ 否则→BlockProcess。

- [ ] **Step 1: 写失败测试**
```csharp
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
        var d = Make(false); // empty whitelist, enforcing
        Assert.Equal(EnforcementAction.Allow, d.Decide(P(@"C:\Windows\System32\winlogon.exe")));
        Assert.Equal(EnforcementAction.Allow, d.Decide(P(@"C:\Windows\explorer.exe")));
    }

    [Fact] public void PausedAllowsEverythingExceptNothing()
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
```

- [ ] **Step 2: 运行确认失败**（`--filter EnforcementDeciderTests`）。
- [ ] **Step 3: 实现** `AnzoneCore/Enforcement/EnforcementDecider.cs`:
```csharp
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

        if (CriticalNames.Contains(name)) return EnforcementAction.Allow;   // ①
        if (_paused) return EnforcementAction.Allow;                        // ②
        if (IsInstaller(name)) return EnforcementAction.BlockInstaller;     // ③
        if (PathNormalizer.IsUnderWindows(norm)) return EnforcementAction.Allow; // ④
        if (_whitelist.Contains(norm)) return EnforcementAction.Allow;      // ⑤
        return EnforcementAction.BlockProcess;                              // ⑥
    }

    private static bool IsInstaller(string fileName)
    {
        if (InstallerNames.Contains(fileName)) return true;
        return fileName.StartsWith("setup") || fileName.Contains("install");
    }
}
```

- [ ] **Step 4: 运行确认通过**（6 passed）。
- [ ] **Step 5: Commit**
```
git add anzone-windows/AnzoneCore/Enforcement/EnforcementDecider.cs anzone-windows/AnzoneCore.Tests/EnforcementDeciderTests.cs
git commit -m "feat: add enforcement decider with system-baseline safety"
```

---

## Task 5: AnzoneDb + WhitelistRepository

**Files:** Create `AnzoneCore/Data/AnzoneDb.cs`, `AnzoneCore/Data/WhitelistRepository.cs`; Test `AnzoneCore.Tests/WhitelistRepositoryTests.cs`.

- [ ] **Step 1: 写失败测试**（用 `Data Source=:memory:` 但需共享连接；这里用临时文件）
```csharp
using System.IO;
using System.Linq;
using AnzoneCore.Data;
using Xunit;

public class WhitelistRepositoryTests : System.IDisposable
{
    private readonly string _file = Path.Combine(Path.GetTempPath(), $"anzwl_{System.Guid.NewGuid():N}.db");
    private readonly AnzoneDb _db;
    private readonly WhitelistRepository _repo;

    public WhitelistRepositoryTests()
    {
        _db = new AnzoneDb($"Data Source={_file}");
        _db.EnsureCreated();
        _repo = new WhitelistRepository(_db);
    }

    public void Dispose() { _db.Dispose(); if (File.Exists(_file)) File.Delete(_file); }

    [Fact] public void AddThenContainsNormalized()
    {
        _repo.Add(@"C:\Program Files\Foo\Foo.EXE", "Foo");
        Assert.True(_repo.Contains(@"c:\program files\foo\foo.exe"));
        Assert.False(_repo.Contains(@"c:\program files\bar\bar.exe"));
    }

    [Fact] public void RemoveRevokes()
    {
        _repo.Add(@"C:\a\a.exe", "A");
        _repo.Remove(@"C:\A\A.EXE");
        Assert.False(_repo.Contains(@"c:\a\a.exe"));
    }

    [Fact] public void GetAllAndNormalizedSet()
    {
        _repo.Add(@"C:\a\a.exe", "A");
        _repo.Add(@"C:\b\b.exe", "B");
        Assert.Equal(2, _repo.GetAll().Count);
        Assert.Contains(@"c:\a\a.exe", _repo.GetNormalizedSet());
    }
}
```

- [ ] **Step 2: 运行确认失败**。
- [ ] **Step 3: 实现**

`AnzoneCore/Data/AnzoneDb.cs`:
```csharp
using Microsoft.Data.Sqlite;

namespace AnzoneCore.Data;

public class AnzoneDb : System.IDisposable
{
    private readonly SqliteConnection _conn;
    public AnzoneDb(string connectionString)
    {
        _conn = new SqliteConnection(connectionString);
        _conn.Open();
    }

    public SqliteConnection Connection => _conn;

    public void EnsureCreated()
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS whitelist (
                image_path TEXT PRIMARY KEY,
                display_name TEXT NOT NULL,
                added_at_utc INTEGER NOT NULL);
            CREATE TABLE IF NOT EXISTS logs (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                timestamp_utc INTEGER NOT NULL,
                type TEXT NOT NULL,
                image_path TEXT,
                detail TEXT NOT NULL);";
        cmd.ExecuteNonQuery();
    }

    public void Dispose() => _conn.Dispose();
}
```

`AnzoneCore/Data/WhitelistRepository.cs`:
```csharp
using System.Collections.Generic;
using AnzoneCore.Model;
using AnzoneCore.Util;

namespace AnzoneCore.Data;

public class WhitelistRepository
{
    private readonly AnzoneDb _db;
    public WhitelistRepository(AnzoneDb db) => _db = db;

    public void Add(string imagePath, string displayName)
    {
        var norm = PathNormalizer.Normalize(imagePath);
        using var cmd = _db.Connection.CreateCommand();
        cmd.CommandText = @"INSERT OR REPLACE INTO whitelist(image_path, display_name, added_at_utc)
                            VALUES ($p, $n, $t)";
        cmd.Parameters.AddWithValue("$p", norm);
        cmd.Parameters.AddWithValue("$n", displayName);
        cmd.Parameters.AddWithValue("$t", System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        cmd.ExecuteNonQuery();
    }

    public void Remove(string imagePath)
    {
        using var cmd = _db.Connection.CreateCommand();
        cmd.CommandText = "DELETE FROM whitelist WHERE image_path = $p";
        cmd.Parameters.AddWithValue("$p", PathNormalizer.Normalize(imagePath));
        cmd.ExecuteNonQuery();
    }

    public bool Contains(string normalizedPath)
    {
        using var cmd = _db.Connection.CreateCommand();
        cmd.CommandText = "SELECT EXISTS(SELECT 1 FROM whitelist WHERE image_path = $p)";
        cmd.Parameters.AddWithValue("$p", PathNormalizer.Normalize(normalizedPath));
        return System.Convert.ToInt64(cmd.ExecuteScalar()) == 1;
    }

    public List<WhitelistEntry> GetAll()
    {
        var list = new List<WhitelistEntry>();
        using var cmd = _db.Connection.CreateCommand();
        cmd.CommandText = "SELECT image_path, display_name, added_at_utc FROM whitelist ORDER BY display_name";
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new WhitelistEntry(r.GetString(0), r.GetString(1), r.GetInt64(2)));
        return list;
    }

    public HashSet<string> GetNormalizedSet()
    {
        var set = new HashSet<string>();
        foreach (var e in GetAll()) set.Add(e.ImagePath);
        return set;
    }
}
```

- [ ] **Step 4: 运行确认通过**（3 passed）。
- [ ] **Step 5: Commit**
```
git add anzone-windows/AnzoneCore/Data/AnzoneDb.cs anzone-windows/AnzoneCore/Data/WhitelistRepository.cs anzone-windows/AnzoneCore.Tests/WhitelistRepositoryTests.cs
git commit -m "feat: add SQLite db and whitelist repository"
```

---

## Task 6: LogRepository

**Files:** Create `AnzoneCore/Data/LogRepository.cs`; Test `AnzoneCore.Tests/LogRepositoryTests.cs`.

- [ ] **Step 1: 写失败测试**
```csharp
using System.IO;
using AnzoneCore.Data;
using AnzoneCore.Model;
using Xunit;

public class LogRepositoryTests : System.IDisposable
{
    private readonly string _file = Path.Combine(Path.GetTempPath(), $"anzlog_{System.Guid.NewGuid():N}.db");
    private readonly AnzoneDb _db;
    private readonly LogRepository _repo;

    public LogRepositoryTests()
    {
        _db = new AnzoneDb($"Data Source={_file}");
        _db.EnsureCreated();
        _repo = new LogRepository(_db);
    }

    public void Dispose() { _db.Dispose(); if (File.Exists(_file)) File.Delete(_file); }

    [Fact] public void RecordPersists()
    {
        _repo.Record(LogType.ProcessBlocked, @"C:\x\x.exe", "blocked x");
        var all = _repo.GetRecent(10);
        Assert.Single(all);
        Assert.Equal(LogType.ProcessBlocked, all[0].Type);
    }

    [Fact] public void RecentOrderedDesc()
    {
        _repo.Record(LogType.ServiceStart, null, "start");
        _repo.Record(LogType.AdminLogin, null, "login");
        var all = _repo.GetRecent(10);
        Assert.Equal(LogType.AdminLogin, all[0].Type);
    }

    [Fact] public void ClearEmpties()
    {
        _repo.Record(LogType.ServiceStart, null, "s");
        _repo.Clear();
        Assert.Empty(_repo.GetRecent(10));
    }
}
```

- [ ] **Step 2: 运行确认失败**。
- [ ] **Step 3: 实现** `AnzoneCore/Data/LogRepository.cs`:
```csharp
using System.Collections.Generic;
using AnzoneCore.Model;

namespace AnzoneCore.Data;

public class LogRepository
{
    private readonly AnzoneDb _db;
    public LogRepository(AnzoneDb db) => _db = db;

    public void Record(LogType type, string? imagePath, string detail)
    {
        using var cmd = _db.Connection.CreateCommand();
        cmd.CommandText = @"INSERT INTO logs(timestamp_utc, type, image_path, detail)
                            VALUES ($t, $ty, $p, $d)";
        cmd.Parameters.AddWithValue("$t", System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        cmd.Parameters.AddWithValue("$ty", type.ToString());
        cmd.Parameters.AddWithValue("$p", (object?)imagePath ?? System.DBNull.Value);
        cmd.Parameters.AddWithValue("$d", detail);
        cmd.ExecuteNonQuery();
    }

    public List<LogEntry> GetRecent(int limit)
    {
        var list = new List<LogEntry>();
        using var cmd = _db.Connection.CreateCommand();
        cmd.CommandText = @"SELECT id, timestamp_utc, type, image_path, detail
                            FROM logs ORDER BY id DESC LIMIT $lim";
        cmd.Parameters.AddWithValue("$lim", limit);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new LogEntry(
                r.GetInt64(0), r.GetInt64(1),
                System.Enum.Parse<LogType>(r.GetString(2)),
                r.IsDBNull(3) ? null : r.GetString(3), r.GetString(4)));
        return list;
    }

    public void Clear()
    {
        using var cmd = _db.Connection.CreateCommand();
        cmd.CommandText = "DELETE FROM logs";
        cmd.ExecuteNonQuery();
    }
}
```

- [ ] **Step 4: 运行确认通过**（3 passed）。
- [ ] **Step 5: Commit**
```
git add anzone-windows/AnzoneCore/Data/LogRepository.cs anzone-windows/AnzoneCore.Tests/LogRepositoryTests.cs
git commit -m "feat: add log repository"
```

---

## Task 7: ConfigStore + AuthService

**Files:** Create `AnzoneCore/Auth/ConfigStore.cs`, `AnzoneCore/Auth/AuthService.cs`; Test `AnzoneCore.Tests/AuthServiceTests.cs`.

ConfigStore 读写 config.json（密码 hash/salt 用 Base64）。AuthService 用 PasswordHasher 校验。

- [ ] **Step 1: 写失败测试**
```csharp
using System.IO;
using AnzoneCore.Auth;
using Xunit;

public class AuthServiceTests : System.IDisposable
{
    private readonly string _file = Path.Combine(Path.GetTempPath(), $"anzcfg_{System.Guid.NewGuid():N}.json");
    private readonly AuthService _auth;

    public AuthServiceTests() => _auth = new AuthService(new ConfigStore(_file));
    public void Dispose() { if (File.Exists(_file)) File.Delete(_file); }

    [Fact] public void FirstRunTrueInitially() => Assert.True(_auth.IsFirstRun());

    [Fact] public void SetPasswordThenVerify()
    {
        _auth.SetAdminPassword("pw1");
        Assert.False(_auth.IsFirstRun());
        Assert.True(_auth.Verify("pw1"));
        Assert.False(_auth.Verify("nope"));
    }

    [Fact] public void ChangePassword()
    {
        _auth.SetAdminPassword("old");
        _auth.SetAdminPassword("new");
        Assert.False(_auth.Verify("old"));
        Assert.True(_auth.Verify("new"));
    }

    [Fact] public void PausedFlagPersists()
    {
        var store = new ConfigStore(_file);
        store.Paused = true;
        Assert.True(new ConfigStore(_file).Paused);
    }
}
```

- [ ] **Step 2: 运行确认失败**。
- [ ] **Step 3: 实现**

`AnzoneCore/Auth/ConfigStore.cs`:
```csharp
using System.IO;
using System.Text.Json;

namespace AnzoneCore.Auth;

public class ConfigStore
{
    private readonly string _path;
    private Data _data;

    private class Data
    {
        public string? AdminHash { get; set; }
        public string? AdminSalt { get; set; }
        public bool Paused { get; set; } = true;     // safe default: paused on fresh install
        public bool FirstRun { get; set; } = true;
    }

    public ConfigStore(string path)
    {
        _path = path;
        _data = File.Exists(_path)
            ? JsonSerializer.Deserialize<Data>(File.ReadAllText(_path)) ?? new Data()
            : new Data();
    }

    private void Save() => File.WriteAllText(_path, JsonSerializer.Serialize(_data));

    public string? AdminHash { get => _data.AdminHash; set { _data.AdminHash = value; Save(); } }
    public string? AdminSalt { get => _data.AdminSalt; set { _data.AdminSalt = value; Save(); } }
    public bool Paused { get => _data.Paused; set { _data.Paused = value; Save(); } }
    public bool FirstRun { get => _data.FirstRun; set { _data.FirstRun = value; Save(); } }
}
```

`AnzoneCore/Auth/AuthService.cs`:
```csharp
using System;
using AnzoneCore.Security;

namespace AnzoneCore.Auth;

public class AuthService
{
    private readonly ConfigStore _config;
    public AuthService(ConfigStore config) => _config = config;

    public bool IsFirstRun() => _config.FirstRun;

    public void SetAdminPassword(string password)
    {
        var salt = PasswordHasher.GenerateSalt();
        var hash = PasswordHasher.Hash(password, salt);
        _config.AdminSalt = Convert.ToBase64String(salt);
        _config.AdminHash = Convert.ToBase64String(hash);
        _config.FirstRun = false;
    }

    public bool Verify(string password)
    {
        if (_config.AdminHash is null || _config.AdminSalt is null) return false;
        var salt = Convert.FromBase64String(_config.AdminSalt);
        var hash = Convert.FromBase64String(_config.AdminHash);
        return PasswordHasher.Verify(password, salt, hash);
    }
}
```

- [ ] **Step 4: 运行确认通过**（4 passed）。
- [ ] **Step 5: Commit**
```
git add anzone-windows/AnzoneCore/Auth anzone-windows/AnzoneCore.Tests/AuthServiceTests.cs
git commit -m "feat: add config store and auth service"
```

---

## Task 8: IPC 消息契约 + 序列化

**Files:** Create `AnzoneCore/Ipc/Messages.cs`; Test `AnzoneCore.Tests/IpcMessagesTests.cs`.

请求/响应用一个带 `Op` 字段的信封 + JSON。供 Service（server）与 Tray/未来 Spec C（client）共用。

- [ ] **Step 1: 写失败测试**
```csharp
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
        var back = IpcCodec.DeserializeResponse(IpcCodec.Serialize(IpcResponse.Error("bad")));
        Assert.False(back.Success);
        Assert.Equal("bad", back.Error);
    }
}
```

- [ ] **Step 2: 运行确认失败**。
- [ ] **Step 3: 实现** `AnzoneCore/Ipc/Messages.cs`:
```csharp
using System.Collections.Generic;
using System.Text.Json;

namespace AnzoneCore.Ipc;

public record IpcRequest(string Op, string? Token, Dictionary<string, string> Args);

public record IpcResponse(bool Success, string? Payload, string? Error)
{
    public static IpcResponse Ok(string? payload = null) => new(true, payload, null);
    public static IpcResponse Error(string error) => new(false, null, error);
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
```

- [ ] **Step 4: 运行确认通过**（3 passed）。
- [ ] **Step 5: Commit**
```
git add anzone-windows/AnzoneCore/Ipc/Messages.cs anzone-windows/AnzoneCore.Tests/IpcMessagesTests.cs
git commit -m "feat: add IPC message contracts"
```

---

## Task 9: CommandHandler（IPC 命令分发逻辑，可单测）

**Files:** Create `AnzoneCore/Ipc/CommandHandler.cs`, `AnzoneCore/Ipc/SessionTokens.cs`; Test `AnzoneCore.Tests/CommandHandlerTests.cs`.

把"收到 IpcRequest → 业务动作 → IpcResponse"的纯逻辑放进 Core（不含管道 IO），便于单测。管道 IO 在 Task 10 的 Service 里。

- [ ] **Step 1: 写失败测试**
```csharp
using System.Collections.Generic;
using System.IO;
using AnzoneCore.Auth;
using AnzoneCore.Data;
using AnzoneCore.Ipc;
using Xunit;

public class CommandHandlerTests : System.IDisposable
{
    private readonly string _db = Path.Combine(Path.GetTempPath(), $"h_{System.Guid.NewGuid():N}.db");
    private readonly string _cfg = Path.Combine(Path.GetTempPath(), $"h_{System.Guid.NewGuid():N}.json");
    private readonly CommandHandler _h;
    private readonly AnzoneDb _conn;

    public CommandHandlerTests()
    {
        _conn = new AnzoneDb($"Data Source={_db}");
        _conn.EnsureCreated();
        var auth = new AuthService(new ConfigStore(_cfg));
        auth.SetAdminPassword("pw");
        _h = new CommandHandler(new WhitelistRepository(_conn), new LogRepository(_conn),
                                auth, new ConfigStore(_cfg), new SessionTokens());
    }
    public void Dispose() { _conn.Dispose(); File.Delete(_db); if (File.Exists(_cfg)) File.Delete(_cfg); }

    private static IpcRequest Req(string op, string? tok = null, Dictionary<string,string>? args = null)
        => new(op, tok, args ?? new());

    [Fact] public void LoginWrongPasswordFails()
    {
        var r = _h.Handle(Req("Login", null, new(){{"password","bad"}}));
        Assert.False(r.Success);
    }

    [Fact] public void LoginThenAddWhitelistWithToken()
    {
        var login = _h.Handle(Req("Login", null, new(){{"password","pw"}}));
        Assert.True(login.Success);
        var token = login.Payload!;
        var add = _h.Handle(Req("AddWhitelist", token, new(){{"path",@"C:\a\a.exe"},{"name","A"}}));
        Assert.True(add.Success);
    }

    [Fact] public void MutatingWithoutTokenRejected()
    {
        var r = _h.Handle(Req("AddWhitelist", "badtoken", new(){{"path",@"C:\a\a.exe"},{"name","A"}}));
        Assert.False(r.Success);
    }

    [Fact] public void StatusNeedsNoToken()
    {
        var r = _h.Handle(Req("GetStatus"));
        Assert.True(r.Success);
    }
}
```

- [ ] **Step 2: 运行确认失败**。
- [ ] **Step 3: 实现**

`AnzoneCore/Ipc/SessionTokens.cs`:
```csharp
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
```

`AnzoneCore/Ipc/CommandHandler.cs`:
```csharp
using System.Text.Json;
using AnzoneCore.Auth;
using AnzoneCore.Data;
using AnzoneCore.Model;

namespace AnzoneCore.Ipc;

public class CommandHandler
{
    private readonly WhitelistRepository _whitelist;
    private readonly LogRepository _logs;
    private readonly AuthService _auth;
    private readonly ConfigStore _config;
    private readonly SessionTokens _tokens;

    public CommandHandler(WhitelistRepository whitelist, LogRepository logs,
        AuthService auth, ConfigStore config, SessionTokens tokens)
    {
        _whitelist = whitelist; _logs = logs; _auth = auth; _config = config; _tokens = tokens;
    }

    public IpcResponse Handle(IpcRequest req)
    {
        switch (req.Op)
        {
            case "Login":
                if (_auth.Verify(req.Args.GetValueOrDefault("password", "")))
                { _logs.Record(LogType.AdminLogin, null, "admin login"); return IpcResponse.Ok(_tokens.Issue()); }
                _logs.Record(LogType.LoginFailed, null, "login failed");
                return IpcResponse.Error("invalid credentials");

            case "GetStatus":
                return IpcResponse.Ok(JsonSerializer.Serialize(new {
                    paused = _config.Paused, whitelistCount = _whitelist.GetAll().Count }));

            case "ListWhitelist":
                return IpcResponse.Ok(JsonSerializer.Serialize(_whitelist.GetAll()));

            case "GetLogs":
                if (!Auth(req)) return IpcResponse.Error("unauthorized");
                return IpcResponse.Ok(JsonSerializer.Serialize(_logs.GetRecent(200)));
        }

        // mutating ops below require a valid token
        if (!Auth(req)) return IpcResponse.Error("unauthorized");
        switch (req.Op)
        {
            case "AddWhitelist":
                _whitelist.Add(req.Args["path"], req.Args.GetValueOrDefault("name", ""));
                _logs.Record(LogType.WhitelistChange, req.Args["path"], "added");
                return IpcResponse.Ok();
            case "RemoveWhitelist":
                _whitelist.Remove(req.Args["path"]);
                _logs.Record(LogType.WhitelistChange, req.Args["path"], "removed");
                return IpcResponse.Ok();
            case "ClearLogs":
                _logs.Clear(); return IpcResponse.Ok();
            case "PauseEnforcement":
                _config.Paused = true; _logs.Record(LogType.EnforcementPaused, null, "paused"); return IpcResponse.Ok();
            case "ResumeEnforcement":
                _config.Paused = false; _logs.Record(LogType.EnforcementResumed, null, "resumed"); return IpcResponse.Ok();
            case "ChangeAdminPassword":
                _auth.SetAdminPassword(req.Args["newPassword"]); return IpcResponse.Ok();
            default:
                return IpcResponse.Error($"unknown op {req.Op}");
        }
    }

    private bool Auth(IpcRequest req) => _tokens.IsValid(req.Token);
}
```

- [ ] **Step 4: 运行确认通过**（4 passed）。
- [ ] **Step 5: Commit**
```
git add anzone-windows/AnzoneCore/Ipc/CommandHandler.cs anzone-windows/AnzoneCore/Ipc/SessionTokens.cs anzone-windows/AnzoneCore.Tests/CommandHandlerTests.cs
git commit -m "feat: add IPC command handler with token-gated mutations"
```

---

## Task 10: AnzoneService —— WMI 监听 + 拦截 + 管道 server

**Files:** Create `AnzoneService/ProcessWatcher.cs`, `AnzoneService/PipeServer.cs`, `AnzoneService/EnforcementWorker.cs`; Modify `AnzoneService/Program.cs`. 验证：编译（WMI/管道无法 JVM 单测，逻辑已在 Core 测过）。

- [ ] **Step 1: ProcessWatcher（WMI）** `AnzoneService/ProcessWatcher.cs`:
```csharp
using System;
using System.Management;

namespace AnzoneService;

public class ProcessWatcher : IDisposable
{
    private ManagementEventWatcher? _watcher;
    public event Action<int, string>? ProcessStarted;  // pid, imagePath

    public void Start()
    {
        _watcher = new ManagementEventWatcher(
            new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace"));
        _watcher.EventArrived += (_, e) =>
        {
            try
            {
                var pid = Convert.ToInt32(e.NewEvent.Properties["ProcessID"].Value);
                var path = ResolvePath(pid);
                if (path != null) ProcessStarted?.Invoke(pid, path);
            }
            catch { /* per-event failure must not kill the watcher */ }
        };
        _watcher.Start();
    }

    private static string? ResolvePath(int pid)
    {
        try { return System.Diagnostics.Process.GetProcessById(pid).MainModule?.FileName; }
        catch { return null; }
    }

    public void Dispose() { _watcher?.Stop(); _watcher?.Dispose(); }
}
```

- [ ] **Step 2: PipeServer** `AnzoneService/PipeServer.cs`:
```csharp
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
                catch (Exception ex) { resp = IpcResponse.Error(ex.Message); }
                await writer.WriteLineAsync(IpcCodec.Serialize(resp));
            }
            catch (OperationCanceledException) { break; }
            catch { /* drop bad client, keep serving */ }
        }
    }
}
```

- [ ] **Step 3: EnforcementWorker** `AnzoneService/EnforcementWorker.cs`:
```csharp
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AnzoneCore.Auth;
using AnzoneCore.Data;
using AnzoneCore.Enforcement;
using AnzoneCore.Ipc;
using AnzoneCore.Model;
using Microsoft.Extensions.Hosting;

namespace AnzoneService;

public class EnforcementWorker : BackgroundService
{
    private readonly string _dataDir = @"C:\ProgramData\anzone";
    private AnzoneDb _db = null!;
    private WhitelistRepository _whitelist = null!;
    private LogRepository _logs = null!;
    private ConfigStore _config = null!;
    private PipeServer _pipe = null!;
    private readonly ProcessWatcher _watcher = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        System.IO.Directory.CreateDirectory(_dataDir);
        _db = new AnzoneDb($"Data Source={System.IO.Path.Combine(_dataDir, "anzone.db")}");
        _db.EnsureCreated();
        _whitelist = new WhitelistRepository(_db);
        _logs = new LogRepository(_db);
        _config = new ConfigStore(System.IO.Path.Combine(_dataDir, "config.json"));
        var auth = new AuthService(_config);
        var handler = new CommandHandler(_whitelist, _logs, auth, _config, new SessionTokens());
        _pipe = new PipeServer(handler);

        _logs.Record(LogType.ServiceStart, null, "service start");

        _watcher.ProcessStarted += OnProcessStarted;
        _watcher.Start();

        try { await _pipe.RunAsync(stoppingToken); }
        finally
        {
            _watcher.Dispose();
            _logs.Record(LogType.ServiceStop, null, "service stop");
        }
    }

    private void OnProcessStarted(int pid, string path)
    {
        var decider = new EnforcementDecider(_whitelist.GetNormalizedSet(), _config.Paused);
        var action = decider.Decide(new ProcessInfo(pid, path));
        if (action == EnforcementAction.Allow) return;
        try
        {
            Process.GetProcessById(pid).Kill();
            _logs.Record(action == EnforcementAction.BlockInstaller ? LogType.InstallBlocked : LogType.ProcessBlocked,
                path, $"killed pid {pid}");
        }
        catch (Exception ex) { _logs.Record(LogType.ProcessBlocked, path, $"kill failed: {ex.Message}"); }
    }
}
```

- [ ] **Step 4: Program.cs** `AnzoneService/Program.cs`:
```csharp
using AnzoneService;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(o => o.ServiceName = "AnzoneService");
builder.Services.AddHostedService<EnforcementWorker>();
builder.Build().Run();
```

- [ ] **Step 5: 编译验证**
```
& 'C:\Program Files\dotnet\dotnet.exe' build C:\myproject\anzone-windows\AnzoneService
```
Expected: succeeded（可能有 System.Management 仅 Windows 的平台警告，可忽略）。

- [ ] **Step 6: Commit**
```
git add anzone-windows/AnzoneService
git commit -m "feat: add WMI watcher, pipe server, and enforcement worker"
```

---

## Task 11: AnzoneTray —— WinForms 托盘 + 管道 client

**Files:** Create `AnzoneTray/PipeClient.cs`, `AnzoneTray/Program.cs`, `AnzoneTray/LoginForm.cs`, `AnzoneTray/WhitelistForm.cs`, `AnzoneTray/LogsForm.cs`. 验证：编译。

- [ ] **Step 1: PipeClient** `AnzoneTray/PipeClient.cs`:
```csharp
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
            return line is null ? IpcResponse.Error("no response") : IpcCodec.DeserializeResponse(line);
        }
        catch (System.Exception ex) { return IpcResponse.Error($"not connected: {ex.Message}"); }
    }
}
```

- [ ] **Step 2: LoginForm** `AnzoneTray/LoginForm.cs`:
```csharp
using System.Collections.Generic;
using System.Windows.Forms;
using AnzoneCore.Ipc;

namespace AnzoneTray;

public class LoginForm : Form
{
    private readonly TextBox _pw = new() { UseSystemPasswordChar = true, Dock = DockStyle.Top };
    public string? Token { get; private set; }

    public LoginForm(PipeClient client)
    {
        Text = "anzone 管理员登录"; Width = 320; Height = 140;
        var btn = new Button { Text = "登录", Dock = DockStyle.Bottom };
        btn.Click += (_, _) =>
        {
            var r = client.Send(new IpcRequest("Login", null, new Dictionary<string,string>{{"password", _pw.Text}}));
            if (r.Success) { Token = r.Payload; DialogResult = DialogResult.OK; Close(); }
            else MessageBox.Show("密码错误或服务未运行");
        };
        Controls.Add(_pw); Controls.Add(btn);
    }
}
```

- [ ] **Step 3: WhitelistForm + LogsForm** `AnzoneTray/WhitelistForm.cs`:
```csharp
using System.Collections.Generic;
using System.Text.Json;
using System.Windows.Forms;
using AnzoneCore.Ipc;
using AnzoneCore.Model;

namespace AnzoneTray;

public class WhitelistForm : Form
{
    private readonly PipeClient _client; private readonly string _token;
    private readonly ListBox _list = new() { Dock = DockStyle.Fill };

    public WhitelistForm(PipeClient client, string token)
    {
        _client = client; _token = token;
        Text = "白名单管理"; Width = 520; Height = 420;
        var add = new Button { Text = "添加 exe", Dock = DockStyle.Bottom };
        var del = new Button { Text = "删除选中", Dock = DockStyle.Bottom };
        add.Click += (_, _) => AddExe();
        del.Click += (_, _) => RemoveSelected();
        Controls.Add(_list); Controls.Add(del); Controls.Add(add);
        Refresh2();
    }

    private void Refresh2()
    {
        _list.Items.Clear();
        var r = _client.Send(new IpcRequest("ListWhitelist", null, new()));
        if (r.Success && r.Payload != null)
            foreach (var e in JsonSerializer.Deserialize<List<WhitelistEntry>>(r.Payload)!)
                _list.Items.Add($"{e.DisplayName}  |  {e.ImagePath}");
    }

    private void AddExe()
    {
        using var dlg = new OpenFileDialog { Filter = "可执行文件|*.exe" };
        if (dlg.ShowDialog() != DialogResult.OK) return;
        _client.Send(new IpcRequest("AddWhitelist", _token,
            new Dictionary<string,string>{{"path", dlg.FileName},{"name", System.IO.Path.GetFileName(dlg.FileName)}}));
        Refresh2();
    }

    private void RemoveSelected()
    {
        if (_list.SelectedItem is not string s) return;
        var path = s.Split("|")[1].Trim();
        _client.Send(new IpcRequest("RemoveWhitelist", _token, new Dictionary<string,string>{{"path", path}}));
        Refresh2();
    }
}
```

`AnzoneTray/LogsForm.cs`:
```csharp
using System.Collections.Generic;
using System.Text.Json;
using System.Windows.Forms;
using AnzoneCore.Ipc;
using AnzoneCore.Model;

namespace AnzoneTray;

public class LogsForm : Form
{
    public LogsForm(PipeClient client, string token)
    {
        Text = "操作日志"; Width = 640; Height = 480;
        var box = new ListBox { Dock = DockStyle.Fill, HorizontalScrollbar = true };
        Controls.Add(box);
        var r = client.Send(new IpcRequest("GetLogs", token, new()));
        if (r.Success && r.Payload != null)
            foreach (var e in JsonSerializer.Deserialize<List<LogEntry>>(r.Payload)!)
                box.Items.Add($"{e.Type}  {e.Detail}  {e.ImagePath}");
    }
}
```

- [ ] **Step 4: Program.cs（托盘）** `AnzoneTray/Program.cs`:
```csharp
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using AnzoneCore.Ipc;

namespace AnzoneTray;

static class Program
{
    private static readonly PipeClient Client = new();
    private static string? _token;

    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        var menu = new ContextMenuStrip();
        var icon = new NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Shield,
            Visible = true, Text = "anzone 管控",
            ContextMenuStrip = menu
        };

        menu.Items.Add("管理员登录", null, (_, _) =>
        {
            using var f = new LoginForm(Client);
            if (f.ShowDialog() == DialogResult.OK) _token = f.Token;
        });
        menu.Items.Add("白名单管理", null, (_, _) => { if (RequireLogin()) new WhitelistForm(Client, _token!).Show(); });
        menu.Items.Add("操作日志", null, (_, _) => { if (RequireLogin()) new LogsForm(Client, _token!).Show(); });
        menu.Items.Add("暂停管控", null, (_, _) => { if (RequireLogin()) Client.Send(new IpcRequest("PauseEnforcement", _token, new())); });
        menu.Items.Add("恢复管控", null, (_, _) => { if (RequireLogin()) Client.Send(new IpcRequest("ResumeEnforcement", _token, new())); });
        menu.Items.Add("退出托盘", null, (_, _) => { icon.Visible = false; Application.Exit(); });

        Application.Run();
    }

    private static bool RequireLogin()
    {
        if (_token != null) return true;
        using var f = new LoginForm(Client);
        if (f.ShowDialog() == DialogResult.OK) { _token = f.Token; return true; }
        return false;
    }
}
```

- [ ] **Step 5: 编译验证**
```
& 'C:\Program Files\dotnet\dotnet.exe' build C:\myproject\anzone-windows\AnzoneTray
```
Expected: succeeded。

- [ ] **Step 6: Commit**
```
git add anzone-windows/AnzoneTray
git commit -m "feat: add WinForms tray admin app"
```

---

## Task 12: 发布 + 服务安装脚本 + 部署文档

**Files:** Create `anzone-windows/scripts/install-service.ps1`, `uninstall-service.ps1`, `anzone-windows/docs/DEPLOYMENT.md`. 验证：发布产物存在。

- [ ] **Step 1: install-service.ps1**
```powershell
# 需管理员 PowerShell 运行。参数：已发布的 AnzoneService.exe 路径
param([string]$ServiceExe = "C:\Program Files\anzone\AnzoneService.exe")
sc.exe create AnzoneService binPath= "`"$ServiceExe`"" start= auto
sc.exe failure AnzoneService reset= 86400 actions= restart/5000/restart/5000/restart/5000
sc.exe start AnzoneService
Write-Host "AnzoneService 已安装并启动（开机自启 + 崩溃自动重启）。"
```

- [ ] **Step 2: uninstall-service.ps1**
```powershell
sc.exe stop AnzoneService
sc.exe delete AnzoneService
Write-Host "AnzoneService 已卸载。"
```

- [ ] **Step 3: DEPLOYMENT.md** `anzone-windows/docs/DEPLOYMENT.md`:
```markdown
# anzone-windows 部署指南

## 前提
- 目标电脑日常用户须为**标准账号（非本地管理员）**，否则自保护失效。
- 安装需一次性管理员权限。

## 发布
在开发机：
    dotnet publish AnzoneService -c Release -r win-x64 --self-contained -o publish\service
    dotnet publish AnzoneTray   -c Release -r win-x64 --self-contained -o publish\tray
将 publish\service\* 拷到目标机 `C:\Program Files\anzone\`，publish\tray\* 同目录。

## 安装（管理员 PowerShell）
1. `scripts\install-service.ps1`（创建并启动服务，开机自启 + SCM 恢复）
2. 让 AnzoneTray.exe 随用户登录启动（放入启动文件夹或注册表 Run）。
3. 首次：托盘"管理员登录"→设置密码→添加白名单→"恢复管控"（首次默认暂停）。

## 解除
托盘登录 → 暂停管控（装软件）；或 `uninstall-service.ps1` 彻底卸载。

## 已知限制
- 反应式拦截，进程会闪现一下才被杀。
- 用户为本地管理员时无法防其停服务/卸载。
- 安装器拦截为启发式（msiexec/setup*/install*），非穷尽。
```

- [ ] **Step 4: 验证发布产物**
```
& 'C:\Program Files\dotnet\dotnet.exe' publish C:\myproject\anzone-windows\AnzoneService -c Release -r win-x64 --self-contained -o C:\myproject\anzone-windows\publish\service
```
Expected: 生成 `publish\service\AnzoneService.exe`（确认文件存在）。publish 目录被 .gitignore 忽略（属 build 产物——确认 `anzone-windows/**/bin/`、根 `**/build/` 不覆盖它；如未忽略，在 .gitignore 加 `anzone-windows/publish/`）。

- [ ] **Step 5: Commit**
```
git add anzone-windows/scripts anzone-windows/docs/DEPLOYMENT.md
git commit -m "docs: add service install scripts and deployment guide"
```

---

## Task 13: 手动验收清单（真机 / 管理员权限）

无法自动化，需人工在 Windows 上走查并记录：

- [ ] 安装服务（install-service.ps1）→ `sc.exe query AnzoneService` 显示 RUNNING
- [ ] 托盘登录设密码 → 添加记事本以外的某 app 到白名单 → 恢复管控
- [ ] 双击白名单外的 portable exe（如某下载的 .exe）→ 进程闪现后被杀，日志出现 PROCESS_BLOCKED
- [ ] 双击白名单内的 app → 正常运行
- [ ] 运行某安装包（setup.exe / .msi）→ 被拦，日志 INSTALL_BLOCKED
- [ ] 暂停管控 → 同一安装包能正常运行 → 恢复管控
- [ ] 以**标准用户**在任务管理器尝试结束 AnzoneService → Access Denied
- [ ] 验证不误杀系统：开机进桌面正常，资源管理器/浏览器（若加白）/系统托盘正常
- [ ] kill 服务进程 → SCM 在 ~5s 内自动重启
- [ ] 重启电脑 → 服务自启，管控恢复

---

## 自查记录（写计划时执行）

**Spec 覆盖：**
- 拦截机制 WMI（spec §1/§6A）→ Task 4(decider) + 10(watcher/worker) ✅
- 防误杀系统基线（spec §6A）→ Task 4 EnforcementDecider 关键用例 ✅
- 防装应用（spec §6C）→ Task 4 installer 规则 + 10 ✅
- 暂停/恢复管控（spec §6D）→ Task 9 handler + 11 托盘 ✅
- 白名单管理（spec §6B）→ Task 5 + 9 + 11 ✅
- 日志（spec §4）→ Task 6 + 全程 Record ✅
- IPC 契约（spec §5）→ Task 8 + 9 + 10(server) + 11(client) ✅
- 鉴权 PBKDF2 + token（spec §3/§5）→ Task 1 + 7 + 9 ✅
- 自保护（spec §6E）→ Task 12 sc.exe failure 恢复 + LocalSystem；验收 Task 13 ✅
- 首次安全默认暂停（spec §6F/§7）→ Task 7 ConfigStore 默认 Paused=true/FirstRun=true ✅
- 测试（spec §8）→ 单测 Task 1-9；手动 Task 13 ✅

**占位符扫描：** 无 TBD/TODO。

**类型一致性：** `EnforcementAction`(Allow/BlockProcess/BlockInstaller)、`LogType`、`IpcRequest(Op,Token,Args)`/`IpcResponse(Success,Payload,Error)`、`CommandHandler.Handle`、`WhitelistRepository`(Add/Remove/Contains/GetAll/GetNormalizedSet)、`AuthService`(IsFirstRun/SetAdminPassword/Verify)、`ConfigStore`(Paused/FirstRun) 跨 task 一致。

**已知执行注意：** Task 10/11 是 Windows 专属，编译验证即可（运行时行为靠 Task 13 手动验收）；`System.Management` 在 net8.0-windows 下可用。
