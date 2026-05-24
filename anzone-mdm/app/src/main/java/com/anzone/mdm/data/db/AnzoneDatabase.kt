package com.anzone.mdm.data.db

import androidx.room.Database
import androidx.room.RoomDatabase
import androidx.room.TypeConverter
import androidx.room.TypeConverters

class LogTypeConverter {
    @TypeConverter fun toName(t: LogType): String = t.name
    @TypeConverter fun fromName(s: String): LogType = LogType.valueOf(s)
}

@Database(entities = [WhitelistApp::class, LogEntry::class], version = 1, exportSchema = false)
@TypeConverters(LogTypeConverter::class)
abstract class AnzoneDatabase : RoomDatabase() {
    abstract fun whitelistDao(): WhitelistDao
    abstract fun logDao(): LogDao
}
