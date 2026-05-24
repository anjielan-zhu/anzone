package com.anzone.mdm

import android.app.Application
import android.content.Context
import androidx.datastore.core.DataStore
import androidx.datastore.preferences.core.Preferences
import androidx.datastore.preferences.preferencesDataStore
import androidx.room.Room
import com.anzone.mdm.data.AuthRepository
import com.anzone.mdm.data.LogRepository
import com.anzone.mdm.data.SessionState
import com.anzone.mdm.data.WhitelistRepository
import com.anzone.mdm.data.db.AnzoneDatabase
import com.anzone.mdm.device.PolicyManager

private val Context.authStore: DataStore<Preferences> by preferencesDataStore(name = "auth")

class AnzoneApp : Application() {
    lateinit var whitelist: WhitelistRepository; private set
    lateinit var logs: LogRepository; private set
    lateinit var auth: AuthRepository; private set
    lateinit var policy: PolicyManager; private set
    val session = SessionState()

    override fun onCreate() {
        super.onCreate()
        val db = Room.databaseBuilder(this, AnzoneDatabase::class.java, "anzone.db").build()
        whitelist = WhitelistRepository(db.whitelistDao())
        logs = LogRepository(db.logDao())
        auth = AuthRepository(authStore)
        policy = PolicyManager(this)
    }

    companion object {
        fun from(ctx: Context) = ctx.applicationContext as AnzoneApp
    }
}
