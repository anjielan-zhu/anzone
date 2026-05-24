package com.anzone.mdm.data

import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow

class SessionState {
    private val _role = MutableStateFlow(Role.NORMAL)
    val role: StateFlow<Role> = _role.asStateFlow()

    fun switchTo(role: Role) { _role.value = role }
}
