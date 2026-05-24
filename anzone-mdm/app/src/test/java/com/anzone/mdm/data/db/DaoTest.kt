package com.anzone.mdm.data.db

import androidx.room.Room
import androidx.test.core.app.ApplicationProvider
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.test.runTest
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

@RunWith(RobolectricTestRunner::class)
@Config(sdk = [34])
class DaoTest {
    private lateinit var db: AnzoneDatabase
    private lateinit var whitelist: WhitelistDao
    private lateinit var logs: LogDao

    @Before fun setup() {
        db = Room.inMemoryDatabaseBuilder(
            ApplicationProvider.getApplicationContext(), AnzoneDatabase::class.java
        ).allowMainThreadQueries().build()
        whitelist = db.whitelistDao()
        logs = db.logDao()
    }

    @After fun tearDown() = db.close()

    @Test fun `upsert and contains`() = runTest {
        whitelist.upsert(WhitelistApp("com.a", "A", 1L))
        assertTrue(whitelist.contains("com.a"))
        assertFalse(whitelist.contains("com.b"))
    }

    @Test fun `delete removes entry`() = runTest {
        whitelist.upsert(WhitelistApp("com.a", "A", 1L))
        whitelist.delete("com.a")
        assertFalse(whitelist.contains("com.a"))
    }

    @Test fun `log insert and ordering desc`() = runTest {
        logs.insert(LogEntry(timestamp = 1, type = LogType.BOOT, packageName = null, detail = "x"))
        logs.insert(LogEntry(timestamp = 2, type = LogType.APP_LAUNCH, packageName = "com.a", detail = "y"))
        val all = logs.observeAll().first()
        assertEquals(2L, all.first().timestamp)
    }

    @Test fun `clear logs`() = runTest {
        logs.insert(LogEntry(timestamp = 1, type = LogType.BOOT, packageName = null, detail = "x"))
        logs.clear()
        assertTrue(logs.getAll().isEmpty())
    }
}
