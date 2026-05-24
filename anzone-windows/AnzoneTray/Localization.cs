using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace AnzoneTray;

public static class L
{
    private static readonly string PrefFile = Path.Combine(
        System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
        "anzone", "tray.lang");

    public static string Lang { get; private set; } = "system";

    public static void Init()
    {
        try { if (File.Exists(PrefFile)) Lang = File.ReadAllText(PrefFile).Trim(); } catch { }
    }

    public static void SetLang(string lang)
    {
        Lang = lang;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PrefFile)!);
            File.WriteAllText(PrefFile, lang);
        }
        catch { }
    }

    private static bool UseTraditional()
    {
        if (Lang == "zh-Hant") return true;
        if (Lang == "zh-Hans") return false;
        var c = CultureInfo.CurrentUICulture.Name;
        return c.StartsWith("zh-Hant") || c == "zh-TW" || c == "zh-HK" || c == "zh-MO";
    }

    public static string T(string key)
    {
        var d = UseTraditional() ? Hant : Hans;
        return d.TryGetValue(key, out var v) ? v : key;
    }

    private static readonly Dictionary<string, string> Hans = new()
    {
        ["tray_tip"] = "anzone 管控",
        ["menu_setup"] = "初始设置",
        ["menu_login"] = "管理员登录",
        ["menu_whitelist"] = "白名单",
        ["menu_logs"] = "日志",
        ["menu_pause"] = "暂停管控",
        ["menu_resume"] = "恢复管控",
        ["menu_language"] = "语言",
        ["menu_exit"] = "退出托盘",
        ["lang_system"] = "跟随系统",
        ["lang_hans"] = "简体中文",
        ["lang_hant"] = "繁體中文",
        ["login_title"] = "管理员登录",
        ["login_btn"] = "登录",
        ["login_fail"] = "密码错误或服务未运行",
        ["setup_title"] = "初始设置",
        ["setup_btn"] = "设置管理员密码",
        ["setup_hint"] = "首次设置：请设置管理员密码",
        ["pw_empty"] = "密码不能为空",
        ["setup_fail"] = "设置失败",
        ["whitelist_title"] = "白名单",
        ["add_exe"] = "添加 exe",
        ["remove_sel"] = "删除选中",
        ["file_filter"] = "可执行文件|*.exe",
        ["logs_title"] = "日志",
    };

    private static readonly Dictionary<string, string> Hant = new()
    {
        ["tray_tip"] = "anzone 管控",
        ["menu_setup"] = "初始設定",
        ["menu_login"] = "管理員登入",
        ["menu_whitelist"] = "白名單",
        ["menu_logs"] = "日誌",
        ["menu_pause"] = "暫停管控",
        ["menu_resume"] = "恢復管控",
        ["menu_language"] = "語言",
        ["menu_exit"] = "退出托盤",
        ["lang_system"] = "跟隨系統",
        ["lang_hans"] = "简体中文",
        ["lang_hant"] = "繁體中文",
        ["login_title"] = "管理員登入",
        ["login_btn"] = "登入",
        ["login_fail"] = "密碼錯誤或服務未執行",
        ["setup_title"] = "初始設定",
        ["setup_btn"] = "設定管理員密碼",
        ["setup_hint"] = "首次設定：請設定管理員密碼",
        ["pw_empty"] = "密碼不能為空",
        ["setup_fail"] = "設定失敗",
        ["whitelist_title"] = "白名單",
        ["add_exe"] = "新增 exe",
        ["remove_sel"] = "刪除選取",
        ["file_filter"] = "可執行檔|*.exe",
        ["logs_title"] = "日誌",
    };
}
