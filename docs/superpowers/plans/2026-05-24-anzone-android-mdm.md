# anzone-android 企业管控客户端 MVP 实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 构建一个 Device Owner 模式的 Android 企业管控客户端：管理员配置白名单，普通用户只能在 Kiosk 桌面运行白名单内 App，系统级防卸载/防装，本地日志。

**Architecture:** 纯原生 Kotlin + Jetpack Compose，方案 B 双 Activity（KioskActivity 作 Launcher / AdminActivity 隐藏管理）。数据层 Room（白名单+日志）+ DataStore（两套账号+会话）。无网络、无常驻 Service。手动构造注入（ServiceLocator），不引入 Hilt。

**Tech Stack:** Kotlin 2.0、Jetpack Compose（Material 3）、Room 2.6、DataStore Preferences 1.1、DevicePolicyManager、JUnit4 + Robolectric + kotlinx-coroutines-test。minSdk 28 / compileSdk 35。

参考设计文档：`docs/superpowers/specs/2026-05-24-anzone-android-mdm-design.md`

---

## 文件结构

新建独立 Gradle 工程 `C:\myproject\anzone-mdm`（与 `anzone/` Flutter scaffold 无关）。包名 `com.anzone.mdm`。

```
anzone-mdm/
├── settings.gradle.kts
├── build.gradle.kts                       (root)
├── gradle/libs.versions.toml              (version catalog)
└── app/
    ├── build.gradle.kts
    └── src/
        ├── main/
        │   ├── AndroidManifest.xml
        │   ├── res/xml/device_admin.xml    (DeviceAdminReceiver 策略声明)
        │   └── java/com/anzone/mdm/
        │       ├── AnzoneApp.kt            (Application + ServiceLocator)
        │       ├── security/
        │       │   └── PasswordHasher.kt   (PBKDF2)
        │       ├── data/
        │       │   ├── db/
        │       │   │   ├── AnzoneDatabase.kt
        │       │   │   ├── WhitelistApp.kt
        │       │   │   ├── WhitelistDao.kt
        │       │   │   ├── LogEntry.kt
        │       │   │   ├── LogType.kt
        │       │   │   └── LogDao.kt
        │       │   ├── WhitelistRepository.kt
        │       │   ├── LogRepository.kt
        │       │   ├── AuthRepository.kt
        │       │   └── SessionState.kt
        │       ├── device/
        │       │   ├── PolicyManager.kt    (DevicePolicyManager 封装)
        │       │   └── AnzoneDeviceAdminReceiver.kt
        │       └── ui/
        │           ├── kiosk/KioskActivity.kt
        │           ├── admin/AdminActivity.kt
        │           ├── admin/SetupWizardScreen.kt
        │           ├── admin/LoginScreen.kt
        │           ├── admin/WhitelistScreen.kt
        │           ├── admin/LogsScreen.kt
        │           └── admin/SettingsScreen.kt
        ├── test/java/com/anzone/mdm/        (JUnit + Robolectric 单元测试)
        └── androidTest/java/com/anzone/mdm/ (instrumented：Room DAO)
└── docs/PROVISIONING.md                    (IT 部署文档)
└── scripts/provision.ps1 / provision.sh    (ADB 激活脚本)
```

每个文件单一职责，Repository 可独立单测。

---

## 测试约定

- 纯逻辑（PasswordHasher、SessionState、Repository）：`src/test`，JUnit4 + Robolectric（`@RunWith(RobolectricTestRunner::class)`），协程用 `runTest`。
- Room DAO：`src/test` 用 Robolectric + 内存数据库（`Room.inMemoryDatabaseBuilder`）。
- DevicePolicyManager / Activity / Launcher 行为：**无法自动化**，列入手动验收清单（Task 16）。
- 每个 task 末尾 commit。提交信息用 conventional commits（`feat:` / `test:` / `chore:`）。

---

## Task 0: Gradle 工程脚手架

**Files:**
- Create: `anzone-mdm/settings.gradle.kts`
- Create: `anzone-mdm/build.gradle.kts`
- Create: `anzone-mdm/gradle/libs.versions.toml`
- Create: `anzone-mdm/app/build.gradle.kts`
- Create: `anzone-mdm/app/src/main/AndroidManifest.xml`（最小占位）

- [ ] **Step 1: 创建 version catalog**

`anzone-mdm/gradle/libs.versions.toml`:
```toml
[versions]
agp = "8.5.2"
kotlin = "2.0.20"
coreKtx = "1.13.1"
composeBom = "2024.09.02"
activityCompose = "1.9.2"
room = "2.6.1"
datastore = "1.1.1"
lifecycle = "2.8.6"
coroutines = "1.9.0"
junit = "4.13.2"
robolectric = "4.13"
androidxTestExt = "1.2.1"
androidxTestCore = "1.6.1"

[libraries]
androidx-core-ktx = { group = "androidx.core", name = "core-ktx", version.ref = "coreKtx" }
androidx-activity-compose = { group = "androidx.activity", name = "activity-compose", version.ref = "activityCompose" }
androidx-lifecycle-runtime-ktx = { group = "androidx.lifecycle", name = "lifecycle-runtime-ktx", version.ref = "lifecycle" }
androidx-lifecycle-viewmodel-compose = { group = "androidx.lifecycle", name = "lifecycle-viewmodel-compose", version.ref = "lifecycle" }
compose-bom = { group = "androidx.compose", name = "compose-bom", version.ref = "composeBom" }
compose-ui = { group = "androidx.compose.ui", name = "ui" }
compose-ui-tooling-preview = { group = "androidx.compose.ui", name = "ui-tooling-preview" }
compose-material3 = { group = "androidx.compose.material3", name = "material3" }
room-runtime = { group = "androidx.room", name = "room-runtime", version.ref = "room" }
room-ktx = { group = "androidx.room", name = "room-ktx", version.ref = "room" }
room-compiler = { group = "androidx.room", name = "room-compiler", version.ref = "room" }
datastore-preferences = { group = "androidx.datastore", name = "datastore-preferences", version.ref = "datastore" }
kotlinx-coroutines-test = { group = "org.jetbrains.kotlinx", name = "kotlinx-coroutines-test", version.ref = "coroutines" }
junit = { group = "junit", name = "junit", version.ref = "junit" }
robolectric = { group = "org.robolectric", name = "robolectric", version.ref = "robolectric" }
androidx-test-core = { group = "androidx.test", name = "core", version.ref = "androidxTestCore" }
androidx-test-ext-junit = { group = "androidx.test.ext", name = "junit", version.ref = "androidxTestExt" }

[plugins]
android-application = { id = "com.android.application", version.ref = "agp" }
kotlin-android = { id = "org.jetbrains.kotlin.android", version.ref = "kotlin" }
kotlin-compose = { id = "org.jetbrains.kotlin.plugin.compose", version.ref = "kotlin" }
ksp = { id = "com.google.devtools.ksp", version = "2.0.20-1.0.25" }
```

- [ ] **Step 2: 根 build / settings**

`anzone-mdm/settings.gradle.kts`:
```kotlin
pluginManagement {
    repositories { google(); mavenCentral(); gradlePluginPortal() }
}
dependencyResolutionManagement {
    repositoriesMode.set(RepositoriesMode.FAIL_ON_PROJECT_REPOS)
    repositories { google(); mavenCentral() }
}
rootProject.name = "anzone-mdm"
include(":app")
```

`anzone-mdm/build.gradle.kts`:
```kotlin
plugins {
    alias(libs.plugins.android.application) apply false
    alias(libs.plugins.kotlin.android) apply false
    alias(libs.plugins.kotlin.compose) apply false
    alias(libs.plugins.ksp) apply false
}
```

- [ ] **Step 3: app 模块 build**

`anzone-mdm/app/build.gradle.kts`:
```kotlin
plugins {
    alias(libs.plugins.android.application)
    alias(libs.plugins.kotlin.android)
    alias(libs.plugins.kotlin.compose)
    alias(libs.plugins.ksp)
}

android {
    namespace = "com.anzone.mdm"
    compileSdk = 35

    defaultConfig {
        applicationId = "com.anzone.mdm"
        minSdk = 28
        targetSdk = 35
        versionCode = 1
        versionName = "1.0.0"
        testInstrumentationRunner = "androidx.test.runner.AndroidJUnitRunner"
    }
    buildTypes {
        release {
            isMinifyEnabled = false
        }
    }
    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }
    kotlinOptions { jvmTarget = "17" }
    buildFeatures { compose = true }
    testOptions { unitTests.isIncludeAndroidResources = true }
}

dependencies {
    implementation(libs.androidx.core.ktx)
    implementation(libs.androidx.activity.compose)
    implementation(libs.androidx.lifecycle.runtime.ktx)
    implementation(libs.androidx.lifecycle.viewmodel.compose)
    implementation(platform(libs.compose.bom))
    implementation(libs.compose.ui)
    implementation(libs.compose.ui.tooling.preview)
    implementation(libs.compose.material3)
    implementation(libs.room.runtime)
    implementation(libs.room.ktx)
    ksp(libs.room.compiler)
    implementation(libs.datastore.preferences)

    testImplementation(libs.junit)
    testImplementation(libs.robolectric)
    testImplementation(libs.androidx.test.core)
    testImplementation(libs.androidx.test.ext.junit)
    testImplementation(libs.kotlinx.coroutines.test)
    testImplementation(libs.room.runtime)
}
```

