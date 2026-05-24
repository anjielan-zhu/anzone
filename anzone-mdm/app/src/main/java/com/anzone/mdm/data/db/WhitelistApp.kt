package com.anzone.mdm.data.db

import androidx.room.Entity
import androidx.room.PrimaryKey

@Entity(tableName = "whitelist")
data class WhitelistApp(
    @PrimaryKey val packageName: String,
    val appLabel: String,
    val addedAt: Long,
)
