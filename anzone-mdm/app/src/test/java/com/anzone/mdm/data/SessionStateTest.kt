package com.anzone.mdm.data

import kotlinx.coroutines.test.runTest
import org.junit.Assert.assertEquals
import org.junit.Test

class SessionStateTest {
    @Test fun `default role is NORMAL`() {
        assertEquals(Role.NORMAL, SessionState().role.value)
    }

    @Test fun `switchTo updates role`() = runTest {
        val s = SessionState()
        s.switchTo(Role.ADMIN)
        assertEquals(Role.ADMIN, s.role.value)
        s.switchTo(Role.NORMAL)
        assertEquals(Role.NORMAL, s.role.value)
    }
}
