package com.anzone.mdm.ui.admin

import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.unit.dp
import com.anzone.mdm.R

@Composable
fun SettingsScreen(
    releaseError: String?,
    currentLang: String,
    onLanguageChange: (String) -> Unit,
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
        Text(stringResource(R.string.language), style = MaterialTheme.typography.titleSmall)
        Row {
            listOf("system" to R.string.lang_system, "zh-Hans" to R.string.lang_hans, "zh-Hant" to R.string.lang_hant).forEach { (tag, label) ->
                val selected = currentLang == tag
                if (selected) Button(onClick = { onLanguageChange(tag) }) { Text(stringResource(label)) }
                else OutlinedButton(onClick = { onLanguageChange(tag) }) { Text(stringResource(label)) }
            }
        }
        Spacer(Modifier.height(16.dp))

        Text(stringResource(R.string.change_admin_pw), style = MaterialTheme.typography.titleSmall)
        OutlinedTextField(adminPw, { adminPw = it }, label = { Text(stringResource(R.string.new_password)) },
            singleLine = true, modifier = Modifier.fillMaxWidth())
        Button(onClick = { onChangeAdminPw(adminPw); adminPw = "" }) { Text(stringResource(R.string.update_admin_pw)) }
        Spacer(Modifier.height(16.dp))

        Text(stringResource(R.string.change_user_pw), style = MaterialTheme.typography.titleSmall)
        OutlinedTextField(normalPw, { normalPw = it }, label = { Text(stringResource(R.string.new_password)) },
            singleLine = true, modifier = Modifier.fillMaxWidth())
        Button(onClick = { onChangeNormalPw(normalPw); normalPw = "" }) { Text(stringResource(R.string.update_user_pw)) }
        Spacer(Modifier.height(24.dp))

        Button(onClick = onSwitchToNormal, modifier = Modifier.fillMaxWidth()) {
            Text(stringResource(R.string.switch_to_user))
        }
        Spacer(Modifier.height(8.dp))
        OutlinedButton(onClick = { showRelease = true }, modifier = Modifier.fillMaxWidth()) {
            Text(stringResource(R.string.release_mgmt))
        }
    }

    if (showRelease) {
        AlertDialog(
            onDismissRequest = { showRelease = false; confirmPw = ""; onReleaseDialogDismiss() },
            title = { Text(stringResource(R.string.confirm_release_title)) },
            text = {
                Column {
                    Text(stringResource(R.string.release_warning))
                    OutlinedTextField(confirmPw, { confirmPw = it },
                        label = { Text(stringResource(R.string.admin_password)) }, singleLine = true)
                    releaseError?.let { Text(it, color = MaterialTheme.colorScheme.error) }
                }
            },
            confirmButton = {
                TextButton(onClick = { onReleaseManagement(confirmPw) }) {
                    Text(stringResource(R.string.confirm_release))
                }
            },
            dismissButton = {
                TextButton(onClick = { showRelease = false; confirmPw = ""; onReleaseDialogDismiss() }) { Text(stringResource(R.string.cancel)) }
            }
        )
    }
}
