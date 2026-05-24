package com.anzone.mdm.data

import android.util.Base64
import androidx.datastore.core.DataStore
import androidx.datastore.preferences.core.Preferences
import androidx.datastore.preferences.core.booleanPreferencesKey
import androidx.datastore.preferences.core.edit
import androidx.datastore.preferences.core.stringPreferencesKey
import com.anzone.mdm.security.PasswordHasher
import kotlinx.coroutines.flow.first

enum class Role { ADMIN, NORMAL }

class AuthRepository(private val store: DataStore<Preferences>) {
    private object Keys {
        val FIRST_RUN = booleanPreferencesKey("is_first_run")
        val ADMIN_USER = stringPreferencesKey("admin_username")
        val ADMIN_HASH = stringPreferencesKey("admin_password_hash")
        val ADMIN_SALT = stringPreferencesKey("admin_password_salt")
        val NORMAL_USER = stringPreferencesKey("normal_username")
        val NORMAL_HASH = stringPreferencesKey("normal_password_hash")
        val NORMAL_SALT = stringPreferencesKey("normal_password_salt")
    }

    private fun b64(b: ByteArray) = Base64.encodeToString(b, Base64.NO_WRAP)
    private fun unb64(s: String) = Base64.decode(s, Base64.NO_WRAP)

    suspend fun isFirstRun(): Boolean =
        store.data.first()[Keys.FIRST_RUN] ?: true

    suspend fun createAccounts(
        adminUser: String, adminPw: String, normalUser: String, normalPw: String
    ) {
        val aSalt = PasswordHasher.generateSalt()
        val nSalt = PasswordHasher.generateSalt()
        val aHash = PasswordHasher.hash(adminPw.toCharArray(), aSalt)
        val nHash = PasswordHasher.hash(normalPw.toCharArray(), nSalt)
        store.edit {
            it[Keys.ADMIN_USER] = adminUser
            it[Keys.ADMIN_SALT] = b64(aSalt)
            it[Keys.ADMIN_HASH] = b64(aHash)
            it[Keys.NORMAL_USER] = normalUser
            it[Keys.NORMAL_SALT] = b64(nSalt)
            it[Keys.NORMAL_HASH] = b64(nHash)
            it[Keys.FIRST_RUN] = false
        }
    }

    suspend fun adminUsername(): String = store.data.first()[Keys.ADMIN_USER] ?: ""
    suspend fun normalUsername(): String = store.data.first()[Keys.NORMAL_USER] ?: ""

    suspend fun verifyAdmin(user: String, pw: String): Boolean =
        verify(user, pw, Keys.ADMIN_USER, Keys.ADMIN_SALT, Keys.ADMIN_HASH)

    suspend fun verifyNormal(user: String, pw: String): Boolean =
        verify(user, pw, Keys.NORMAL_USER, Keys.NORMAL_SALT, Keys.NORMAL_HASH)

    private suspend fun verify(
        user: String, pw: String,
        userKey: Preferences.Key<String>,
        saltKey: Preferences.Key<String>,
        hashKey: Preferences.Key<String>,
    ): Boolean {
        val prefs = store.data.first()
        if (prefs[userKey] != user) return false
        val salt = prefs[saltKey]?.let(::unb64) ?: return false
        val hash = prefs[hashKey]?.let(::unb64) ?: return false
        return PasswordHasher.verify(pw.toCharArray(), salt, hash)
    }

    suspend fun changeAdminPassword(newPw: String) =
        changePassword(newPw, Keys.ADMIN_SALT, Keys.ADMIN_HASH)

    suspend fun changeNormalPassword(newPw: String) =
        changePassword(newPw, Keys.NORMAL_SALT, Keys.NORMAL_HASH)

    private suspend fun changePassword(
        newPw: String, saltKey: Preferences.Key<String>, hashKey: Preferences.Key<String>,
    ) {
        val salt = PasswordHasher.generateSalt()
        val hash = PasswordHasher.hash(newPw.toCharArray(), salt)
        store.edit { it[saltKey] = b64(salt); it[hashKey] = b64(hash) }
    }
}
