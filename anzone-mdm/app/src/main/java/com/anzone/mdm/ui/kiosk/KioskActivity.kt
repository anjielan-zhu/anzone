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
import androidx.lifecycle.lifecycleScope
import com.anzone.mdm.AnzoneApp
import com.anzone.mdm.data.db.LogType
import com.anzone.mdm.data.db.WhitelistApp
import com.anzone.mdm.ui.admin.AdminActivity
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
                LaunchedEffect(Unit) {
                    app.whitelist.observeAll().collect { apps = it }
                }
                Scaffold(topBar = {
                    TopAppBar(title = { Text("anzone") }, actions = {
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
                            Column(Modifier.padding(8.dp), horizontalAlignment = Alignment.CenterHorizontally) {
                                FilledTonalButton(onClick = { launchApp(app, a.packageName, a.appLabel) }) {
                                    Text(a.appLabel.take(2))
                                }
                                Text(a.appLabel, style = MaterialTheme.typography.labelSmall)
                            }
                        }
                    }
                }
            }
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
