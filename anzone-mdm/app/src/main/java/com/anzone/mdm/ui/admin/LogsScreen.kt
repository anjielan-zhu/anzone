package com.anzone.mdm.ui.admin

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.unit.dp
import com.anzone.mdm.R
import com.anzone.mdm.data.db.LogEntry
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale

@Composable
fun LogsScreen(logs: List<LogEntry>, onClear: () -> Unit, onExport: () -> Unit) {
    val fmt = SimpleDateFormat("MM-dd HH:mm:ss", Locale.getDefault())
    Column(Modifier.fillMaxSize().padding(16.dp)) {
        Row {
            Text(stringResource(R.string.logs_title, logs.size), style = MaterialTheme.typography.titleMedium)
            Spacer(Modifier.weight(1f))
            TextButton(onClick = onExport) { Text(stringResource(R.string.export)) }
            TextButton(onClick = onClear) { Text(stringResource(R.string.clear)) }
        }
        LazyColumn(Modifier.weight(1f)) {
            items(logs, key = { it.id }) { e ->
                ListItem(
                    headlineContent = { Text("${e.type}  ${e.detail}") },
                    supportingContent = { Text("${fmt.format(Date(e.timestamp))}  ${e.packageName ?: ""}") }
                )
            }
        }
    }
}
