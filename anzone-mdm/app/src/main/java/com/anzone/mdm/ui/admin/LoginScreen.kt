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
            label = { Text("Username") }, singleLine = true, modifier = Modifier.fillMaxWidth())
        OutlinedTextField(value = pw, onValueChange = { pw = it },
            label = { Text("Password") }, singleLine = true, modifier = Modifier.fillMaxWidth())
        error?.let { Text(it, color = MaterialTheme.colorScheme.error) }
        Spacer(Modifier.height(16.dp))
        Button(onClick = { onSubmit(user, pw) }, modifier = Modifier.fillMaxWidth()) {
            Text("Login")
        }
    }
}