- [ ] **Step 4: 最小 Manifest（占位，后续 Task 13 补全）**

`anzone-mdm/app/src/main/AndroidManifest.xml`:
```xml
<?xml version="1.0" encoding="utf-8"?>
<manifest xmlns:android="http://schemas.android.com/apk/res/android">
    <application
        android:label="anzone"
        android:theme="@android:style/Theme.Material.Light.NoActionBar" />
</manifest>
```

- [ ] **Step 5: 验证构建**

Run: `cd anzone-mdm && ./gradlew :app:assembleDebug`
Expected: BUILD SUCCESSFUL（空壳）。Windows 用 `.\gradlew.bat`。

> 注：需先 `gradle wrapper --gradle-version 8.9` 生成 wrapper，或用本机 Gradle。若无 wrapper，第一步先 `gradle wrapper`。

- [ ] **Step 6: Commit**

```bash
git add anzone-mdm
git commit -m "chore: scaffold anzone-mdm gradle project"
```

---

## Task 1: PasswordHasher（PBKDF2）

**Files:**
- Create: `app/src/main/java/com/anzone/mdm/security/PasswordHasher.kt`
- Test: `app/src/test/java/com/anzone/mdm/security/PasswordHasherTest.kt`

- [ ] **Step 1: 写失败测试**

`PasswordHasherTest.kt`:
```kotlin
package com.anzone.mdm.security

import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test

class PasswordHasherTest {
    @Test fun `same password and salt produce same hash`() {
        val salt = PasswordHasher.generateSalt()
        val h1 = PasswordHasher.hash("secret123".toCharArray(), salt)
        val h2 = PasswordHasher.hash("secret123".toCharArray(), salt)
        assertTrue(h1.contentEquals(h2))
    }

    @Test fun `different salt produces different hash`() {
        val h1 = PasswordHasher.hash("secret123".toCharArray(), PasswordHasher.generateSalt())
        val h2 = PasswordHasher.hash("secret123".toCharArray(), PasswordHasher.generateSalt())
        assertFalse(h1.contentEquals(h2))
    }

    @Test fun `verify accepts correct password`() {
        val salt = PasswordHasher.generateSalt()
        val hash = PasswordHasher.hash("correct".toCharArray(), salt)
        assertTrue(PasswordHasher.verify("correct".toCharArray(), salt, hash))
    }

    @Test fun `verify rejects wrong password`() {
        val salt = PasswordHasher.generateSalt()
        val hash = PasswordHasher.hash("correct".toCharArray(), salt)
        assertFalse(PasswordHasher.verify("wrong".toCharArray(), salt, hash))
    }
}
```

- [ ] **Step 2: 运行确认失败**

Run: `./gradlew :app:testDebugUnitTest --tests "*PasswordHasherTest*"`
Expected: 编译失败（PasswordHasher 未定义）。

- [ ] **Step 3: 实现**

`PasswordHasher.kt`:
```kotlin
package com.anzone.mdm.security

import java.security.MessageDigest
import java.security.SecureRandom
import javax.crypto.SecretKeyFactory
import javax.crypto.spec.PBEKeySpec

object PasswordHasher {
    private const val ITERATIONS = 100_000
    private const val KEY_LENGTH = 256
    private const val SALT_BYTES = 16

    fun generateSalt(): ByteArray =
        ByteArray(SALT_BYTES).also { SecureRandom().nextBytes(it) }

    fun hash(password: CharArray, salt: ByteArray): ByteArray {
        val spec = PBEKeySpec(password, salt, ITERATIONS, KEY_LENGTH)
        return SecretKeyFactory.getInstance("PBKDF2WithHmacSHA256")
            .generateSecret(spec).encoded
    }

    fun verify(password: CharArray, salt: ByteArray, expectedHash: ByteArray): Boolean =
        MessageDigest.isEqual(hash(password, salt), expectedHash)
}
```

- [ ] **Step 4: 运行确认通过**

Run: `./gradlew :app:testDebugUnitTest --tests "*PasswordHasherTest*"`
Expected: PASS（4 个测试）。

- [ ] **Step 5: Commit**

```bash
git add app/src/main/java/com/anzone/mdm/security app/src/test/java/com/anzone/mdm/security
git commit -m "feat: add PBKDF2 password hasher"
```

---

## Task 2: Room 实体 + DAO

**Files:**
- Create: `app/src/main/java/com/anzone/mdm/data/db/WhitelistApp.kt`
- Create: `.../db/LogType.kt`, `.../db/LogEntry.kt`
- Create: `.../db/WhitelistDao.kt`, `.../db/LogDao.kt`
- Create: `.../db/AnzoneDatabase.kt`
- Test: `app/src/test/java/com/anzone/mdm/data/db/DaoTest.kt`

- [ ] **Step 1: 写实体与 DAO 与数据库**

`WhitelistApp.kt`:
```kotlin
package com.anzone.mdm.data.db

import androidx.room.Entity
import androidx.room.PrimaryKey

@Entity(tableName = "whitelist")
data class WhitelistApp(
    @PrimaryKey val packageName: String,
    val appLabel: String,
    val addedAt: Long,
)
```

`LogType.kt`:
```kotlin
package com.anzone.mdm.data.db

enum class LogType {
    APP_LAUNCH, BLOCK_INSTALL, ADMIN_LOGIN, LOGIN_FAILED,
    WHITELIST_CHANGE, MANAGEMENT_EXIT, ROLE_SWITCH, BOOT
}
```

`LogEntry.kt`:
```kotlin
package com.anzone.mdm.data.db

import androidx.room.Entity
import androidx.room.PrimaryKey

@Entity(tableName = "logs")
data class LogEntry(
    @PrimaryKey(autoGenerate = true) val id: Long = 0,
    val timestamp: Long,
    val type: LogType,
    val packageName: String?,
    val detail: String,
)
```

`WhitelistDao.kt`:
```kotlin
package com.anzone.mdm.data.db

import androidx.room.Dao
import androidx.room.Insert
import androidx.room.OnConflictStrategy
import androidx.room.Query
import kotlinx.coroutines.flow.Flow

@Dao
interface WhitelistDao {
    @Query("SELECT * FROM whitelist ORDER BY appLabel")
    fun observeAll(): Flow<List<WhitelistApp>>

    @Query("SELECT * FROM whitelist")
    suspend fun getAll(): List<WhitelistApp>

    @Query("SELECT EXISTS(SELECT 1 FROM whitelist WHERE packageName = :pkg)")
    suspend fun contains(pkg: String): Boolean

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun upsert(app: WhitelistApp)

    @Query("DELETE FROM whitelist WHERE packageName = :pkg")
    suspend fun delete(pkg: String)
}
```

`LogDao.kt`:
```kotlin
package com.anzone.mdm.data.db

import androidx.room.Dao
import androidx.room.Insert
import androidx.room.Query
import kotlinx.coroutines.flow.Flow

@Dao
interface LogDao {
    @Insert suspend fun insert(entry: LogEntry)

    @Query("SELECT * FROM logs ORDER BY timestamp DESC")
    fun observeAll(): Flow<List<LogEntry>>

    @Query("SELECT * FROM logs ORDER BY timestamp DESC")
    suspend fun getAll(): List<LogEntry>

    @Query("DELETE FROM logs") suspend fun clear()
}
```

`AnzoneDatabase.kt`:
```kotlin
package com.anzone.mdm.data.db

import androidx.room.Database
import androidx.room.RoomDatabase
import androidx.room.TypeConverter
import androidx.room.TypeConverters

class LogTypeConverter {
    @TypeConverter fun toName(t: LogType): String = t.name
    @TypeConverter fun fromName(s: String): LogType = LogType.valueOf(s)
}

@Database(entities = [WhitelistApp::class, LogEntry::class], version = 1, exportSchema = false)
@TypeConverters(LogTypeConverter::class)
abstract class AnzoneDatabase : RoomDatabase() {
    abstract fun whitelistDao(): WhitelistDao
    abstract fun logDao(): LogDao
}
```

- [ ] **Step 2: 写测试（Robolectric + 内存库）**

