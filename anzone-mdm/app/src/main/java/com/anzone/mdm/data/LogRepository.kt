package com.anzone.mdm.data

import com.anzone.mdm.data.db.LogDao
import com.anzone.mdm.data.db.LogEntry
import com.anzone.mdm.data.db.LogType
import kotlinx.coroutines.flow.Flow
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale

class LogRepository(private val dao: LogDao) {
    private val fmt = SimpleDateFormat("yyyy-MM-dd HH:mm:ss", Locale.getDefault())

    fun observeAll(): Flow<List<LogEntry>> = dao.observeAll()
    suspend fun getAll(): List<LogEntry> = dao.getAll()
    suspend fun clear() = dao.clear()

    suspend fun record(type: LogType, packageName: String?, detail: String) =
        dao.insert(LogEntry(timestamp = System.currentTimeMillis(), type = type,
            packageName = packageName, detail = detail))

    suspend fun exportAsText(): String = getAll().joinToString("\n") { e ->
        "${fmt.format(Date(e.timestamp))}\t${e.type}\t${e.packageName ?: "-"}\t${e.detail}"
    }
}
