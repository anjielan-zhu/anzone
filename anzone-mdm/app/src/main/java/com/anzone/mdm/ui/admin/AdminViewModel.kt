package com.anzone.mdm.ui.admin

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.anzone.mdm.AnzoneApp
import com.anzone.mdm.data.db.LogType
import kotlinx.coroutines.flow.SharingStarted
import kotlinx.coroutines.flow.stateIn
import kotlinx.coroutines.launch

class AdminViewModel(private val app: AnzoneApp) : ViewModel() {
    val whitelist = app.whitelist.observeAll()
        .stateIn(viewModelScope, SharingStarted.WhileSubscribed(5000), emptyList())
    val logs = app.logs.observeAll()
        .stateIn(viewModelScope, SharingStarted.WhileSubscribed(5000), emptyList())

    private val _firstRun = kotlinx.coroutines.flow.MutableStateFlow(true)
    val firstRun: kotlinx.coroutines.flow.StateFlow<Boolean> = _firstRun
    private val _authed = kotlinx.coroutines.flow.MutableStateFlow(false)
    val authed: kotlinx.coroutines.flow.StateFlow<Boolean> = _authed
    private val _loginError = kotlinx.coroutines.flow.MutableStateFlow<String?>(null)
    val loginError: kotlinx.coroutines.flow.StateFlow<String?> = _loginError

    init { viewModelScope.launch { _firstRun.value = app.auth.isFirstRun() } }

    fun installedApps(): List<InstalledApp> {
        val pm = app.packageManager
        val intent = android.content.Intent(android.content.Intent.ACTION_MAIN)
            .addCategory(android.content.Intent.CATEGORY_LAUNCHER)
        return pm.queryIntentActivities(intent, 0).map {
            InstalledApp(it.activityInfo.packageName, it.loadLabel(pm).toString())
        }.distinctBy { it.packageName }
    }

    fun createAccounts(au: String, ap: String, nu: String, np: String) = viewModelScope.launch {
        app.auth.createAccounts(au, ap, nu, np)
        _firstRun.value = false
        _authed.value = true
    }

    fun login(user: String, pw: String) = viewModelScope.launch {
        if (app.auth.verifyAdmin(user, pw)) {
            _authed.value = true; _loginError.value = null
            app.logs.record(LogType.ADMIN_LOGIN, null, "admin login")
        } else {
            _loginError.value = "Wrong username or password"
            app.logs.record(LogType.LOGIN_FAILED, null, "admin login failed")
        }
    }

    fun addApp(a: InstalledApp) = viewModelScope.launch {
        app.whitelist.add(a.packageName, a.label)
        app.logs.record(LogType.WHITELIST_CHANGE, a.packageName, "added ${a.label}")
    }

    fun removeApp(pkg: String) = viewModelScope.launch {
        app.whitelist.remove(pkg)
        app.logs.record(LogType.WHITELIST_CHANGE, pkg, "removed from whitelist")
    }

    fun changeAdminPw(pw: String) = viewModelScope.launch { app.auth.changeAdminPassword(pw) }
    fun changeNormalPw(pw: String) = viewModelScope.launch { app.auth.changeNormalPassword(pw) }
    fun clearLogs() = viewModelScope.launch { app.logs.clear() }
    suspend fun exportLogs(): String = app.logs.exportAsText()
}
