package com.anzone.mdm.security

import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test

class PasswordHasherTest {
    @Test fun `same password and salt produce same hash`() {
        val salt = PasswordHasher.generateSalt()
        val h1 = PasswordHasher.hash("secret123".toCharArray(), salt)
        val h2 = PasswordHasher.hash("secret123".toCharArray(), salt)
        assertTrue(h1.contentEquals(h2))
    }

    @Test fun `different salt produces different hash`() {
        val h1 = PasswordHasher.hash("secret123".toCharArray(), PasswordHasher.generateSalt())
        val h2 = PasswordHasher.hash("secret123".toCharArray(), PasswordHasher.generateSalt())
        assertFalse(h1.contentEquals(h2))
    }

    @Test fun `verify accepts correct password`() {
        val salt = PasswordHasher.generateSalt()
        val hash = PasswordHasher.hash("correct".toCharArray(), salt)
        assertTrue(PasswordHasher.verify("correct".toCharArray(), salt, hash))
    }

    @Test fun `verify rejects wrong password`() {
        val salt = PasswordHasher.generateSalt()
        val hash = PasswordHasher.hash("correct".toCharArray(), salt)
        assertFalse(PasswordHasher.verify("wrong".toCharArray(), salt, hash))
    }
}
