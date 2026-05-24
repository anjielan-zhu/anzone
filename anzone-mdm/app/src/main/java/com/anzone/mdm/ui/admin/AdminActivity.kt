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
                    !authed -> LoginScreen("Admin login", loginError, vm::login)
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
                listOf("Whitelist", "Logs", "Settings").forEachIndexed { i, label ->
                    NavigationBarItem(selected = tab == i, onClick = { tab = i },
                        icon = {}, label = { Text(label) })
                }
            }
        }) { pad ->
            Surface(Modifier.padding(pad)) {
                when (tab) {
                    0 -> WhitelistScreen(whitelist, vm.installedApps(), vm::addApp) { vm.removeApp(it) }
                    1 -> LogsScreen(logs, onClear = vm::clearLogs, onExport = {
                        scope.launch { android.util.Log.i("anzone-export", vm.exportLogs()) }
                    })
                    else -> SettingsScreen(
                        onChangeAdminPw = vm::changeAdminPw,
                        onChangeNormalPw = vm::changeNormalPw,
                        onSwitchToNormal = {
                            app.session.switchTo(Role.NORMAL)
                            scope.launch { app.logs.record(LogType.ROLE_SWITCH, null, "switch to normal user") }
                            startActivity(Intent(this@AdminActivity, KioskActivity::class.java))
                            finish()
                        },
                        onReleaseManagement = { pw ->
                            scope.launch {
                                if (app.auth.verifyAdmin(app.auth.adminUsername(), pw)) {
                                    app.policy.releaseManagement()
                                    app.logs.record(LogType.MANAGEMENT_EXIT, null, "release management")
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
