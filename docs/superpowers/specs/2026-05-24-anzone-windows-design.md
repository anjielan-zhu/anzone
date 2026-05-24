# anzone-windows 企业管控客户端 MVP — 设计文档

- **日期**：2026-05-24
- **状态**：已通过 brainstorming，待写实施计划
- **范围**：so.md 全套需求中的 **Windows 端子项目（Spec A）**

---

## 0. 背景与定位

母需求 `anzone/so.md` 是覆盖 Windows + Android + iOS 三端的企业终端软件白名单管控产品（对标 Intune / Hexnode / JAMF），已拆为三个独立子项目：

| Spec | 内容 | 状态 |
|---|---|---|
| **Spec A** | **Windows 拦截引擎 + 托盘管理（本文档）** | **设计中** |
| Spec B | Android Device Owner Kiosk 客户端 | 已完成（`C:\myproject\anzone-mdm`） |
| Spec C | Flutter 跨平台/统一管理 UI | 未启动 |

本文档只覆盖 Spec A。

> 注：Spec A 自带 WinForms 托盘管理 UI，这缩小了 Spec C 的职责——Spec C 的定位（统一/远程多设备控制台）待 Spec A 完成后重新审视。

---

## 1. 确认的需求边界

| 维度 | 决策 |
|---|---|
| 部署场景 | 企业自有办公电脑，IT 管理 |
| Windows 版本 | **含家庭版（Home）** → 无组策略/AppLocker/WDAC，只能用自写 user-mode 服务 |
| 拦截强度 | 防住"会上网查教程的人"（中等，user-mode，不写内核驱动） |
| 拦截机制 | **WMI 进程启动事件 + 终止**（反应式） |
| 技术栈 | **C# / .NET**（Worker Service + WinForms 托盘 + 共享类库），self-contained 发布 |
| 管控模型 | **始终管控 + 单管理员密码暂停/管理**（无普通用户账号；用电脑即受管） |
| 控制方式 | WinForms 托盘管理程序，经命名管道 IPC 控制服务 |
| 部署规模 | 单机起步，服务器列入后续 |

### MVP 必含功能
1. 白名单管理 + 进程拦截
2. 防装应用（拦截安装包运行）
3. 服务自保护（防结束 / 防卸载）
4. 操作日志（本地）

### 核心前提（写入假设）
**日常用户必须是标准账号（非本地管理员）。** 所有 user-mode 自保护都建立于此。若用户是本地 Administrator，没有任何 user-mode 手段能阻止其停止/卸载服务——那需要内核驱动 + 代码签名，列入 v2。

### 明确排除（列入 v2）
哈希匹配、内核级/防本地管理员、拦截 C:\Windows 内 living-off-the-land 程序、网络/远程管理（属 Spec C）、多设备、文件篡改自愈（超出 SCM 恢复）、代码签名、穷尽式安装器识别、iOS/Android 端。

---

## 2. 架构总览

```
┌───────────────────────────────────────────────────────────────┐
│                        Windows 电脑                             │
│                                                                 │
│  会话层（用户登录后）            系统层（开机即起, LocalSystem）   │
│  ┌──────────────────────┐       ┌──────────────────────────┐   │
│  │  AnzoneTray (WinForms) │      │  AnzoneService (Worker)   │   │
│  │  托盘管理程序           │      │  拦截引擎（无 UI）          │   │
│  │  - 管理员登录           │◀────▶│  - WMI 进程启动监听         │   │
│  │  - 白名单增删           │ 命名 │  - 白名单匹配→Kill          │   │
│  │  - 日志查看             │ 管道 │  - 安装器拦截(msiexec/setup)│   │
│  │  - 暂停/恢复/退出管控    │ IPC  │  - 自保护(SCM恢复)          │   │
│  └──────────────────────┘       └────────────┬─────────────┘   │
│                                               ▼                 │
│                              ┌────────────────────────────┐    │
│                              │  AnzoneCore (类库, 共享)     │    │
│                              │  白名单/日志/鉴权/IPC 契约/   │    │
│                              │  拦截决策逻辑                 │    │
│                              └────────────┬───────────────┘    │
│                                           ▼                     │
│                              ┌────────────────────────────┐    │
│                              │  C:\ProgramData\anzone\     │    │
│                              │  SQLite: 白名单 + 日志       │    │
│                              │  config.json: 管理员密码哈希 │    │
│                              └────────────────────────────┘    │
└───────────────────────────────────────────────────────────────┘
```

