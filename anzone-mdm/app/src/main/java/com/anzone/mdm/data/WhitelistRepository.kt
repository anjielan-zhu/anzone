package com.anzone.mdm.data

import com.anzone.mdm.data.db.WhitelistApp
import com.anzone.mdm.data.db.WhitelistDao
import kotlinx.coroutines.flow.Flow

class WhitelistRepository(private val dao: WhitelistDao) {
    fun observeAll(): Flow<List<WhitelistApp>> = dao.observeAll()
    suspend fun getAll(): List<WhitelistApp> = dao.getAll()
    suspend fun isAllowed(pkg: String): Boolean = dao.contains(pkg)
    suspend fun add(pkg: String, label: String) =
        dao.upsert(WhitelistApp(pkg, label, System.currentTimeMillis()))
    suspend fun remove(pkg: String) = dao.delete(pkg)
}
