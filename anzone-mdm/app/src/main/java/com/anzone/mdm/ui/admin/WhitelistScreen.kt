package com.anzone.mdm.ui.admin

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.unit.dp
import com.anzone.mdm.R
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
        Row(verticalAlignment = Alignment.CenterVertically) {
            Text(stringResource(R.string.whitelist_title, current.size), style = MaterialTheme.typography.titleMedium)
            Spacer(Modifier.weight(1f))
            Button(onClick = { picking = true }) { Text(stringResource(R.string.add)) }
        }
        LazyColumn(Modifier.weight(1f)) {
            items(current, key = { it.packageName }) { app ->
                ListItem(
                    headlineContent = { Text(app.appLabel) },
                    supportingContent = { Text(app.packageName) },
                    trailingContent = {
                        TextButton(onClick = { onRemove(app.packageName) }) { Text(stringResource(R.string.remove)) }
                    }
                )
            }
        }
    }
    if (picking) {
        AlertDialog(
            onDismissRequest = { picking = false },
            confirmButton = { TextButton(onClick = { picking = false }) { Text(stringResource(R.string.close)) } },
            title = { Text(stringResource(R.string.select_installed_app)) },
            text = {
                LazyColumn(Modifier.heightIn(max = 400.dp)) {
                    items(installed, key = { it.packageName }) { app ->
                        ListItem(
                            headlineContent = { Text(app.label) },
                            supportingContent = { Text(app.packageName) },
                            trailingContent = {
                                TextButton(onClick = { onAdd(app); picking = false }) { Text(stringResource(R.string.add)) }
                            }
                        )
                    }
                }
            }
        )
    }
}