`DaoTest.kt`:
```kotlin
package com.anzone.mdm.data.db

import androidx.room.Room
import androidx.test.core.app.ApplicationProvider
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.test.runTest
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner

@RunWith(RobolectricTestRunner::class)
class DaoTest {
    private lateinit var db: AnzoneDatabase
    private lateinit var whitelist: WhitelistDao
    private lateinit var logs: LogDao

    @Before fun setup() {
        db = Room.inMemoryDatabaseBuilder(
            ApplicationProvider.getApplicationContext(), AnzoneDatabase::class.java
        ).allowMainThreadQueries().build()
        whitelist = db.whitelistDao()
        logs = db.logDao()
    }

    @After fun tearDown() = db.close()

    @Test fun `upsert and contains`() = runTest {
        whitelist.upsert(WhitelistApp("com.a", "A", 1L))
        assertTrue(whitelist.contains("com.a"))
        assertFalse(whitelist.contains("com.b"))
    }

    @Test fun `delete removes entry`() = runTest {
        whitelist.upsert(WhitelistApp("com.a", "A", 1L))
        whitelist.delete("com.a")
        assertFalse(whitelist.contains("com.a"))
    }

    @Test fun `log insert and ordering desc`() = runTest {
        logs.insert(LogEntry(timestamp = 1, type = LogType.BOOT, packageName = null, detail = "x"))
        logs.insert(LogEntry(timestamp = 2, type = LogType.APP_LAUNCH, packageName = "com.a", detail = "y"))
        val all = logs.observeAll().first()
        assertEquals(2L, all.first().timestamp)
    }

    @Test fun `clear logs`() = runTest {
        logs.insert(LogEntry(timestamp = 1, type = LogType.BOOT, packageName = null, detail = "x"))
        logs.clear()
        assertTrue(logs.getAll().isEmpty())
    }
}
```

- [ ] **Step 3: 运行确认失败 → 实现已在 Step 1 → 运行确认通过**

Run: `./gradlew :app:testDebugUnitTest --tests "*DaoTest*"`
Expected: PASS（4 个测试）。若 Room KSP 报错先确认 Step 1 文件齐全。

- [ ] **Step 4: Commit**

```bash
git add app/src/main/java/com/anzone/mdm/data/db app/src/test/java/com/anzone/mdm/data/db
git commit -m "feat: add Room entities and DAOs for whitelist and logs"
```

---

## Task 3: WhitelistRepository

**Files:**
- Create: `app/src/main/java/com/anzone/mdm/data/WhitelistRepository.kt`
- Test: `app/src/test/java/com/anzone/mdm/data/WhitelistRepositoryTest.kt`

- [ ] **Step 1: 写失败测试**

`WhitelistRepositoryTest.kt`:
```kotlin
package com.anzone.mdm.data

import androidx.room.Room
import androidx.test.core.app.ApplicationProvider
import com.anzone.mdm.data.db.AnzoneDatabase
import kotlinx.coroutines.test.runTest
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner

@RunWith(RobolectricTestRunner::class)
class WhitelistRepositoryTest {
    private lateinit var db: AnzoneDatabase
    private lateinit var repo: WhitelistRepository

    @Before fun setup() {
        db = Room.inMemoryDatabaseBuilder(
            ApplicationProvider.getApplicationContext(), AnzoneDatabase::class.java
        ).allowMainThreadQueries().build()
        repo = WhitelistRepository(db.whitelistDao())
    }

    @After fun tearDown() = db.close()

    @Test fun `add then isAllowed true`() = runTest {
        repo.add("com.tencent.mm", "微信")
        assertTrue(repo.isAllowed("com.tencent.mm"))
    }

    @Test fun `not added isAllowed false`() = runTest {
        assertFalse(repo.isAllowed("com.evil.app"))
    }

    @Test fun `remove revokes`() = runTest {
        repo.add("com.a", "A")
        repo.remove("com.a")
        assertFalse(repo.isAllowed("com.a"))
    }

    @Test fun `getAll returns added`() = runTest {
        repo.add("com.a", "A")
        repo.add("com.b", "B")
        assertEquals(2, repo.getAll().size)
    }
}
```

- [ ] **Step 2: 运行确认失败**

Run: `./gradlew :app:testDebugUnitTest --tests "*WhitelistRepositoryTest*"`
Expected: 编译失败（WhitelistRepository 未定义）。

- [ ] **Step 3: 实现**

`WhitelistRepository.kt`:
```kotlin
package com.anzone.mdm.data

import com.anzone.mdm.data.db.WhitelistApp
import com.anzone.mdm.data.db.WhitelistDao
import kotlinx.coroutines.flow.Flow

class WhitelistRepository(private val dao: WhitelistDao) {
    fun observeAll(): Flow<List<WhitelistApp>> = dao.observeAll()
    suspend fun getAll(): List<WhitelistApp> = dao.getAll()
    suspend fun isAllowed(pkg: String): Boolean = dao.contains(pkg)
    suspend fun add(pkg: String, label: String) =
        dao.upsert(WhitelistApp(pkg, label, System.currentTimeMillis()))
    suspend fun remove(pkg: String) = dao.delete(pkg)
}
```

- [ ] **Step 4: 运行确认通过**

Run: `./gradlew :app:testDebugUnitTest --tests "*WhitelistRepositoryTest*"`
Expected: PASS（4 个测试）。

- [ ] **Step 5: Commit**

```bash
git add app/src/main/java/com/anzone/mdm/data/WhitelistRepository.kt app/src/test/java/com/anzone/mdm/data/WhitelistRepositoryTest.kt
git commit -m "feat: add WhitelistRepository"
```

---

## Task 4: LogRepository

**Files:**
- Create: `app/src/main/java/com/anzone/mdm/data/LogRepository.kt`
- Test: `app/src/test/java/com/anzone/mdm/data/LogRepositoryTest.kt`

- [ ] **Step 1: 写失败测试**

`LogRepositoryTest.kt`:
```kotlin
package com.anzone.mdm.data

import androidx.room.Room
import androidx.test.core.app.ApplicationProvider
import com.anzone.mdm.data.db.AnzoneDatabase
import com.anzone.mdm.data.db.LogType
import kotlinx.coroutines.test.runTest
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner

@RunWith(RobolectricTestRunner::class)
class LogRepositoryTest {
    private lateinit var db: AnzoneDatabase
    private lateinit var repo: LogRepository

    @Before fun setup() {
        db = Room.inMemoryDatabaseBuilder(
            ApplicationProvider.getApplicationContext(), AnzoneDatabase::class.java
        ).allowMainThreadQueries().build()
        repo = LogRepository(db.logDao())
    }

    @After fun tearDown() = db.close()

    @Test fun `record persists entry`() = runTest {
        repo.record(LogType.APP_LAUNCH, "com.a", "启动 A")
        val all = repo.getAll()
        assertEquals(1, all.size)
        assertEquals(LogType.APP_LAUNCH, all.first().type)
    }

    @Test fun `clear empties`() = runTest {
        repo.record(LogType.BOOT, null, "开机")
        repo.clear()
        assertTrue(repo.getAll().isEmpty())
    }

    @Test fun `export produces non-empty text with header line`() = runTest {
        repo.record(LogType.ADMIN_LOGIN, null, "管理员登录")
        val text = repo.exportAsText()
        assertTrue(text.contains("ADMIN_LOGIN"))
        assertTrue(text.contains("管理员登录"))
    }
}
```

- [ ] **Step 2: 运行确认失败**

Run: `./gradlew :app:testDebugUnitTest --tests "*LogRepositoryTest*"`
Expected: 编译失败。

- [ ] **Step 3: 实现**

`LogRepository.kt`:
```kotlin
package com.anzone.mdm.data

import com.anzone.mdm.data.db.LogDao
import com.anzone.mdm.data.db.LogEntry
import com.anzone.mdm.data.db.LogType
import kotlinx.coroutines.flow.Flow
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale

class LogRepository(private val dao: LogDao) {
    private val fmt = SimpleDateFormat("yyyy-MM-dd HH:mm:ss", Locale.getDefault())

    fun observeAll(): Flow<List<LogEntry>> = dao.observeAll()
    suspend fun getAll(): List<LogEntry> = dao.getAll()
    suspend fun clear() = dao.clear()

    suspend fun record(type: LogType, packageName: String?, detail: String) =
        dao.insert(LogEntry(timestamp = System.currentTimeMillis(), type = type,
            packageName = packageName, detail = detail))

    suspend fun exportAsText(): String = getAll().joinToString("\n") { e ->
        "${fmt.format(Date(e.timestamp))}\t${e.type}\t${e.packageName ?: "-"}\t${e.detail}"
    }
}
```

- [ ] **Step 4: 运行确认通过**

Run: `./gradlew :app:testDebugUnitTest --tests "*LogRepositoryTest*"`
Expected: PASS（3 个测试）。

- [ ] **Step 5: Commit**

```bash
git add app/src/main/java/com/anzone/mdm/data/LogRepository.kt app/src/test/java/com/anzone/mdm/data/LogRepositoryTest.kt
git commit -m "feat: add LogRepository with text export"
```

---

## Task 5: AuthRepository（两套账号 + DataStore）

**Files:**
- Create: `app/src/main/java/com/anzone/mdm/data/AuthRepository.kt`
- Test: `app/src/test/java/com/anzone/mdm/data/AuthRepositoryTest.kt`

设计：用 `DataStore<Preferences>` 存两套账号的用户名、hash(Base64)、salt(Base64) 及 `is_first_run`。`Role` 枚举 ADMIN/NORMAL。

