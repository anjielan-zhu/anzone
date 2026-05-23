# anzone-android 企业管控客户端 MVP — 设计文档

- **日期**：2026-05-24
- **状态**：已通过 brainstorming，待写实施计划
- **范围**：so.md 全套需求中的 **Android 端子项目（Spec B）**

---

## 0. 背景与定位

母需求 `anzone/so.md` 是一份覆盖 Windows + Android + iOS 三端的企业终端软件白名单管控产品说明书（PRD）。该 PRD 体量等同商业 MDM 产品（对标 Microsoft Intune / Hexnode / JAMF），不可能一次性交付，已拆分为三个独立子项目：

| Spec | 内容 | 状态 |
|---|---|---|
| Spec A | Windows 拦截内核（native 服务） | 未启动 |
| **Spec B** | **Android 管控客户端（本文档）** | **设计中** |
| Spec C | Flutter 跨平台管理员 UI（复用 anzone scaffold） | 未启动 |

本文档只覆盖 Spec B。Spec A / C 各自独立走 spec → plan → 实现。

**`anzone/` 当前的 Flutter scaffold 不属于本子项目**——Spec B 是另起的纯原生 Kotlin 工程。

---

## 1. 确认的需求边界

| 维度 | 决策 |
|---|---|
| 部署场景 | 企业发放的公司自有 Android 手机，IT 预装 |
| 部署档位 | **T3 — Device Owner 模式**（出厂重置 + ADB 一次性激活） |
| 拦截强度目标 | 防住"会上网查教程的人"（中等） |
| 桌面模型 | **Kiosk** —— 本 App 作默认 Launcher，只显示白名单 App |
| 机型范围 | 不限品牌；MVP 只测主流原生 Android 行为，OEM 特定保活适配延后 |
| 技术栈 | 纯原生 Kotlin + Jetpack Compose + Room + DataStore + DevicePolicyManager |
| 架构 | 方案 B：双 Activity（KioskActivity + AdminActivity），权限物理隔离 |
| 网络 | 无。全本地，无远程管理、无多设备同步 |

### MVP 必含功能
1. 白名单管理 + Kiosk 桌面
2. 防卸载 / 防退出管控
3. 防装应用（系统级拦截 APK 安装）
4. 操作日志（本地 SQLite）

### 明确排除（列入未来迭代）
iOS 端、Windows 端、网络/远程管理、多设备统一管理、自我修复看门狗、OEM 保活专项适配、白名单时效规则、登录失败锁定、日志加密、批量导入导出、自定义拦截提示语。

---

## 2. 角色状态机（核心交互模型）

App 内有**两个账号**：**1 个管理员** + **1 个普通用户**，各自独立用户名+密码（均 PBKDF2 哈希存储）。普通用户是默认运行态。

```
首次安装打开
    ↓
[首次创建向导] ← 依次创建 管理员账号 + 普通用户账号（无默认弱口令）
    ↓
┌──────────────┐   切换普通用户(免密,记住会话)  ┌────────────┐
│  管理员模式    │ ─────────────────────────▶│ 普通用户模式 │
│ AdminActivity │                            │  = Kiosk    │
│ - 加/删白名单  │ ◀─────────────────────────│ 只能用白名单 │
│ - 看/导出日志  │   输入管理员用户名+密码      └─────┬──────┘
│ - 改双方密码   │                                  │
│ - 解除管控     │                            设备重启│
└──────────────┘                                  ▼
       ▲              自动恢复普通用户会话 (Kiosk，免密)
       └──── "退出/解除管控" 需管理员密码 ◀───────────
```

规则：
1. 首次打开 → 创建向导依次设**管理员**与**普通用户**两套用户名+密码（PBKDF2 哈希，无出厂默认口令）
2. 管理员配好白名单 → 点"切换普通用户" → 进入 Kiosk（**免密**，会话记住普通用户身份）
3. 普通用户只能用白名单 App
4. **设备重启 → 自动恢复上次的普通用户会话，直接进 Kiosk（免密）**，绝不自动进管理员
5. 从普通用户切回管理员 / 退出管控 → 必须输入**管理员**用户名+密码
6. 普通用户密码的用途：管理员可在设置里查看/重置；预留未来"普通用户主动注销后重新登录"场景。MVP 下重启走免密自动恢复，不强制普通用户输密码

---

## 3. 架构总览

