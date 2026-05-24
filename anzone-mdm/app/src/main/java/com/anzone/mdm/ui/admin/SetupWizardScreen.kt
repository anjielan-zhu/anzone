package com.anzone.mdm.ui.admin

import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.unit.dp
import com.anzone.mdm.R

@Composable
fun SetupWizardScreen(onCreate: (String, String, String, String) -> Unit) {
    var step by remember { mutableIntStateOf(0) }
    var adminUser by remember { mutableStateOf("") }
    var adminPw by remember { mutableStateOf("") }
    var normalUser by remember { mutableStateOf("") }
    var normalPw by remember { mutableStateOf("") }
    var pwConfirm by remember { mutableStateOf("") }
    var error by remember { mutableStateOf<String?>(null) }
    val errEmpty = stringResource(R.string.err_empty)
    val errMismatch = stringResource(R.string.err_pw_mismatch)

    Column(Modifier.fillMaxSize().padding(24.dp), verticalArrangement = Arrangement.Center) {
        Text(if (step == 0) stringResource(R.string.setup_admin_title) else stringResource(R.string.setup_user_title),
            style = MaterialTheme.typography.headlineSmall)
        Spacer(Modifier.height(16.dp))
        val user = if (step == 0) adminUser else normalUser
        val pw = if (step == 0) adminPw else normalPw
        OutlinedTextField(value = user, onValueChange = {
            if (step == 0) adminUser = it else normalUser = it
        }, label = { Text(stringResource(R.string.username)) }, singleLine = true, modifier = Modifier.fillMaxWidth())
        OutlinedTextField(value = pw, onValueChange = {
            if (step == 0) adminPw = it else normalPw = it
        }, label = { Text(stringResource(R.string.password)) }, singleLine = true, modifier = Modifier.fillMaxWidth())
        OutlinedTextField(value = pwConfirm, onValueChange = { pwConfirm = it },
            label = { Text(stringResource(R.string.confirm_password)) }, singleLine = true, modifier = Modifier.fillMaxWidth())
        error?.let { Text(it, color = MaterialTheme.colorScheme.error) }
        Spacer(Modifier.height(16.dp))
        Button(onClick = {
            when {
                user.isBlank() || pw.isBlank() -> error = errEmpty
                pw != pwConfirm -> error = errMismatch
                step == 0 -> { error = null; pwConfirm = ""; step = 1 }
                else -> onCreate(adminUser, adminPw, normalUser, normalPw)
            }
        }, modifier = Modifier.fillMaxWidth()) {
            Text(if (step == 0) stringResource(R.string.next) else stringResource(R.string.finish))
        }
    }
}
