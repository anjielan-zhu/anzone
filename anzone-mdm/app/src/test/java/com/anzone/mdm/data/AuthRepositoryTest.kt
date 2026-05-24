package com.anzone.mdm.data

import androidx.datastore.core.DataStore
import androidx.datastore.preferences.core.PreferenceDataStoreFactory
import androidx.datastore.preferences.core.Preferences
import androidx.test.core.app.ApplicationProvider
import kotlinx.coroutines.test.runTest
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import java.io.File

@RunWith(RobolectricTestRunner::class)
class AuthRepositoryTest {
    private lateinit var repo: AuthRepository

    @Before fun setup() {
        val ctx = ApplicationProvider.getApplicationContext<android.content.Context>()
        val file = File(ctx.cacheDir, "auth_test_${System.nanoTime()}.preferences_pb")
        val store: DataStore<Preferences> = PreferenceDataStoreFactory.create { file }
        repo = AuthRepository(store)
    }

    @Test fun `first run true before setup`() = runTest {
        assertTrue(repo.isFirstRun())
    }

    @Test fun `after createAccounts first run false`() = runTest {
        repo.createAccounts("boss", "adminpw", "worker", "userpw")
        assertFalse(repo.isFirstRun())
    }

    @Test fun `admin login accepts correct credentials`() = runTest {
        repo.createAccounts("boss", "adminpw", "worker", "userpw")
        assertTrue(repo.verifyAdmin("boss", "adminpw"))
        assertFalse(repo.verifyAdmin("boss", "wrong"))
        assertFalse(repo.verifyAdmin("nobody", "adminpw"))
    }

    @Test fun `normal login accepts correct credentials`() = runTest {
        repo.createAccounts("boss", "adminpw", "worker", "userpw")
        assertTrue(repo.verifyNormal("worker", "userpw"))
        assertFalse(repo.verifyNormal("worker", "wrong"))
    }

    @Test fun `changeAdminPassword updates`() = runTest {
        repo.createAccounts("boss", "adminpw", "worker", "userpw")
        repo.changeAdminPassword("newpw")
        assertFalse(repo.verifyAdmin("boss", "adminpw"))
        assertTrue(repo.verifyAdmin("boss", "newpw"))
    }
}
