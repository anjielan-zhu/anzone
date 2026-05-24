namespace AnzoneCore.Model;

public enum LogType
{
    ProcessBlocked, InstallBlocked, AdminLogin, LoginFailed,
    WhitelistChange, EnforcementPaused, EnforcementResumed, ServiceStart, ServiceStop
}

public record LogEntry(long Id, long TimestampUtc, LogType Type, string? ImagePath, string Detail);
