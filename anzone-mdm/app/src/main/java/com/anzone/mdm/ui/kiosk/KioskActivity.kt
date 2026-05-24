package com.anzone.mdm.ui.kiosk

import android.content.Intent
import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.compose.foundation.Image
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.grid.GridCells
import androidx.compose.foundation.lazy.grid.LazyVerticalGrid
import androidx.compose.foundation.lazy.grid.items
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.ImageBitmap
import androidx.compose.ui.graphics.asImageBitmap
import androidx.compose.ui.unit.dp
import androidx.core.graphics.drawable.toBitmap
import androidx.lifecycle.lifecycleScope
import com.anzone.mdm.AnzoneApp
import com.anzone.mdm.data.db.LogType
import com.anzone.mdm.data.db.WhitelistApp
import com.anzone.mdm.ui.admin.AdminActivity
import com.anzone.mdm.ui.admin.LoginScreen
import kotlinx.coroutines.launch

class KioskActivity : ComponentActivity() {
    @OptIn(ExperimentalMaterial3Api::class)
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        val app = AnzoneApp.from(this)
        applyManagement(app)
        setContent {
            MaterialTheme {
                var apps by remember { mutableStateOf<List<WhitelistApp>>(emptyList()) }
                var locked by remember { mutableStateOf(false) }
                var lockError by remember { mutableStateOf<String?>(null) }
                LaunchedEffect(Unit) {
                    app.whitelist.observeAll().collect { apps = it }
                }
                if (locked) {
                    LoginScreen(title = "Unlock (enter user password)", error = lockError, onSubmit = { user, pw ->
                        lifecycleScope.launch {
                            if (app.auth.verifyNormal(user, pw)) {
                                locked = false; lockError = null
                                app.logs.record(LogType.ROLE_SWITCH, null, "normal user unlocked")
                            } else {
                                lockError = "Wrong username or password"
                                app.logs.record(LogType.LOGIN_FAILED, null, "unlock failed")
                            }
                        }
                    })
                } else {
                    Scaffold(containerColor = androidx.compose.ui.graphics.Color.Transparent, topBar = {
                        TopAppBar(title = { Text("anzone") }, actions = {
                            TextButton(onClick = { locked = true }) { Text("Lock") }
                            TextButton(onClick = {
                                startActivity(Intent(this@KioskActivity, AdminActivity::class.java))
                            }) { Text("Admin") }
                        })
                    }) { pad ->
                        LazyVerticalGrid(
                            columns = GridCells.Adaptive(96.dp),
                            modifier = Modifier.padding(pad).fillMaxSize().padding(16.dp)
                        ) {
                            items(apps, key = { it.packageName }) { a ->
                                AppCell(a) { launchApp(app, a.packageName, a.appLabel) }
                            }
                        }
                    }
                }
            }
        }
    }

    @Composable
    private fun AppCell(app: WhitelistApp, onClick: () -> Unit) {
        val icon: ImageBitmap? = remember(app.packageName) {
            try { packageManager.getApplicationIcon(app.packageName).toBitmap(96, 96).asImageBitmap() }
            catch (e: Exception) { null }
        }
        Column(Modifier.padding(8.dp), horizontalAlignment = Alignment.CenterHorizontally) {
            if (icon != null) {
                IconButton(onClick = onClick, modifier = Modifier.size(64.dp)) {
                    Image(bitmap = icon, contentDescription = app.appLabel, modifier = Modifier.size(56.dp))
                }
            } else {
                FilledTonalButton(onClick = onClick) { Text(app.appLabel.take(2)) }
            }
            Text(app.appLabel, style = MaterialTheme.typography.labelSmall)
        }
    }

    private fun applyManagement(app: AnzoneApp) {
        lifecycleScope.launch {
            val pkgs = app.whitelist.getAll().map { it.packageName }.toTypedArray()
            app.policy.applyManagement(pkgs)
            if (app.policy.isDeviceOwner()) startLockTask()
            app.logs.record(LogType.BOOT, null, "enter kiosk")
        }
    }

    private fun launchApp(app: AnzoneApp, pkg: String, label: String) {
        val intent = packageManager.getLaunchIntentForPackage(pkg)
        if (intent != null) {
            startActivity(intent)
            lifecycleScope.launch { app.logs.record(LogType.APP_LAUNCH, pkg, "launch $label") }
        } else {
            lifecycleScope.launch {
                app.whitelist.remove(pkg)
                app.logs.record(LogType.WHITELIST_CHANGE, pkg, "app uninstalled, auto-removed")
            }
        }
    }

    @Deprecated("Kiosk swallows back", ReplaceWith(""))
    override fun onBackPressed() { /* swallow: Kiosk does not allow exit via back */ }
}
