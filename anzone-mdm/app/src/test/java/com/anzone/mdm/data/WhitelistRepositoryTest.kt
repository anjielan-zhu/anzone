package com.anzone.mdm.data

import androidx.room.Room
import androidx.test.core.app.ApplicationProvider
import com.anzone.mdm.data.db.AnzoneDatabase
import kotlinx.coroutines.test.runTest
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner

@RunWith(RobolectricTestRunner::class)
class WhitelistRepositoryTest {
    private lateinit var db: AnzoneDatabase
    private lateinit var repo: WhitelistRepository

    @Before fun setup() {
        db = Room.inMemoryDatabaseBuilder(
            ApplicationProvider.getApplicationContext(), AnzoneDatabase::class.java
        ).allowMainThreadQueries().build()
        repo = WhitelistRepository(db.whitelistDao())
    }

    @After fun tearDown() = db.close()

    @Test fun `add then isAllowed true`() = runTest {
        repo.add("com.tencent.mm", "WeChat")
        assertTrue(repo.isAllowed("com.tencent.mm"))
    }

    @Test fun `not added isAllowed false`() = runTest {
        assertFalse(repo.isAllowed("com.evil.app"))
    }

    @Test fun `remove revokes`() = runTest {
        repo.add("com.a", "A")
        repo.remove("com.a")
        assertFalse(repo.isAllowed("com.a"))
    }

    @Test fun `getAll returns added`() = runTest {
        repo.add("com.a", "A")
        repo.add("com.b", "B")
        assertEquals(2, repo.getAll().size)
    }
}
