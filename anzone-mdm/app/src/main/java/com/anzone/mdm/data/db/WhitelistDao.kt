package com.anzone.mdm.data.db

import androidx.room.Dao
import androidx.room.Insert
import androidx.room.OnConflictStrategy
import androidx.room.Query
import kotlinx.coroutines.flow.Flow

@Dao
interface WhitelistDao {
    @Query("SELECT * FROM whitelist ORDER BY appLabel")
    fun observeAll(): Flow<List<WhitelistApp>>

    @Query("SELECT * FROM whitelist")
    suspend fun getAll(): List<WhitelistApp>

    @Query("SELECT EXISTS(SELECT 1 FROM whitelist WHERE packageName = :pkg)")
    suspend fun contains(pkg: String): Boolean

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun upsert(app: WhitelistApp)

    @Query("DELETE FROM whitelist WHERE packageName = :pkg")
    suspend fun delete(pkg: String)
}
