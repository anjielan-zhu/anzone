package com.anzone.mdm.ui.admin

import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.unit.dp
import com.anzone.mdm.R

@Composable
fun LoginScreen(title: String, error: String?, onSubmit: (String, String) -> Unit) {
    var user by remember { mutableStateOf("") }
    var pw by remember { mutableStateOf("") }
    Column(Modifier.fillMaxSize().padding(24.dp), verticalArrangement = Arrangement.Center) {
        Text(title, style = MaterialTheme.typography.headlineSmall)
        Spacer(Modifier.height(16.dp))
        OutlinedTextField(value = user, onValueChange = { user = it },
            label = { Text(stringResource(R.string.username)) }, singleLine = true, modifier = Modifier.fillMaxWidth())
        OutlinedTextField(value = pw, onValueChange = { pw = it },
            label = { Text(stringResource(R.string.password)) }, singleLine = true, modifier = Modifier.fillMaxWidth())
        error?.let { Text(it, color = MaterialTheme.colorScheme.error) }
        Spacer(Modifier.height(16.dp))
        Button(onClick = { onSubmit(user, pw) }, modifier = Modifier.fillMaxWidth()) {
            Text(stringResource(R.string.login))
        }
    }
}