- [ ] **Step 1: 写失败测试**

`AuthRepositoryTest.kt`:
```kotlin
package com.anzone.mdm.data

import androidx.datastore.core.DataStore
import androidx.datastore.preferences.core.Preferences
import androidx.datastore.preferences.preferencesDataStoreFile
import androidx.datastore.preferences.core.PreferenceDataStoreFactory
import androidx.test.core.app.ApplicationProvider
import kotlinx.coroutines.test.runTest
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import java.io.File

@RunWith(RobolectricTestRunner::class)
class AuthRepositoryTest {
    private lateinit var repo: AuthRepository

    @Before fun setup() {
        val ctx = ApplicationProvider.getApplicationContext<android.content.Context>()
        val file = File(ctx.cacheDir, "auth_test_${System.nanoTime()}.preferences_pb")
        val store: DataStore<Preferences> = PreferenceDataStoreFactory.create { file }
        repo = AuthRepository(store)
    }

    @Test fun `first run true before setup`() = runTest {
        assertTrue(repo.isFirstRun())
    }

    @Test fun `after createAccounts first run false`() = runTest {
        repo.createAccounts("boss", "adminpw", "worker", "userpw")
        assertFalse(repo.isFirstRun())
    }

    @Test fun `admin login accepts correct credentials`() = runTest {
        repo.createAccounts("boss", "adminpw", "worker", "userpw")
        assertTrue(repo.verifyAdmin("boss", "adminpw"))
        assertFalse(repo.verifyAdmin("boss", "wrong"))
        assertFalse(repo.verifyAdmin("nobody", "adminpw"))
    }

    @Test fun `normal login accepts correct credentials`() = runTest {
        repo.createAccounts("boss", "adminpw", "worker", "userpw")
        assertTrue(repo.verifyNormal("worker", "userpw"))
        assertFalse(repo.verifyNormal("worker", "wrong"))
    }

    @Test fun `changeAdminPassword updates`() = runTest {
        repo.createAccounts("boss", "adminpw", "worker", "userpw")
        repo.changeAdminPassword("newpw")
        assertFalse(repo.verifyAdmin("boss", "adminpw"))
        assertTrue(repo.verifyAdmin("boss", "newpw"))
    }
}
```

- [ ] **Step 2: 运行确认失败**

Run: `./gradlew :app:testDebugUnitTest --tests "*AuthRepositoryTest*"`
Expected: 编译失败。

- [ ] **Step 3: 实现**

`AuthRepository.kt`:
```kotlin
package com.anzone.mdm.data

import androidx.datastore.core.DataStore
import androidx.datastore.preferences.core.Preferences
import androidx.datastore.preferences.core.booleanPreferencesKey
import androidx.datastore.preferences.core.edit
import androidx.datastore.preferences.core.stringPreferencesKey
import com.anzone.mdm.security.PasswordHasher
import kotlinx.coroutines.flow.first
import android.util.Base64

enum class Role { ADMIN, NORMAL }

class AuthRepository(private val store: DataStore<Preferences>) {
    private object Keys {
        val FIRST_RUN = booleanPreferencesKey("is_first_run")
        val ADMIN_USER = stringPreferencesKey("admin_username")
        val ADMIN_HASH = stringPreferencesKey("admin_password_hash")
        val ADMIN_SALT = stringPreferencesKey("admin_password_salt")
        val NORMAL_USER = stringPreferencesKey("normal_username")
        val NORMAL_HASH = stringPreferencesKey("normal_password_hash")
        val NORMAL_SALT = stringPreferencesKey("normal_password_salt")
    }

    private fun b64(b: ByteArray) = Base64.encodeToString(b, Base64.NO_WRAP)
    private fun unb64(s: String) = Base64.decode(s, Base64.NO_WRAP)

    suspend fun isFirstRun(): Boolean =
        store.data.first()[Keys.FIRST_RUN] ?: true

    suspend fun createAccounts(
        adminUser: String, adminPw: String, normalUser: String, normalPw: String
    ) {
        val aSalt = PasswordHasher.generateSalt()
        val nSalt = PasswordHasher.generateSalt()
        val aHash = PasswordHasher.hash(adminPw.toCharArray(), aSalt)
        val nHash = PasswordHasher.hash(normalPw.toCharArray(), nSalt)
        store.edit {
            it[Keys.ADMIN_USER] = adminUser
            it[Keys.ADMIN_SALT] = b64(aSalt)
            it[Keys.ADMIN_HASH] = b64(aHash)
            it[Keys.NORMAL_USER] = normalUser
            it[Keys.NORMAL_SALT] = b64(nSalt)
            it[Keys.NORMAL_HASH] = b64(nHash)
            it[Keys.FIRST_RUN] = false
        }
    }

    suspend fun adminUsername(): String = store.data.first()[Keys.ADMIN_USER] ?: ""
    suspend fun normalUsername(): String = store.data.first()[Keys.NORMAL_USER] ?: ""

    suspend fun verifyAdmin(user: String, pw: String): Boolean =
        verify(user, pw, Keys.ADMIN_USER, Keys.ADMIN_SALT, Keys.ADMIN_HASH)

    suspend fun verifyNormal(user: String, pw: String): Boolean =
        verify(user, pw, Keys.NORMAL_USER, Keys.NORMAL_SALT, Keys.NORMAL_HASH)

    private suspend fun verify(
        user: String, pw: String,
        userKey: Preferences.Key<String>,
        saltKey: Preferences.Key<String>,
        hashKey: Preferences.Key<String>,
    ): Boolean {
        val prefs = store.data.first()
        if (prefs[userKey] != user) return false
        val salt = prefs[saltKey]?.let(::unb64) ?: return false
        val hash = prefs[hashKey]?.let(::unb64) ?: return false
        return PasswordHasher.verify(pw.toCharArray(), salt, hash)
    }

    suspend fun changeAdminPassword(newPw: String) =
        changePassword(newPw, Keys.ADMIN_SALT, Keys.ADMIN_HASH)

    suspend fun changeNormalPassword(newPw: String) =
        changePassword(newPw, Keys.NORMAL_SALT, Keys.NORMAL_HASH)

    private suspend fun changePassword(
        newPw: String, saltKey: Preferences.Key<String>, hashKey: Preferences.Key<String>,
    ) {
        val salt = PasswordHasher.generateSalt()
        val hash = PasswordHasher.hash(newPw.toCharArray(), salt)
        store.edit { it[saltKey] = b64(salt); it[hashKey] = b64(hash) }
    }
}
```

> 注：测试在 Robolectric 下运行，`android.util.Base64` 可用。

- [ ] **Step 4: 运行确认通过**

Run: `./gradlew :app:testDebugUnitTest --tests "*AuthRepositoryTest*"`
Expected: PASS（5 个测试）。

- [ ] **Step 5: Commit**

```bash
git add app/src/main/java/com/anzone/mdm/data/AuthRepository.kt app/src/test/java/com/anzone/mdm/data/AuthRepositoryTest.kt
git commit -m "feat: add AuthRepository with admin and normal accounts"
```

---

## Task 6: SessionState

**Files:**
- Create: `app/src/main/java/com/anzone/mdm/data/SessionState.kt`
- Test: `app/src/test/java/com/anzone/mdm/data/SessionStateTest.kt`

职责：内存保存当前 `Role`，提供切换；持久化"上次会话角色"（重启复位 NORMAL 的逻辑由 Activity 决定，SessionState 只管内存态 + 暴露 StateFlow）。

- [ ] **Step 1: 写失败测试**

`SessionStateTest.kt`:
```kotlin
package com.anzone.mdm.data

import kotlinx.coroutines.test.runTest
import org.junit.Assert.assertEquals
import org.junit.Test

class SessionStateTest {
    @Test fun `default role is NORMAL`() {
        assertEquals(Role.NORMAL, SessionState().role.value)
    }

    @Test fun `switchTo updates role`() = runTest {
        val s = SessionState()
        s.switchTo(Role.ADMIN)
        assertEquals(Role.ADMIN, s.role.value)
        s.switchTo(Role.NORMAL)
        assertEquals(Role.NORMAL, s.role.value)
    }
}
```

- [ ] **Step 2: 运行确认失败**

Run: `./gradlew :app:testDebugUnitTest --tests "*SessionStateTest*"`
Expected: 编译失败。

- [ ] **Step 3: 实现**

`SessionState.kt`:
```kotlin
package com.anzone.mdm.data

import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow

class SessionState {
    private val _role = MutableStateFlow(Role.NORMAL)
    val role: StateFlow<Role> = _role.asStateFlow()

    fun switchTo(role: Role) { _role.value = role }
}
```

- [ ] **Step 4: 运行确认通过 → Commit**

Run: `./gradlew :app:testDebugUnitTest --tests "*SessionStateTest*"`
Expected: PASS（2 个测试）。
```bash
git add app/src/main/java/com/anzone/mdm/data/SessionState.kt app/src/test/java/com/anzone/mdm/data/SessionStateTest.kt
git commit -m "feat: add SessionState"
```

---

