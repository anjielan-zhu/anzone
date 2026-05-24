package com.anzone.mdm.security

import java.security.MessageDigest
import java.security.SecureRandom
import javax.crypto.SecretKeyFactory
import javax.crypto.spec.PBEKeySpec

object PasswordHasher {
    private const val ITERATIONS = 100_000
    private const val KEY_LENGTH = 256
    private const val SALT_BYTES = 16

    fun generateSalt(): ByteArray =
        ByteArray(SALT_BYTES).also { SecureRandom().nextBytes(it) }

    fun hash(password: CharArray, salt: ByteArray): ByteArray {
        val spec = PBEKeySpec(password, salt, ITERATIONS, KEY_LENGTH)
        return SecretKeyFactory.getInstance("PBKDF2WithHmacSHA256")
            .generateSecret(spec).encoded
    }

    fun verify(password: CharArray, salt: ByteArray, expectedHash: ByteArray): Boolean =
        MessageDigest.isEqual(hash(password, salt), expectedHash)
}
