package com.anzone.mdm.device

import android.app.admin.DeviceAdminReceiver
import android.content.ComponentName
import android.content.Context

class AnzoneDeviceAdminReceiver : DeviceAdminReceiver() {
    companion object {
        fun componentName(ctx: Context) =
            ComponentName(ctx, AnzoneDeviceAdminReceiver::class.java)
    }
}
