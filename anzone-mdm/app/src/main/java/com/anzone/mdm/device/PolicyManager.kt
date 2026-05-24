package com.anzone.mdm.device

import android.app.admin.DevicePolicyManager
import android.content.Context
import android.os.UserManager

class PolicyManager(private val context: Context) {
    private val dpm =
        context.getSystemService(Context.DEVICE_POLICY_SERVICE) as DevicePolicyManager
    private val admin = AnzoneDeviceAdminReceiver.componentName(context)

    fun isDeviceOwner(): Boolean = dpm.isDeviceOwnerApp(context.packageName)

    /** Enter management: lock the runnable task set + apply user restrictions. */
    fun applyManagement(lockTaskPackages: Array<String>) {
        if (!isDeviceOwner()) return
        dpm.setLockTaskPackages(admin, lockTaskPackages + context.packageName)
        dpm.addUserRestriction(admin, UserManager.DISALLOW_INSTALL_APPS)
        dpm.addUserRestriction(admin, UserManager.DISALLOW_UNINSTALL_APPS)
        dpm.addUserRestriction(admin, UserManager.DISALLOW_INSTALL_UNKNOWN_SOURCES)
        dpm.addUserRestriction(admin, UserManager.DISALLOW_SAFE_BOOT)
        dpm.addUserRestriction(admin, UserManager.DISALLOW_FACTORY_RESET)
        dpm.addUserRestriction(admin, UserManager.DISALLOW_DEBUGGING_FEATURES)
    }

    /** Release management: remove restrictions, prepare to exit. */
    fun releaseManagement() {
        if (!isDeviceOwner()) return
        listOf(
            UserManager.DISALLOW_INSTALL_APPS,
            UserManager.DISALLOW_UNINSTALL_APPS,
            UserManager.DISALLOW_INSTALL_UNKNOWN_SOURCES,
            UserManager.DISALLOW_SAFE_BOOT,
            UserManager.DISALLOW_FACTORY_RESET,
            UserManager.DISALLOW_DEBUGGING_FEATURES,
        ).forEach { dpm.clearUserRestriction(admin, it) }
        dpm.setLockTaskPackages(admin, emptyArray())
    }

    /** Fully relinquish Device Owner (irreversible; needs re-provisioning). */
    fun clearDeviceOwner() {
        if (isDeviceOwner()) dpm.clearDeviceOwnerApp(context.packageName)
    }
}