## Task 7: PolicyManager（DevicePolicyManager 封装）

**Files:**
- Create: `app/src/main/java/com/anzone/mdm/device/PolicyManager.kt`
- Create: `app/src/main/java/com/anzone/mdm/device/AnzoneDeviceAdminReceiver.kt`

此层薄封装系统 API，**无法在 JVM 单测**（需真机 Device Owner），列入手动验收。仅保证编译正确与接口清晰。

- [ ] **Step 1: 实现 DeviceAdminReceiver**

`AnzoneDeviceAdminReceiver.kt`:
```kotlin
package com.anzone.mdm.device

import android.app.admin.DeviceAdminReceiver
import android.content.ComponentName
import android.content.Context

class AnzoneDeviceAdminReceiver : DeviceAdminReceiver() {
    companion object {
        fun componentName(ctx: Context) =
            ComponentName(ctx, AnzoneDeviceAdminReceiver::class.java)
    }
}
```

- [ ] **Step 2: 实现 PolicyManager**

`PolicyManager.kt`:
```kotlin
package com.anzone.mdm.device

import android.app.admin.DevicePolicyManager
import android.content.Context
import android.os.UserManager

class PolicyManager(private val context: Context) {
    private val dpm =
        context.getSystemService(Context.DEVICE_POLICY_SERVICE) as DevicePolicyManager
    private val admin = AnzoneDeviceAdminReceiver.componentName(context)

    fun isDeviceOwner(): Boolean = dpm.isDeviceOwnerApp(context.packageName)

    /** 进入管控：锁定可运行的任务集 + 施加用户限制。*/
    fun applyManagement(lockTaskPackages: Array<String>) {
        if (!isDeviceOwner()) return
        dpm.setLockTaskPackages(admin, lockTaskPackages + context.packageName)
        dpm.addUserRestriction(admin, UserManager.DISALLOW_INSTALL_APPS)
        dpm.addUserRestriction(admin, UserManager.DISALLOW_UNINSTALL_APPS)
        dpm.addUserRestriction(admin, UserManager.DISALLOW_INSTALL_UNKNOWN_SOURCES)
        dpm.addUserRestriction(admin, UserManager.DISALLOW_SAFE_BOOT)
        dpm.addUserRestriction(admin, UserManager.DISALLOW_FACTORY_RESET)
        dpm.addUserRestriction(admin, UserManager.DISALLOW_DEBUGGING_FEATURES)
    }

    /** 解除管控：移除限制，准备退出。*/
    fun releaseManagement() {
        if (!isDeviceOwner()) return
        listOf(
            UserManager.DISALLOW_INSTALL_APPS,
            UserManager.DISALLOW_UNINSTALL_APPS,
            UserManager.DISALLOW_INSTALL_UNKNOWN_SOURCES,
            UserManager.DISALLOW_SAFE_BOOT,
            UserManager.DISALLOW_FACTORY_RESET,
            UserManager.DISALLOW_DEBUGGING_FEATURES,
        ).forEach { dpm.clearUserRestriction(admin, it) }
        dpm.setLockTaskPackages(admin, emptyArray())
    }

    /** 彻底放弃 Device Owner（不可逆，需重新 provisioning）。*/
    fun clearDeviceOwner() {
        if (isDeviceOwner()) dpm.clearDeviceOwnerApp(context.packageName)
    }
}
```

- [ ] **Step 3: 编译验证**

Run: `./gradlew :app:compileDebugKotlin`
Expected: BUILD SUCCESSFUL。

- [ ] **Step 4: Commit**

```bash
git add app/src/main/java/com/anzone/mdm/device
git commit -m "feat: add PolicyManager and DeviceAdminReceiver"
```

---

## Task 8: ServiceLocator（AnzoneApp）

**Files:**
- Create: `app/src/main/java/com/anzone/mdm/AnzoneApp.kt`

集中构造单例依赖，供 Activity 取用。无 DI 框架。

- [ ] **Step 1: 实现**

`AnzoneApp.kt`:
```kotlin
package com.anzone.mdm

import android.app.Application
import android.content.Context
import androidx.datastore.core.DataStore
import androidx.datastore.preferences.core.Preferences
import androidx.datastore.preferences.preferencesDataStore
import androidx.room.Room
import com.anzone.mdm.data.AuthRepository
import com.anzone.mdm.data.LogRepository
import com.anzone.mdm.data.SessionState
import com.anzone.mdm.data.WhitelistRepository
import com.anzone.mdm.data.db.AnzoneDatabase
import com.anzone.mdm.device.PolicyManager

private val Context.authStore: DataStore<Preferences> by preferencesDataStore(name = "auth")

class AnzoneApp : Application() {
    lateinit var whitelist: WhitelistRepository; private set
    lateinit var logs: LogRepository; private set
    lateinit var auth: AuthRepository; private set
    lateinit var policy: PolicyManager; private set
    val session = SessionState()

    override fun onCreate() {
        super.onCreate()
        val db = Room.databaseBuilder(this, AnzoneDatabase::class.java, "anzone.db").build()
        whitelist = WhitelistRepository(db.whitelistDao())
        logs = LogRepository(db.logDao())
        auth = AuthRepository(authStore)
        policy = PolicyManager(this)
    }

    companion object {
        fun from(ctx: Context) = ctx.applicationContext as AnzoneApp
    }
}
```

- [ ] **Step 2: 编译验证 → Commit**

Run: `./gradlew :app:compileDebugKotlin`
Expected: BUILD SUCCESSFUL。
```bash
git add app/src/main/java/com/anzone/mdm/AnzoneApp.kt
git commit -m "feat: add Application-level ServiceLocator"
```

---

## Task 9: 首次创建向导 Compose 屏

**Files:**
- Create: `app/src/main/java/com/anzone/mdm/ui/admin/SetupWizardScreen.kt`

两步表单：管理员账号 → 普通用户账号。校验非空、两次密码一致。提交回调 `onCreate(adminUser, adminPw, normalUser, normalPw)`。

- [ ] **Step 1: 实现可组合屏（无状态，纯回调，便于复用）**

`SetupWizardScreen.kt`:
```kotlin
package com.anzone.mdm.ui.admin

import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp

@Composable
fun SetupWizardScreen(onCreate: (String, String, String, String) -> Unit) {
    var step by remember { mutableIntStateOf(0) }
    var adminUser by remember { mutableStateOf("") }
    var adminPw by remember { mutableStateOf("") }
    var normalUser by remember { mutableStateOf("") }
    var normalPw by remember { mutableStateOf("") }
    var pwConfirm by remember { mutableStateOf("") }
    var error by remember { mutableStateOf<String?>(null) }

    Column(Modifier.fillMaxSize().padding(24.dp), verticalArrangement = Arrangement.Center) {
        Text(if (step == 0) "创建管理员账号" else "创建普通用户账号",
            style = MaterialTheme.typography.headlineSmall)
        Spacer(Modifier.height(16.dp))
        val user = if (step == 0) adminUser else normalUser
        val pw = if (step == 0) adminPw else normalPw
        OutlinedTextField(value = user, onValueChange = {
            if (step == 0) adminUser = it else normalUser = it
        }, label = { Text("用户名") }, singleLine = true, modifier = Modifier.fillMaxWidth())
        OutlinedTextField(value = pw, onValueChange = {
            if (step == 0) adminPw = it else normalPw = it
        }, label = { Text("密码") }, singleLine = true, modifier = Modifier.fillMaxWidth())
        OutlinedTextField(value = pwConfirm, onValueChange = { pwConfirm = it },
            label = { Text("确认密码") }, singleLine = true, modifier = Modifier.fillMaxWidth())
        error?.let { Text(it, color = MaterialTheme.colorScheme.error) }
        Spacer(Modifier.height(16.dp))
        Button(onClick = {
            when {
                user.isBlank() || pw.isBlank() -> error = "用户名和密码不能为空"
                pw != pwConfirm -> error = "两次密码不一致"
                step == 0 -> { error = null; pwConfirm = ""; step = 1 }
                else -> onCreate(adminUser, adminPw, normalUser, normalPw)
            }
        }, modifier = Modifier.fillMaxWidth()) {
            Text(if (step == 0) "下一步" else "完成创建")
        }
    }
}
```

- [ ] **Step 2: 编译验证 → Commit**

Run: `./gradlew :app:compileDebugKotlin`
Expected: BUILD SUCCESSFUL。
```bash
git add app/src/main/java/com/anzone/mdm/ui/admin/SetupWizardScreen.kt
git commit -m "feat: add first-run setup wizard screen"
```

---

## Task 10: 登录屏

**Files:**
- Create: `app/src/main/java/com/anzone/mdm/ui/admin/LoginScreen.kt`

- [ ] **Step 1: 实现**

