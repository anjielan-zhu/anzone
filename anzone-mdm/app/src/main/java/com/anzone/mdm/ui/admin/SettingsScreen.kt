package com.anzone.mdm.ui.admin

import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp

@Composable
fun SettingsScreen(
    releaseError: String?,
    onChangeAdminPw: (String) -> Unit,
    onChangeNormalPw: (String) -> Unit,
    onSwitchToNormal: () -> Unit,
    onReleaseManagement: (String) -> Unit,
    onReleaseDialogDismiss: () -> Unit,
) {
    var adminPw by remember { mutableStateOf("") }
    var normalPw by remember { mutableStateOf("") }
    var confirmPw by remember { mutableStateOf("") }
    var showRelease by remember { mutableStateOf(false) }

    Column(Modifier.fillMaxSize().padding(16.dp)) {
        Text("Change admin password", style = MaterialTheme.typography.titleSmall)
        OutlinedTextField(adminPw, { adminPw = it }, label = { Text("New password") },
            singleLine = true, modifier = Modifier.fillMaxWidth())
        Button(onClick = { onChangeAdminPw(adminPw); adminPw = "" }) { Text("Update admin password") }
        Spacer(Modifier.height(16.dp))

        Text("Change normal-user password", style = MaterialTheme.typography.titleSmall)
        OutlinedTextField(normalPw, { normalPw = it }, label = { Text("New password") },
            singleLine = true, modifier = Modifier.fillMaxWidth())
        Button(onClick = { onChangeNormalPw(normalPw); normalPw = "" }) { Text("Update normal-user password") }
        Spacer(Modifier.height(24.dp))

        Button(onClick = onSwitchToNormal, modifier = Modifier.fillMaxWidth()) {
            Text("Switch to normal user (enter Kiosk)")
        }
        Spacer(Modifier.height(8.dp))
        OutlinedButton(onClick = { showRelease = true }, modifier = Modifier.fillMaxWidth()) {
            Text("Release management (exit)")
        }
    }

    if (showRelease) {
        AlertDialog(
            onDismissRequest = { showRelease = false; confirmPw = ""; onReleaseDialogDismiss() },
            title = { Text("Confirm release management") },
            text = {
                Column {
                    Text("After release the device returns to normal and apps can be freely installed/removed. Re-enabling management requires re-provisioning. Re-enter the admin password to confirm.")
                    OutlinedTextField(confirmPw, { confirmPw = it },
                        label = { Text("Admin password") }, singleLine = true)
                    releaseError?.let { Text(it, color = MaterialTheme.colorScheme.error) }
                }
            },
            confirmButton = {
                TextButton(onClick = { onReleaseManagement(confirmPw) }) {
                    Text("Confirm release")
                }
            },
            dismissButton = {
                TextButton(onClick = { showRelease = false; confirmPw = ""; onReleaseDialogDismiss() }) { Text("Cancel") }
            }
        )
    }
}
