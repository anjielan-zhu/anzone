package com.anzone.mdm.data

import androidx.room.Room
import androidx.test.core.app.ApplicationProvider
import com.anzone.mdm.data.db.AnzoneDatabase
import com.anzone.mdm.data.db.LogType
import kotlinx.coroutines.test.runTest
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner

@RunWith(RobolectricTestRunner::class)
class LogRepositoryTest {
    private lateinit var db: AnzoneDatabase
    private lateinit var repo: LogRepository

    @Before fun setup() {
        db = Room.inMemoryDatabaseBuilder(
            ApplicationProvider.getApplicationContext(), AnzoneDatabase::class.java
        ).allowMainThreadQueries().build()
        repo = LogRepository(db.logDao())
    }

    @After fun tearDown() = db.close()

    @Test fun `record persists entry`() = runTest {
        repo.record(LogType.APP_LAUNCH, "com.a", "launch A")
        val all = repo.getAll()
        assertEquals(1, all.size)
        assertEquals(LogType.APP_LAUNCH, all.first().type)
    }

    @Test fun `clear empties`() = runTest {
        repo.record(LogType.BOOT, null, "boot")
        repo.clear()
        assertTrue(repo.getAll().isEmpty())
    }

    @Test fun `export produces text with type and detail`() = runTest {
        repo.record(LogType.ADMIN_LOGIN, null, "admin login")
        val text = repo.exportAsText()
        assertTrue(text.contains("ADMIN_LOGIN"))
        assertTrue(text.contains("admin login"))
    }
}