`LoginScreen.kt`:
```kotlin
package com.anzone.mdm.ui.admin

import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp

@Composable
fun LoginScreen(title: String, error: String?, onSubmit: (String, String) -> Unit) {
    var user by remember { mutableStateOf("") }
    var pw by remember { mutableStateOf("") }
    Column(Modifier.fillMaxSize().padding(24.dp), verticalArrangement = Arrangement.Center) {
        Text(title, style = MaterialTheme.typography.headlineSmall)
        Spacer(Modifier.height(16.dp))
        OutlinedTextField(value = user, onValueChange = { user = it },
            label = { Text("用户名") }, singleLine = true, modifier = Modifier.fillMaxWidth())
        OutlinedTextField(value = pw, onValueChange = { pw = it },
            label = { Text("密码") }, singleLine = true, modifier = Modifier.fillMaxWidth())
        error?.let { Text(it, color = MaterialTheme.colorScheme.error) }
        Spacer(Modifier.height(16.dp))
        Button(onClick = { onSubmit(user, pw) }, modifier = Modifier.fillMaxWidth()) {
            Text("登录")
        }
    }
}
```

- [ ] **Step 2: 编译验证 → Commit**

Run: `./gradlew :app:compileDebugKotlin`
```bash
git add app/src/main/java/com/anzone/mdm/ui/admin/LoginScreen.kt
git commit -m "feat: add login screen"
```

---

## Task 11: 白名单管理 / 日志 / 设置 屏

**Files:**
- Create: `app/src/main/java/com/anzone/mdm/ui/admin/WhitelistScreen.kt`
- Create: `app/src/main/java/com/anzone/mdm/ui/admin/LogsScreen.kt`
- Create: `app/src/main/java/com/anzone/mdm/ui/admin/SettingsScreen.kt`

数据通过参数传入（List + 回调），保持可组合屏无状态。

- [ ] **Step 1: WhitelistScreen**

`WhitelistScreen.kt`:
```kotlin
package com.anzone.mdm.ui.admin

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.anzone.mdm.data.db.WhitelistApp

data class InstalledApp(val packageName: String, val label: String)

@Composable
fun WhitelistScreen(
    current: List<WhitelistApp>,
    installed: List<InstalledApp>,
    onAdd: (InstalledApp) -> Unit,
    onRemove: (String) -> Unit,
) {
    var picking by remember { mutableStateOf(false) }
    Column(Modifier.fillMaxSize().padding(16.dp)) {
        Row(verticalAlignment = androidx.compose.ui.Alignment.CenterVertically) {
            Text("白名单（${current.size}）", style = MaterialTheme.typography.titleMedium)
            Spacer(Modifier.weight(1f))
            Button(onClick = { picking = true }) { Text("添加") }
        }
        LazyColumn(Modifier.weight(1f)) {
            items(current, key = { it.packageName }) { app ->
                ListItem(
                    headlineContent = { Text(app.appLabel) },
                    supportingContent = { Text(app.packageName) },
                    trailingContent = {
                        TextButton(onClick = { onRemove(app.packageName) }) { Text("删除") }
                    }
                )
            }
        }
    }
    if (picking) {
        AlertDialog(
            onDismissRequest = { picking = false },
            confirmButton = { TextButton(onClick = { picking = false }) { Text("关闭") } },
            title = { Text("选择已安装应用") },
            text = {
                LazyColumn(Modifier.heightIn(max = 400.dp)) {
                    items(installed, key = { it.packageName }) { app ->
                        ListItem(
                            headlineContent = { Text(app.label) },
                            supportingContent = { Text(app.packageName) },
                            trailingContent = {
                                TextButton(onClick = { onAdd(app); picking = false }) { Text("添加") }
                            }
                        )
                    }
                }
            }
        )
    }
}
```

- [ ] **Step 2: LogsScreen**

`LogsScreen.kt`:
```kotlin
package com.anzone.mdm.ui.admin

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.anzone.mdm.data.db.LogEntry
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale

@Composable
fun LogsScreen(logs: List<LogEntry>, onClear: () -> Unit, onExport: () -> Unit) {
    val fmt = SimpleDateFormat("MM-dd HH:mm:ss", Locale.getDefault())
    Column(Modifier.fillMaxSize().padding(16.dp)) {
        Row {
            Text("日志（${logs.size}）", style = MaterialTheme.typography.titleMedium)
            Spacer(Modifier.weight(1f))
            TextButton(onClick = onExport) { Text("导出") }
            TextButton(onClick = onClear) { Text("清空") }
        }
        LazyColumn(Modifier.weight(1f)) {
            items(logs, key = { it.id }) { e ->
                ListItem(
                    headlineContent = { Text("${e.type}  ${e.detail}") },
                    supportingContent = { Text("${fmt.format(Date(e.timestamp))}  ${e.packageName ?: ""}") }
                )
            }
        }
    }
}
```

- [ ] **Step 3: SettingsScreen**

`SettingsScreen.kt`:
```kotlin
package com.anzone.mdm.ui.admin

import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp

@Composable
fun SettingsScreen(
    onChangeAdminPw: (String) -> Unit,
    onChangeNormalPw: (String) -> Unit,
    onSwitchToNormal: () -> Unit,
    onReleaseManagement: (String) -> Unit, // 传入二次确认的管理员密码
) {
    var adminPw by remember { mutableStateOf("") }
    var normalPw by remember { mutableStateOf("") }
    var confirmPw by remember { mutableStateOf("") }
    var showRelease by remember { mutableStateOf(false) }

    Column(Modifier.fillMaxSize().padding(16.dp)) {
        Text("修改管理员密码", style = MaterialTheme.typography.titleSmall)
        OutlinedTextField(adminPw, { adminPw = it }, label = { Text("新密码") },
            singleLine = true, modifier = Modifier.fillMaxWidth())
        Button(onClick = { onChangeAdminPw(adminPw); adminPw = "" }) { Text("更新管理员密码") }
        Spacer(Modifier.height(16.dp))

        Text("修改普通用户密码", style = MaterialTheme.typography.titleSmall)
        OutlinedTextField(normalPw, { normalPw = it }, label = { Text("新密码") },
            singleLine = true, modifier = Modifier.fillMaxWidth())
        Button(onClick = { onChangeNormalPw(normalPw); normalPw = "" }) { Text("更新普通用户密码") }
        Spacer(Modifier.height(24.dp))

        Button(onClick = onSwitchToNormal, modifier = Modifier.fillMaxWidth()) {
            Text("切换普通用户（进入 Kiosk）")
        }
        Spacer(Modifier.height(8.dp))
        OutlinedButton(onClick = { showRelease = true }, modifier = Modifier.fillMaxWidth()) {
            Text("解除管控（退出）")
        }
    }

    if (showRelease) {
        AlertDialog(
            onDismissRequest = { showRelease = false },
            title = { Text("确认解除管控") },
            text = {
                Column {
                    Text("解除后设备恢复正常，可自由装卸软件。重新管控需 IT 重新 provisioning。请再次输入管理员密码确认。")
                    OutlinedTextField(confirmPw, { confirmPw = it },
                        label = { Text("管理员密码") }, singleLine = true)
                }
            },
            confirmButton = {
                TextButton(onClick = { onReleaseManagement(confirmPw); showRelease = false }) {
                    Text("确认解除")
                }
            },
            dismissButton = { TextButton(onClick = { showRelease = false }) { Text("取消") } }
        )
    }
}
```

- [ ] **Step 4: 编译验证 → Commit**

Run: `./gradlew :app:compileDebugKotlin`
```bash
git add app/src/main/java/com/anzone/mdm/ui/admin
git commit -m "feat: add whitelist, logs, settings admin screens"
```

---

## Task 12: AdminActivity（向导/登录/Tab 编排 + ViewModel）

**Files:**
- Create: `app/src/main/java/com/anzone/mdm/ui/admin/AdminActivity.kt`
- Create: `app/src/main/java/com/anzone/mdm/ui/admin/AdminViewModel.kt`

ViewModel 持有 Repository，暴露状态与动作；Activity 决定显示向导/登录/主界面。

- [ ] **Step 1: AdminViewModel**

