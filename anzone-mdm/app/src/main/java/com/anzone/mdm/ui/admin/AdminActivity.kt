package com.anzone.mdm.ui.admin

import android.content.Intent
import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.res.stringResource
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.anzone.mdm.R
import com.anzone.mdm.AnzoneApp
import com.anzone.mdm.data.Role
import com.anzone.mdm.data.db.LogType
import com.anzone.mdm.ui.kiosk.KioskActivity
import kotlinx.coroutines.launch

class AdminActivity : ComponentActivity() {
    override fun attachBaseContext(newBase: android.content.Context) {
        super.attachBaseContext(com.anzone.mdm.util.LocaleManager.wrap(newBase))
    }

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
                    !authed -> LoginScreen(
                        stringResource(R.string.admin_login_title),
                        loginError?.let { stringResource(R.string.err_bad_credentials) },
                        vm::login,
                    )
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
        val installed = remember { vm.installedApps() }
        val releaseError by vm.releaseError.collectAsStateWithLifecycle()
        Scaffold(bottomBar = {
            NavigationBar {
                listOf(
                    stringResource(R.string.tab_whitelist),
                    stringResource(R.string.tab_logs),
                    stringResource(R.string.tab_settings),
                ).forEachIndexed { i, label ->
                    NavigationBarItem(selected = tab == i, onClick = { tab = i },
                        icon = {}, label = { Text(label) })
                }
            }
        }) { pad ->
            Surface(Modifier.padding(pad)) {
                when (tab) {
                    0 -> WhitelistScreen(whitelist, installed, vm::addApp) { vm.removeApp(it) }
                    1 -> LogsScreen(logs, onClear = vm::clearLogs, onExport = {
                        scope.launch { android.util.Log.i("anzone-export", vm.exportLogs()) }
                    })
                    else -> {
                        val localizedReleaseError = releaseError?.let { stringResource(R.string.err_wrong_password) }
                        SettingsScreen(
                        releaseError = localizedReleaseError,
                        currentLang = com.anzone.mdm.util.LocaleManager.getLang(this@AdminActivity),
                        onLanguageChange = { lang ->
                            com.anzone.mdm.util.LocaleManager.setLang(this@AdminActivity, lang)
                            recreate()
                        },
                        onChangeAdminPw = vm::changeAdminPw,
                        onChangeNormalPw = vm::changeNormalPw,
                        onSwitchToNormal = {
                            app.session.switchTo(Role.NORMAL)
                            scope.launch { app.logs.record(LogType.ROLE_SWITCH, null, "switch to normal user") }
                            startActivity(Intent(this@AdminActivity, KioskActivity::class.java))
                            finish()
                        },
                        onReleaseManagement = { pw -> vm.attemptRelease(pw) {
                            runCatching { stopLockTask() }
                            runCatching {
                                startActivity(Intent(Intent.ACTION_MAIN).apply {
                                    addCategory(Intent.CATEGORY_HOME)
                                    flags = Intent.FLAG_ACTIVITY_NEW_TASK
                                })
                            }
                            finishAffinity()
                        } },
                        onReleaseDialogDismiss = vm::clearReleaseError,
                        )
                    }
                }
            }
        }
    }
}