三个工程：`AnzoneService`（Worker Service，LocalSystem）+ `AnzoneTray`（WinForms 托盘，用户会话，标准权限）+ `AnzoneCore`（共享类库）。数据存 `C:\ProgramData\anzone\`（ACL：服务可写、普通用户只读）。命名管道 IPC 是 Tray↔Service 唯一通道，**其协议契约即 Spec C 以后复用的接口**。

---

## 3. 组件职责

| 组件 | 进程/权限 | 职责 | 可单测 |
|---|---|---|---|
| `AnzoneService` | 服务, LocalSystem | WMI 监听进程创建；调用 Core 决策；非白名单 `Process.Kill()`；安装器拦截；写日志；命名管道 server；SCM 自保护 | 部分（决策逻辑在 Core） |
| `AnzoneTray` | 用户会话, 标准权限 | 托盘图标；管理员登录；白名单增删；日志查看；暂停/恢复/退出管控；命名管道 client | 否（手动验收） |
| `AnzoneCore` | 类库 | `WhitelistRepository`、`LogRepository`、`AuthService`(PBKDF2)、SQLite 访问、IPC 消息契约、**`EnforcementDecider`（是否拦截的纯逻辑）** | ✅ |

`AnzoneCore` 不依赖服务运行，可独立 xUnit 测试。

---

## 4. 数据模型（SQLite，`C:\ProgramData\anzone\anzone.db`）

```
WhitelistEntry:  ImagePath(规范化全路径, 主键) | DisplayName | AddedAtUtc
LogEntry:        Id(自增) | TimestampUtc | Type | ImagePath? | Detail
```

**LogType 枚举**：PROCESS_BLOCKED / INSTALL_BLOCKED / ADMIN_LOGIN / LOGIN_FAILED / WHITELIST_CHANGE / ENFORCEMENT_PAUSED / ENFORCEMENT_RESUMED / SERVICE_START / SERVICE_STOP

**config.json**（服务可写、普通用户只读 ACL）：`admin_password_hash` / `admin_password_salt`（PBKDF2WithHmacSHA256，10 万次迭代，复用 Spec B 同算法）/ `enforcement_paused`(bool) / `first_run`(bool)。

白名单按**规范化全路径**匹配（不区分大小写）。复制 exe 到别处=不同路径=被拦。哈希匹配列 v2。

---

## 5. IPC 契约（命名管道 `\\.\pipe\anzone-control`，JSON 消息）

**Spec C 的 Flutter UI 以后复用此契约。**

```
Login(password) -> {token} | error          # token 内存态、会话级、会过期
GetStatus() -> {paused, whitelistCount, version}
ListWhitelist() -> [entries]
ListInstalledApps() -> [{path, displayName}] # 服务枚举开始菜单 .lnk + 注册表卸载项
AddWhitelist(token, path, name) / RemoveWhitelist(token, path)
GetLogs(token, limit) -> [logs] / ClearLogs(token)
PauseEnforcement(token) / ResumeEnforcement(token)
ChangeAdminPassword(token, newPassword)
```

**所有改动类命令都要有效 token**（先用管理员密码 Login 换取）。标准用户能连管道但无密码改不了任何东西。管道 ACL：允许交互用户连接；鉴权靠 token。

---

## 6. 关键流程

### A. 开机管控
服务 LocalSystem + 自动启动 → 载入白名单到内存 → 启 WMI `Win32_ProcessStartTrace` 监听 → 每个新进程交给 `EnforcementDecider`：若 未暂停 且 路径不在白名单 且 不在系统基线允许 且（命中安装器规则 或 非系统目录）→ `Process.Kill()` + 写 PROCESS_BLOCKED/INSTALL_BLOCKED。

> ⚠️ **防误杀系统（关键安全设计）**：绝不能拦 Windows 自身进程，否则系统瘫痪。基线策略：
> - **`C:\Windows\` 目录树下的进程默认放行**（OS 二进制）
> - 硬编码关键进程安全表（winlogon/csrss/services/svchost/lsass/explorer/dwm + 我们自己的服务和托盘）
> - 管控只针对 `C:\Windows` 之外的程序（Program Files、用户目录、可移动盘、Temp）
> 这是 `EnforcementDecider` 的首要规则，必须有专门单测覆盖。

### B. 加白名单
托盘管理员登录 → `ListInstalledApps`（服务枚举开始菜单 .lnk 目标 + 注册表卸载项）→ 勾选 → `AddWhitelist` → 服务更新 DB + 内存集合 → 写 WHITELIST_CHANGE。也支持手动输入/选择 exe 路径。

### C. 防装应用
安装器 `msiexec.exe` 在 System32（会被基线放行），故**安装拦截是独立规则层**：非暂停态下，拦截 `msiexec.exe` 及匹配 `setup*.exe` / `*install*.exe` 的进程启动 → 写 INSTALL_BLOCKED。诚实说明：启发式无法覆盖所有安装器，中等强度。

### D. 暂停 / 恢复管控（= so.md "解除管控后才能装软件"）
托盘管理员验证 → `PauseEnforcement` → 服务停止 kill（仍记日志）+ 置 `enforcement_paused=true` → 可正常装软件 → `ResumeEnforcement` 恢复。

### E. 自保护
服务 LocalSystem + 启动类型 Automatic + SCM 失败恢复（崩溃自动重启）。标准用户停不了/卸不了（Access Denied）。托盘崩溃不影响拦截（拦截在服务里）。前提：用户为标准账号。

### F. 首次安装安全默认
全新装机白名单为空，若立刻管控会拦掉一切非 Windows 程序。故**首次启动 `enforcement_paused=true` + `first_run=true`**；管理员在托盘设密码 + 配白名单后手动恢复管控。避免装完锁死。

---

## 7. 错误处理

- **WMI 监听器挂掉** → 服务检测并重建 watcher + 写日志（拦截不能静默失效）
- **Kill 失败**（受保护/提权进程）→ 记日志，不崩溃
- **DB IO 错误** → 拦截以内存白名单集合为准；DB 错误只降级日志，不影响拦截
- **管道断开** → 托盘显示"未连接"并自动重连
- **首次无管理员密码** → 托盘强制进入"设置管理员密码"，服务保持暂停态直至配置完成

只在边界（WMI、进程操作、DB、管道）做防御；内部调用信任。

---

## 8. 测试策略

- **单元测试（xUnit，本机 .NET 可跑）**：`AnzoneCore`
  - `EnforcementDecider`：系统基线放行、白名单命中/未命中、安装器规则、暂停态——这是最关键的纯逻辑，必须充分覆盖（含"不误杀 C:\Windows 进程"用例）
  - `WhitelistRepository` / `LogRepository`（SQLite 内存库或临时文件）
  - `AuthService`（PBKDF2 哈希/校验）
  - 路径规范化
- **手动验收（需真机 Windows）**：WMI 实际拦截效果、Kill、命名管道、服务生命周期、SCM 恢复、托盘 UI、防误杀系统验证、标准用户停服务被拒验证。清单随交付。

遵循 TDD：决策逻辑与仓库先写测试再实现。

---

## 9. 不在 MVP 范围（v2）

哈希匹配、内核级/防本地管理员、拦截 C:\Windows 内 living-off-the-land 程序、网络/远程管理（Spec C）、多设备、文件篡改自愈、代码签名（EV 证书）、穷尽式安装器识别、iOS/Android。

---

## 10. 未决 / 风险

- **反应式拦截窗口**：进程被杀前会短暂运行（user-mode 物理限制）。匹配"防住会查教程的人"档位，但快速程序可能完成部分动作。阻止式拦截需内核驱动（v2）。
- **本地管理员用户**：若部署环境用户是本地管理员，自保护全部失效——部署前必须确认用标准账号。
- **WMI 事件延迟**：极少数高频进程风暴下可能有处理积压；MVP 不做限流，观察后定。
- **Spec C 衔接**：命名管道 IPC 契约需保持稳定，供 Spec C 复用；未来加远程管理需在其上叠加网络层 + 鉴权。