`AdminViewModel.kt`:
```kotlin
package com.anzone.mdm.ui.admin

import android.content.pm.PackageManager
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.anzone.mdm.AnzoneApp
import com.anzone.mdm.data.Role
import com.anzone.mdm.data.db.LogType
import kotlinx.coroutines.flow.*
import kotlinx.coroutines.launch

class AdminViewModel(private val app: AnzoneApp) : ViewModel() {
    val whitelist = app.whitelist.observeAll()
        .stateIn(viewModelScope, SharingStarted.WhileSubscribed(5000), emptyList())
    val logs = app.logs.observeAll()
        .stateIn(viewModelScope, SharingStarted.WhileSubscribed(5000), emptyList())

    private val _firstRun = MutableStateFlow(true)
    val firstRun: StateFlow<Boolean> = _firstRun
    private val _authed = MutableStateFlow(false)
    val authed: StateFlow<Boolean> = _authed
    private val _loginError = MutableStateFlow<String?>(null)
    val loginError: StateFlow<String?> = _loginError

    init { viewModelScope.launch { _firstRun.value = app.auth.isFirstRun() } }

    fun installedApps(): List<InstalledApp> {
        val pm = app.packageManager
        val intent = android.content.Intent(android.content.Intent.ACTION_MAIN)
            .addCategory(android.content.Intent.CATEGORY_LAUNCHER)
        return pm.queryIntentActivities(intent, 0).map {
            InstalledApp(it.activityInfo.packageName,
                it.loadLabel(pm).toString())
        }.distinctBy { it.packageName }
    }

    fun createAccounts(au: String, ap: String, nu: String, np: String) = viewModelScope.launch {
        app.auth.createAccounts(au, ap, nu, np)
        _firstRun.value = false
        _authed.value = true
    }

    fun login(user: String, pw: String) = viewModelScope.launch {
        if (app.auth.verifyAdmin(user, pw)) {
            _authed.value = true; _loginError.value = null
            app.logs.record(LogType.ADMIN_LOGIN, null, "管理员登录")
        } else {
            _loginError.value = "用户名或密码错误"
            app.logs.record(LogType.LOGIN_FAILED, null, "管理员登录失败")
        }
    }

    fun addApp(a: InstalledApp) = viewModelScope.launch {
        app.whitelist.add(a.packageName, a.label)
        app.logs.record(LogType.WHITELIST_CHANGE, a.packageName, "添加白名单 ${a.label}")
    }

    fun removeApp(pkg: String) = viewModelScope.launch {
        app.whitelist.remove(pkg)
        app.logs.record(LogType.WHITELIST_CHANGE, pkg, "移除白名单")
    }

    fun changeAdminPw(pw: String) = viewModelScope.launch { app.auth.changeAdminPassword(pw) }
    fun changeNormalPw(pw: String) = viewModelScope.launch { app.auth.changeNormalPassword(pw) }
    fun clearLogs() = viewModelScope.launch { app.logs.clear() }
    suspend fun exportLogs(): String = app.logs.exportAsText()
}
```

- [ ] **Step 2: AdminActivity**

`AdminActivity.kt`:
```kotlin
package com.anzone.mdm.ui.admin

import android.content.Intent
import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.anzone.mdm.AnzoneApp
import com.anzone.mdm.data.Role
import com.anzone.mdm.data.db.LogType
import com.anzone.mdm.ui.kiosk.KioskActivity
import kotlinx.coroutines.launch

class AdminActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        val app = AnzoneApp.from(this)
        val vm = AdminViewModel(app)
        setContent {
            MaterialTheme {
                val firstRun by vm.firstRun.collectAsStateWithLifecycle()
                val authed by vm.authed.collectAsStateWithLifecycle()
                val loginError by vm.loginError.collectAsStateWithLifecycle()
                when {
                    firstRun -> SetupWizardScreen(onCreate = vm::createAccounts)
                    !authed -> LoginScreen("管理员登录", loginError, vm::login)
                    else -> AdminHome(vm, app)
                }
            }
        }
    }

    @Composable
    private fun AdminHome(vm: AdminViewModel, app: AnzoneApp) {
        val scope = rememberCoroutineScope()
        var tab by remember { mutableIntStateOf(0) }
        val whitelist by vm.whitelist.collectAsStateWithLifecycle()
        val logs by vm.logs.collectAsStateWithLifecycle()
        Scaffold(bottomBar = {
            NavigationBar {
                listOf("白名单", "日志", "设置").forEachIndexed { i, label ->
                    NavigationBarItem(selected = tab == i, onClick = { tab = i },
                        icon = {}, label = { Text(label) })
                }
            }
        }) { pad ->
            Surface(Modifier.padding(pad)) {
                when (tab) {
                    0 -> WhitelistScreen(whitelist, vm.installedApps(), vm::addApp) { vm.removeApp(it) }
                    1 -> LogsScreen(logs, onClear = vm::clearLogs, onExport = {
                        scope.launch { /* 导出文本可写入 Downloads，MVP 仅在 logcat 显示 */
                            android.util.Log.i("anzone-export", vm.exportLogs()) }
                    })
                    else -> SettingsScreen(
                        onChangeAdminPw = vm::changeAdminPw,
                        onChangeNormalPw = vm::changeNormalPw,
                        onSwitchToNormal = {
                            app.session.switchTo(Role.NORMAL)
                            scope.launch { app.logs.record(LogType.ROLE_SWITCH, null, "切换普通用户") }
                            startActivity(Intent(this@AdminActivity, KioskActivity::class.java))
                            finish()
                        },
                        onReleaseManagement = { pw ->
                            scope.launch {
                                if (app.auth.verifyAdmin(app.auth.adminUsername(), pw)) {
                                    app.policy.releaseManagement()
                                    app.logs.record(LogType.MANAGEMENT_EXIT, null, "解除管控")
                                    app.policy.clearDeviceOwner()
                                    finishAffinity()
                                }
                            }
                        }
                    )
                }
            }
        }
    }
}
```

- [ ] **Step 3: 编译验证 → Commit**

Run: `./gradlew :app:compileDebugKotlin`
Expected: BUILD SUCCESSFUL（KioskActivity 在 Task 13 创建，若此步因引用报错，先建 Task 13 的空 KioskActivity 占位）。
```bash
git add app/src/main/java/com/anzone/mdm/ui/admin/AdminActivity.kt app/src/main/java/com/anzone/mdm/ui/admin/AdminViewModel.kt
git commit -m "feat: add AdminActivity and AdminViewModel"
```

---

## Task 13: KioskActivity（Launcher + 应用网格 + 管理员入口 + LockTask）

**Files:**
- Create: `app/src/main/java/com/anzone/mdm/ui/kiosk/KioskActivity.kt`

- [ ] **Step 1: 实现**

`KioskActivity.kt`:
```kotlin
package com.anzone.mdm.ui.kiosk

import android.content.Intent
import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.grid.GridCells
import androidx.compose.foundation.lazy.grid.LazyVerticalGrid
import androidx.compose.foundation.lazy.grid.items
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.anzone.mdm.AnzoneApp
import com.anzone.mdm.data.db.LogType
import com.anzone.mdm.data.db.WhitelistApp
import com.anzone.mdm.ui.admin.AdminActivity
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.launch

class KioskActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        val app = AnzoneApp.from(this)
        // 施加管控（仅 Device Owner 生效）
        lifecycleScope_apply(app)
        setContent {
            MaterialTheme {
                val scope = rememberCoroutineScope()
                var apps by remember { mutableStateOf<List<WhitelistApp>>(emptyList()) }
                var showAdminGate by remember { mutableStateOf(false) }
                LaunchedEffect(Unit) {
                    app.whitelist.observeAll().collect { apps = it }
                }
                Scaffold(topBar = {
                    TopAppBar(title = { Text("anzone") }, actions = {
                        TextButton(onClick = { showAdminGate = true }) { Text("管理员") }
                    })
                }) { pad ->
                    LazyVerticalGrid(
                        columns = GridCells.Adaptive(96.dp),
                        modifier = Modifier.padding(pad).fillMaxSize().padding(16.dp)
                    ) {
                        items(apps, key = { it.packageName }) { a ->
                            Column(Modifier.padding(8.dp), horizontalAlignment = Alignment.CenterHorizontally) {
                                FilledTonalButton(onClick = { launch(app, a.packageName, a.appLabel) }) {
                                    Text(a.appLabel.take(2))
                                }
                                Text(a.appLabel, style = MaterialTheme.typography.labelSmall)
                            }
                        }
                    }
                }
                if (showAdminGate) {
                    // 进管理员需登录：直接跳 AdminActivity，由其登录屏校验
                    LaunchedEffect(Unit) {
                        startActivity(Intent(this@KioskActivity, AdminActivity::class.java))
                        showAdminGate = false
                    }
                }
            }
        }
    }

    private fun lifecycleScope_apply(app: AnzoneApp) {
        lifecycleScope.launch {
            val pkgs = app.whitelist.getAll().map { it.packageName }.toTypedArray()
            app.policy.applyManagement(pkgs)
            if (app.policy.isDeviceOwner()) startLockTask()
            app.logs.record(LogType.BOOT, null, "进入 Kiosk")
        }
    }

    private fun launch(app: AnzoneApp, pkg: String, label: String) {
        val intent = packageManager.getLaunchIntentForPackage(pkg)
        if (intent != null) {
            startActivity(intent)
            lifecycleScope.launch { app.logs.record(LogType.APP_LAUNCH, pkg, "启动 $label") }
        } else {
            lifecycleScope.launch {
                app.whitelist.remove(pkg)
                app.logs.record(LogType.WHITELIST_CHANGE, pkg, "应用已卸载，自动移除")
            }
        }
    }

    override fun onBackPressed() { /* 吞掉返回键，Kiosk 不允许退出 */ }
}
```

> 需 `import androidx.lifecycle.lifecycleScope` 与 `import androidx.compose.runtime.collectAsState`（已用 LaunchedEffect 手动 collect，无需后者）。补 `import androidx.lifecycle.lifecycleScope`。

- [ ] **Step 2: 编译验证 → Commit**