```
┌─────────────────────────────────────────────────────┐
│            Android 设备 (Device Owner 模式)            │
│                                                       │
│  ┌──────────────────┐      ┌──────────────────────┐  │
│  │  KioskActivity    │      │   AdminActivity       │  │
│  │ (CATEGORY_HOME)   │ 管理员 │ (无 launcher 图标)    │  │
│  │ - 白名单 App 网格  │─入口─▶│ - 首次创建向导        │  │
│  │ - "管理员"入口按钮 │      │ - 登录校验            │  │
│  │   → 要求登录       │ 切回  │ - 白名单管理          │  │
│  └────────┬─────────┘ 普通  │ - 日志查看/导出       │  │
│           │           用户  │ - 改密码/解除管控      │  │
│           │           ◀────│                       │  │
│           └──────────┬──────┴──────────┬───────────┘  │
│                      ▼                  ▼              │
│          ┌────────────────────────────────┐           │
│          │   Repository 层                 │           │
│          │ WhitelistRepo / LogRepo /       │           │
│          │ AuthRepo / SessionState         │           │
│          └───────┬────────────────┬───────┘           │
│                  ▼                ▼                    │
│        ┌──────────┐      ┌─────────────┐              │
│        │  Room DB  │      │ DataStore   │              │
│        │(白名单/日志)│      │(账号hash/会话)│              │
│        └──────────┘      └─────────────┘              │
│                                                        │
│  ┌──────────────────────────────────────────────┐    │
│  │  DevicePolicyManager (系统级管控)               │    │
│  │  - setLockTaskPackages / startLockTask         │    │
│  │  - addUserRestriction(DISALLOW_INSTALL_APPS …) │    │
│  │  - clearDeviceOwnerApp (解除管控)               │    │
│  │  AnzoneDeviceAdminReceiver (BroadcastReceiver)  │    │
│  └──────────────────────────────────────────────┘    │
└────────────────────────────────────────────────────────┘
```

**单进程、无网络、无常驻 Service。** Launcher Activity 在 Device Owner 下天然受系统保护、长期存活。

---

## 4. 组件职责

| 组件 | 职责 | 依赖 |
|---|---|---|
| `KioskActivity` | 默认 Launcher。渲染白名单网格；点击启动授权 App；提供"管理员"入口按钮→要求登录；拦截 Home/Back/Recent（LockTask） | Repository, DPM |
| `AdminActivity` | 首次创建向导（建管理员+普通用户两账号）+ 登录校验后进入。Tab：白名单管理 / 日志中心 / 设置（改两套密码、解除管控）/ 设备信息。无 launcher 图标 | Repository, DPM |
| `AnzoneDeviceAdminReceiver` | `DeviceAdminReceiver` 子类。`onEnabled`/`onDisabled` 回调；监听包安装/卸载广播写日志 | LogRepository |
| `WhitelistRepository` | 增删查白名单；提供"已安装 App 列表"供管理员勾选 | Room, PackageManager |
| `LogRepository` | 写入/查询/导出/清空日志 | Room |
| `AuthRepository` | 管理**两套账号**（管理员/普通用户）：创建、校验用户名+密码、改密码 | DataStore |
| `SessionState` | 当前角色（ADMIN / NORMAL）内存态 + 切换逻辑；持久化"上次普通用户会话"供重启免密恢复 | DataStore |

每个 Repository 单一职责、接口清晰、可独立单测。

---

## 5. 数据模型

### Room 实体

```kotlin
@Entity(tableName = "whitelist")
data class WhitelistApp(
    @PrimaryKey val packageName: String,  // 如 "com.tencent.mm"
    val appLabel: String,                  // 显示名，如 "微信"
    val addedAt: Long
)

enum class LogType { APP_LAUNCH, BLOCK_INSTALL, ADMIN_LOGIN, LOGIN_FAILED,
                     WHITELIST_CHANGE, MANAGEMENT_EXIT, ROLE_SWITCH, BOOT }

@Entity(tableName = "logs")
data class LogEntry(
    @PrimaryKey(autoGenerate = true) val id: Long = 0,
    val timestamp: Long,
    val type: LogType,
    val packageName: String?,   // 相关 App，可空
    val detail: String          // 人类可读描述
)
```

### DataStore（Preferences）
- `admin_username`
- `admin_password_hash` / `admin_password_salt` — PBKDF2WithHmacSHA256，10 万次迭代，随机 16 字节 salt
- `normal_username`
- `normal_password_hash` / `normal_password_salt` — 同上
- `is_first_run` — 控制是否进入创建向导
- `current_role` — ADMIN / NORMAL（重启复位为 NORMAL，免密恢复普通用户会话）

> 两套密码均不明文存储。MVP 用 JDK 自带 `javax.crypto`，不引入第三方加密库。

---

## 6. 关键流程

### A. 部署 / Provisioning（IT 一次性）
```
设备出厂重置 → 跳过 Google 账号
→ adb shell dpm set-device-owner com.anzone.mdm/.AnzoneDeviceAdminReceiver
→ App 成为 Device Owner
→ 首次启动进入"创建账号"向导
→ 设为默认 Launcher → 重启 → 进入普通用户 Kiosk
```
交付物含 ADB 激活脚本 + 图文文档。

### B. 首次创建账号（管理员 + 普通用户）
```
首次打开检测 is_first_run=true → 强制进入创建向导
→ 第1步 设管理员用户名+密码（+确认）
→ 第2步 设普通用户用户名+密码（+确认）
→ 两套各自 PBKDF2 哈希存入 DataStore
→ is_first_run=false → 进入管理员模式
```

### C. 普通用户启动 App
```
KioskActivity 显示白名单网格 → 点「微信」
→ 查 WhitelistRepository 确认在白名单 → startActivity
→ 写 APP_LAUNCH 日志
（白名单外 App 不显示图标，无入口，天然拦截）
```

