# anzone 变更设计：简繁本地化 + Android 登录/锁定 + Kiosk 外观

- **日期**：2026-05-24
- **状态**：已通过 brainstorming，待实施
- **范围**：对已构建的 Spec A（`anzone-windows`）和 Spec B（`anzone-mdm`）的增量变更
- **参考**：`2026-05-24-anzone-android-mdm-design.md`、`2026-05-24-anzone-windows-design.md`

## 变更清单
1. 简繁中文本地化（Android + Windows，应内语言切换）
2. Android 普通用户登录 / 锁定（单用户，保留自动恢复 + 加手动锁定解锁）
3. Android 防结束进程（强化现有 Device Owner + LockTask）
4. Android Kiosk 桌面背景不变（透出系统壁纸）
5. Android Kiosk 程序图标不变（显示真实 App 图标）

---

## 1. 简繁本地化

### Android (`anzone-mdm`)
- 抽取所有硬编码 UI 字符串到字符串资源：
  - `app/src/main/res/values/strings.xml`（默认，简体）
  - `app/src/main/res/values-zh-rTW/strings.xml`（繁体）
  - （可选 `values-en/` 英文）
- Compose 中用 `stringResource(R.string.xxx)` 取代字面量（SetupWizard / Login / Whitelist / Logs / Settings / Kiosk）。
- 设置页新增"语言"选项：跟随系统 / 简体 / 繁体。选择存 DataStore（key `app_language`）。
- 切换实现：`AppCompatDelegate.setApplicationLocales(LocaleListCompat.forLanguageTags("zh-Hans"/"zh-Hant"))`，或自管 locale + `Activity.recreate()`。默认空 = 跟随系统。

### Windows (`anzone-windows`)
- 新增本地化资源：`AnzoneTray/Strings.resx`（默认简体）、`Strings.zh-Hant.resx`（繁体）；通过生成的 `Strings` 访问器取字符串。
- 托盘所有可见文本改为 `Strings.XxX`。
- 托盘"设置/语言"子菜单：跟随系统 / 简体 / 繁体；选择存 `config.json`（key `language`，值 `system`/`zh-Hans`/`zh-Hant`）。
- 启动时按偏好设 `CultureInfo.CurrentUICulture`；切换后提示重启托盘生效（简单可靠，避免运行时重建所有窗体）。
- `AnzoneCore`/`AnzoneService` 无 UI，不本地化（日志 detail 保持中性英文标识符 + 时间）。

---

## 2. Android 普通用户登录 / 锁定（单用户）

数据模型不变（`AuthRepository` 已有普通用户 `normal_username`/`normal_password_hash`）。

- **默认行为不变**：开机/冷启动 → 自动恢复进 Kiosk（免密），沿用现状。
- **新增锁定**：`KioskActivity` 顶栏加"锁定"按钮 → 进入**锁定态**：隐藏白名单网格，显示普通用户登录框（用户名 + 密码）。
- **解锁**：输入校验 `AuthRepository.verifyNormal(user, pw)` → 成功回到 Kiosk 网格；失败提示并写 `LogType.LOGIN_FAILED`。成功写一条 ROLE_SWITCH/或新增 `USER_UNLOCK`（复用 ROLE_SWITCH，detail 注明"普通用户解锁"）。
- 锁定态同样吞 Back / 不暴露 Home（LockTask 已保证）。
- 用途：员工临时离开锁屏，回来输普通用户密码解锁。

实现：在 `KioskActivity` 增加 `locked: Boolean` Compose 状态；锁定时渲染一个复用的登录可组合（可直接复用现有 `LoginScreen`，title 传"普通用户解锁"，onSubmit 走 verifyNormal）。

---

## 3. Android 防结束进程（强化）

现状已基本满足（Device Owner 应用禁卸载 + LockTask 锁任务 + 默认 Launcher + `DISALLOW_*` 限制）。补充：
- 进 Kiosk 时除 `startLockTask()` 外，调用 `dpm.setLockTaskFeatures(admin, flags)` 限定锁定态功能（如仅允许 `LOCK_TASK_FEATURE_HOME`，禁通知/系统信息/全局动作下拉）。
- 确认锁定态下状态栏下拉、最近任务、电源菜单不可用（LockTask 行为 + features 标志）。
- 不引入看门狗进程（Launcher 进程受系统保护，YAGNI；列入 v2 若真出现被杀场景）。

---

## 4. Android Kiosk：真实壁纸 + 真实图标

### 桌面背景不变（透出系统壁纸）
- `KioskActivity` 使用显示壁纸的主题：AndroidManifest 给该 Activity 设 `android:theme` 指向一个带 `<item name="android:windowShowWallpaper">true</item>` 且窗口背景透明的主题；Compose 根 `Surface` 用透明色（`Color.Transparent`），不铺纯色。
- 效果：Kiosk 网格浮在设备当前壁纸之上，背景与原桌面一致。

### 程序图标不变（真实 App 图标）
- 白名单网格每项加载真实图标：`packageManager.getApplicationIcon(packageName)`（`Drawable`）→ 转 `ImageBitmap`（`drawable.toBitmap().asImageBitmap()`）→ Compose `Image` 显示。
- 取代当前"取 appLabel 前两字"的文字占位。
- 图标加载放在 `remember`/`derivedStateOf` 缓存，避免每次重组重新解码（性能）。
- App 已卸载取不到图标时回退到默认占位（不崩溃，沿用现有"已卸载自动移除"逻辑）。

---

## 测试与验证
- **本地化**：Android 可加 instrumented/单元测试验证语言偏好持久化（DataStore key）；字符串切换主要靠手动验收（切到繁体看界面）。Windows 资源切换手动验收。
- **锁定/解锁**：`AuthRepository.verifyNormal` 已有单测覆盖；锁定 UI 交互手动验收。
- **壁纸/图标**：纯 UI/系统行为，真机手动验收（无法自动化）。
- 既有 22（Android）/ 33（Windows）单测必须保持通过。

## 不在本次范围
- 多普通用户（已确认维持单用户）。
- 运行时无重启切换 Windows 语言（用重启托盘代替）。
- 看门狗式防杀（Device Owner 已够"防住会查教程的人"档位）。
- 其他语言（仅简体 + 繁体）。