Run: `./gradlew :app:compileDebugKotlin`
```bash
git add app/src/main/java/com/anzone/mdm/ui/kiosk/KioskActivity.kt
git commit -m "feat: add KioskActivity launcher with app grid and admin entry"
```

---

## Task 14: AndroidManifest + device_admin.xml 完整接线

**Files:**
- Modify: `app/src/main/AndroidManifest.xml`
- Create: `app/src/main/res/xml/device_admin.xml`

- [ ] **Step 1: device_admin.xml**

`app/src/main/res/xml/device_admin.xml`:
```xml
<?xml version="1.0" encoding="utf-8"?>
<device-admin xmlns:android="http://schemas.android.com/apk/res/android">
    <uses-policies>
        <wipe-data />
        <force-lock />
        <disable-keyguard-features />
    </uses-policies>
</device-admin>
```

- [ ] **Step 2: 完整 Manifest**

`app/src/main/AndroidManifest.xml`:
```xml
<?xml version="1.0" encoding="utf-8"?>
<manifest xmlns:android="http://schemas.android.com/apk/res/android">

    <application
        android:name=".AnzoneApp"
        android:label="anzone"
        android:icon="@android:drawable/sym_def_app_icon"
        android:theme="@android:style/Theme.Material.Light.NoActionBar">

        <!-- Kiosk = 默认 Launcher -->
        <activity
            android:name=".ui.kiosk.KioskActivity"
            android:exported="true"
            android:launchMode="singleTask"
            android:stateNotNeeded="true">
            <intent-filter>
                <action android:name="android.intent.action.MAIN" />
                <category android:name="android.intent.category.HOME" />
                <category android:name="android.intent.category.DEFAULT" />
                <category android:name="android.intent.category.LAUNCHER" />
            </intent-filter>
        </activity>

        <!-- 管理员界面：无 launcher 图标，仅内部跳转 -->
        <activity
            android:name=".ui.admin.AdminActivity"
            android:exported="false"
            android:launchMode="singleTask" />

        <!-- Device Admin / Device Owner 接收器 -->
        <receiver
            android:name=".device.AnzoneDeviceAdminReceiver"
            android:exported="true"
            android:permission="android.permission.BIND_DEVICE_ADMIN">
            <meta-data
                android:name="android.app.device_admin"
                android:resource="@xml/device_admin" />
            <intent-filter>
                <action android:name="android.app.action.DEVICE_ADMIN_ENABLED" />
            </intent-filter>
        </receiver>
    </application>
</manifest>
```

- [ ] **Step 3: 构建 APK**

Run: `./gradlew :app:assembleDebug`
Expected: BUILD SUCCESSFUL，产出 `app/build/outputs/apk/debug/app-debug.apk`。

- [ ] **Step 4: Commit**

```bash
git add app/src/main/AndroidManifest.xml app/src/main/res/xml/device_admin.xml
git commit -m "feat: wire manifest as launcher and device admin"
```

---

## Task 15: Provisioning 脚本 + 部署文档

**Files:**
- Create: `anzone-mdm/scripts/provision.ps1`
- Create: `anzone-mdm/scripts/provision.sh`
- Create: `anzone-mdm/docs/PROVISIONING.md`

- [ ] **Step 1: PowerShell 脚本**

`scripts/provision.ps1`:
```powershell
# 用法: .\provision.ps1 -Apk path\to\app-debug.apk
param([Parameter(Mandatory=$true)][string]$Apk)
$pkg = "com.anzone.mdm"
$admin = "$pkg/.device.AnzoneDeviceAdminReceiver"
Write-Host "确认设备已出厂重置、未添加任何 Google 账号、已开启 USB 调试。"
adb install -r $Apk
if ($LASTEXITCODE -ne 0) { Write-Error "安装失败"; exit 1 }
adb shell dpm set-device-owner $admin
if ($LASTEXITCODE -ne 0) { Write-Error "设置 Device Owner 失败（设备上必须没有任何已有账号）"; exit 1 }
Write-Host "成功。重启设备后进入 Kiosk。"
```

- [ ] **Step 2: bash 脚本**

`scripts/provision.sh`:
```bash
#!/usr/bin/env bash
set -euo pipefail
APK="${1:?用法: ./provision.sh path/to/app-debug.apk}"
PKG="com.anzone.mdm"
ADMIN="$PKG/.device.AnzoneDeviceAdminReceiver"
echo "确认设备已出厂重置、未添加任何账号、已开启 USB 调试。"
adb install -r "$APK"
adb shell dpm set-device-owner "$ADMIN"
echo "成功。重启设备后进入 Kiosk。"
```

- [ ] **Step 3: 部署文档**

`docs/PROVISIONING.md`:
```markdown
# anzone Device Owner 部署指南

## 前置条件
- 设备已**出厂重置**，开机后**跳过/不添加任何 Google 或厂商账号**（有账号会导致 set-device-owner 失败）。
- 开发者选项 → 开启 **USB 调试**。
- 电脑安装 adb（platform-tools）。

## 步骤
1. 连接设备，`adb devices` 确认已授权。
2. Windows：`scripts\provision.ps1 -Apk app-debug.apk`
   macOS/Linux：`./scripts/provision.sh app-debug.apk`
3. 看到 "成功" 后重启设备。
4. 设备进入 anzone Kiosk。首次启动按向导创建管理员+普通用户账号。
5. 管理员配置白名单 → 切换普通用户。

## 解除
管理员登录 → 设置 → 解除管控 → 输入密码确认。之后设备恢复正常，需重新执行上面步骤才能再次管控。

## 已知限制
- 仅测主流原生 Android，OEM 激进保活未专项适配。
- 不防 fastboot 强刷。
- HarmonyOS Next (5.0+) 不支持。
```

- [ ] **Step 4: Commit**

```bash
git add anzone-mdm/scripts anzone-mdm/docs/PROVISIONING.md
git commit -m "docs: add provisioning scripts and deployment guide"
```

---

## Task 16: 手动验收清单（真机 Device Owner 环境）

**这些无法自动化，必须在真机/模拟器人工走查并记录结果。**

- [ ] 出厂重置设备 → 运行 provision 脚本 → `adb shell dumpsys device_policy` 确认 anzone 是 device owner
- [ ] 重启 → 自动进入 KioskActivity（不是系统 Launcher）
- [ ] 首次启动向导：创建管理员+普通用户两账号；杀掉重开不再要求创建
- [ ] 管理员登录：正确密码进入；错误密码报错并写 LOGIN_FAILED 日志
- [ ] 添加白名单 App → 切换普通用户 → Kiosk 网格只显示白名单 App
- [ ] 点白名单 App 能启动；写 APP_LAUNCH 日志
- [ ] 尝试装 APK（应用商店/文件管理器）→ 被系统拦截（DISALLOW_INSTALL_APPS）
- [ ] 设置 → 应用 → anzone：卸载按钮不可用（DISALLOW_UNINSTALL_APPS / Device Owner）
- [ ] Home 键无法回到系统桌面；Back 键被吞
- [ ] 重启 → 自动恢复普通用户 Kiosk（免密），写 BOOT 日志
- [ ] 管理员 → 设置 → 解除管控 → 二次密码确认 → 设备恢复正常，可正常装卸软件
- [ ] OEM 保活观察：锁屏/清后台后 Kiosk 是否能自恢复（记录实际表现，作为 v2 适配输入）

---

## 自查记录（写计划时执行）

**Spec 覆盖：**
- 角色状态机（spec §2）→ Task 5(AuthRepo) + 6(SessionState) + 9(向导) + 10(登录) + 12(编排) ✅
- 架构双 Activity（spec §3/4）→ Task 12 + 13 ✅
- 数据模型（spec §5）→ Task 2(Room) + 5(DataStore) ✅
- 关键流程 A–I（spec §6）→ provisioning(15) / 创建(9,12) / 启动(13) / 登录(12) / 切换(12) / 加白名单(11,12) / 防装(7,14) / 解除(11,12) / 重启(13) ✅
- 安全机制（spec §7）→ Task 7 PolicyManager 的 user restrictions ✅
- 错误处理（spec §8）→ 非 Device Owner（7 的 isDeviceOwner 守卫 + 16 验收）/ App 已卸载（13 的 launch 回退）/ 首次无账号（12 向导强制）✅
- 测试（spec §9）→ 单测 Task 1–6；手动验收 Task 16 ✅

**占位符扫描：** 无 TBD/TODO。日志导出 MVP 暂写 logcat（已注明），非占位。

**类型一致性：** `WhitelistRepository.add/remove/isAllowed`、`AuthRepository.verifyAdmin/verifyNormal/createAccounts/changeAdminPassword/changeNormalPassword`、`SessionState.switchTo`、`PolicyManager.applyManagement/releaseManagement/clearDeviceOwner/isDeviceOwner`、`LogType` 枚举值在各 Task 间一致。

**已知待执行注意：** Task 13 的 KioskActivity 需补 `import androidx.lifecycle.lifecycleScope`（已在该 Task 注明）。Task 12 编译若因引用 KioskActivity 报错，先建 Task 13 占位类。
```