### D. 管理员登录 / 切回
```
Kiosk "管理员"入口按钮 → 输入管理员用户名+密码
→ AuthRepository 校验管理员账号 PBKDF2 → 通过 → AdminActivity，写 ADMIN_LOGIN
→ 失败 → 提示，写 LOGIN_FAILED
```

### E. 切换普通用户
```
AdminActivity → "切换普通用户" → current_role=NORMAL（记住普通用户会话）
→ 启动 KioskActivity + startLockTask() → 写 ROLE_SWITCH
（免密，直接进）
```

### F. 添加白名单
```
AdminActivity → 白名单管理 → "添加" → PackageManager 列已安装 App
→ 勾选 → 存 Room → 写 WHITELIST_CHANGE → 返回 Kiosk 自动刷新网格
```

### G. 防装应用
```
进入 Kiosk 时 dpm.addUserRestriction(DISALLOW_INSTALL_APPS)
+ DISALLOW_INSTALL_UNKNOWN_SOURCES
→ 系统层禁止任何 APK 安装
→ Receiver 监听安装尝试 → 写 BLOCK_INSTALL 日志
```

### H. 解除管控（唯一出口，仅管理员）
```
AdminActivity → 设置 → "解除管控" → 二次密码确认 → 警告弹窗
→ stopLockTask() + 移除 user restrictions + 恢复系统 Launcher
+ dpm.clearDeviceOwnerApp()
→ 写 MANAGEMENT_EXIT → 设备恢复正常，可装软件
（重新管控需再次 provisioning）
```

### I. 重启恢复
```
重启 → App 是 Device Owner + 默认 Launcher → 自动进 KioskActivity
→ current_role 复位为 NORMAL，免密恢复普通用户会话
→ 重新施加 LockTask + user restrictions → 写 BOOT 日志
```

---

## 7. 安全 / 防绕过机制（基于 Device Owner）

| 威胁 | 对策 | 局限（诚实说明） |
|---|---|---|
| 卸载本 App | Device Owner 应用系统禁止卸载 | 需保持 Device Owner 状态 |
| 强杀进程/清后台 | 作为 Launcher，被杀后系统立即重启回桌面 | OEM 激进清理可能短暂黑屏，下一期适配 |
| 改系统时间绕过 | MVP 白名单无时效，改时间无影响 | — |
| 装 APK 绕过 | `DISALLOW_INSTALL_APPS` 系统级拦截 | — |
| 进安全模式卸载 | `DISALLOW_SAFE_BOOT` | — |
| 恢复出厂设置 | `DISALLOW_FACTORY_RESET` | fastboot 强刷无法防，超出软件层 |
| 管理员密码泄露 | PBKDF2 哈希存储，失败写日志 | MVP 不做失败锁定，列入 v2 |
| ADB 绕过 | `DISALLOW_DEBUGGING_FEATURES`（provisioning 后施加） | provisioning 阶段 IT 需 ADB，装完再锁 |

> MVP **不含** so.md 提的"核心文件被篡改自动恢复"自我修复机制——需看门狗进程互拉，复杂度高，列入 v2。当前防护匹配"防住会查教程的人"。

---

## 8. 错误处理

只在系统边界做防御（Device Owner 状态、PackageManager、DB IO），内部调用信任。

- **非 Device Owner 启动**：检测到自己不是 Device Owner → 显示引导页说明需 provisioning，不崩溃、不进 Kiosk
- **白名单 App 已卸载**：点击时 PackageManager 找不到 → 提示"应用已不在设备上" + 自动从白名单移除 + 写日志
- **Room 读写失败**：Repository 层捕获，降级处理，不让 Kiosk 桌面崩溃
- **首次启动无账号**：强制进创建向导，不可跳过

---

## 9. 测试策略（TDD）

- **单元测试**（JUnit + Robolectric）：三个 Repository 增删查改、PBKDF2 哈希校验、白名单匹配、SessionState 切换
- **Instrumented 测试**（androidTest）：Room DAO、DevicePolicyManager 调用（需 Device Owner 测试环境）
- **手动验收**（无法自动化）：Kiosk Launcher 行为、Home/Back 拦截、provisioning 全流程、解除管控流程——真机人工走查。交付时明确标注"已自动测 / 需人工验证"

每个 Repository 先写测试再写实现。

---

## 10. 未决 / 风险

- **OEM 保活差异**：MVP 只保证主流原生 Android 行为；小米/华为/OPPO/vivo 的激进清理可能导致 Kiosk 短暂退出，专项适配列入 v2。
- **Device Owner 激活门槛**：依赖 IT 出厂重置 + ADB，需配套清晰文档，否则部署受阻。
- **HarmonyOS Next（5.0+）**：不再兼容 Android App，本方案在该系统上不可用，需另立项。
- **Spec C 衔接**：未来电脑端 Flutter 管理员 UI 若要远程管理本客户端，需补网络层与鉴权，当前架构预留但不实现。
