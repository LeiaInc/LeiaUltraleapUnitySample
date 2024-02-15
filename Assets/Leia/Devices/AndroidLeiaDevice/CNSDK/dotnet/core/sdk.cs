/*
 * Copyright 2024 (c) Leia Inc.  All rights reserved.
 *
 * NOTICE:  All information contained herein is, and remains
 * the property of Leia Inc. and its suppliers, if any.  The
 * intellectual and technical concepts contained herein are
 * proprietary to Leia Inc. and its suppliers and may be covered
 * by U.S. and Foreign Patents, patents in process, and are
 * protected by trade secret or copyright law.  Dissemination of
 * this information or reproduction of this materials strictly
 * forbidden unless prior written permission is obtained from
 * Leia Inc.
 */
#if UNITY_ANDROID
using System;
using System.Runtime.InteropServices;

namespace Leia
{
    public enum FaceTrackingRuntimeType
    {
        IN_SERVICE = 0,
        IN_APP     = 1,
    }
    class CoreLibrary : IDisposable
    {
        private static CoreLibrary instance = null;
        public static IntPtr Get()
        {
            if (instance == null)
            {
                instance = new CoreLibrary();
            }
            if (instance.GetHandle() == IntPtr.Zero)
            {
                throw new Exception("Failed to laod leia core library");
            }
            return instance.GetHandle();
        }
        private CoreLibrary()
        {
            _ptr = leiaSdkUnityLoadCoreLibrary();
        }
        public void Dispose()
        {
            leia_core_library_release(_ptr);
        }
        public IntPtr GetHandle()
        {
            return _ptr;
        }

        private IntPtr _ptr;

        [DllImport(Constants.SDK_DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr leiaSdkUnityLoadCoreLibrary();
        [DllImport(Constants.SDK_DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void leia_core_library_release(IntPtr ptr);
    }
    public class SDKConfig : IDisposable
    {
        public SDKConfig()
        {
            _ptr = leia_core_init_configuration_alloc(CoreLibrary.Get(), Version.VERSION);
        }
        public void SetLicenseKey(string licenseKey)
        {
            leia_core_init_configuration_set_license_key(_ptr, licenseKey);
        }
        public void SetLicenseNetworkProxy(string networkProxy)
        {
            leia_core_init_configuration_set_license_network_proxy(_ptr, networkProxy);
        }
        public void SetEnableValidation(bool enable)
        {
            leia_core_init_configuration_set_enable_validation(_ptr, Convert.ToInt32(enable));
        }
        public void SetPlatformLogLevel(LogLevel logLevel)
        {
            leia_core_init_configuration_set_platform_log_level(_ptr, logLevel);
        }
        public void SetFaceTrackingSharedCameraSink(SharedCameraSink sink)
        {
            IntPtr sinkNative = IntPtr.Zero;
            if (sink != null)
            {
                sinkNative = sink.ReleaseOwnership();
            }
            leia_core_init_configuration_set_face_tracking_shared_camera_sink(_ptr, sinkNative);
        }
        public void SetFaceTrackingRuntime(FaceTrackingRuntimeType runtime)
        {
            leia_core_init_configuration_set_face_tracking_runtime(_ptr, Convert.ToInt32(runtime));
        }
        public void SetFaceTrackingEnable(bool enable)
        {
            leia_core_init_configuration_set_face_tracking_enable(_ptr, Convert.ToInt32(enable));
        }
        public void SetFaceTrackingStart(bool start)
        {
            leia_core_init_configuration_set_face_tracking_start(_ptr, Convert.ToInt32(start));
        }
        public void SetFaceTrackingCheckPermission(bool checkPermission)
        {
            leia_core_init_configuration_set_face_tracking_check_permission(_ptr, Convert.ToInt32(checkPermission));
        }
        public void SetFaceTrackingPermissionDialogKillProcess(bool permissionDialogKillProcess)
        {
            leia_core_init_configuration_set_face_tracking_permission_dialog_kill_process(_ptr, Convert.ToInt32(permissionDialogKillProcess));
        }
        public void SetFaceTrackingServerLogLevel(LogLevel serverLogLevel)
        {
            leia_core_init_configuration_set_face_tracking_server_log_level(_ptr, serverLogLevel);
        }
        public void Dispose()
        {
            leia_core_init_configuration_free(_ptr);
        }
        public IntPtr GetHandle()
        {
            return _ptr;
        }

        private IntPtr _ptr;

        [DllImport(Constants.SDK_DLL_NAME, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr leia_core_init_configuration_alloc(IntPtr coreLibrary, [MarshalAs(UnmanagedType.LPStr)] string cnsdkVersion);
        [DllImport(Constants.SDK_DLL_NAME, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern void leia_core_init_configuration_set_hint(IntPtr ptr, [MarshalAs(UnmanagedType.LPStr)] string hint);
        [DllImport(Constants.SDK_DLL_NAME, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern void leia_core_init_configuration_set_license_key(IntPtr ptr, [MarshalAs(UnmanagedType.LPStr)] string licenseKey);
        [DllImport(Constants.SDK_DLL_NAME, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern void leia_core_init_configuration_set_license_network_proxy(IntPtr ptr, [MarshalAs(UnmanagedType.LPStr)] string networkProxy);
        [DllImport(Constants.SDK_DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void leia_core_init_configuration_set_enable_validation(IntPtr ptr, Int32 enable);
        [DllImport(Constants.SDK_DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void leia_core_init_configuration_set_platform_log_level(IntPtr ptr, LogLevel logLevel);
        [DllImport(Constants.SDK_DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void leia_core_init_configuration_set_face_tracking_shared_camera_sink(IntPtr ptr, IntPtr sharedCameraSink);
        [DllImport(Constants.SDK_DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void leia_core_init_configuration_set_face_tracking_runtime(IntPtr ptr, Int32 runtime);
        [DllImport(Constants.SDK_DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void leia_core_init_configuration_set_face_tracking_enable(IntPtr ptr, Int32 enable);
        [DllImport(Constants.SDK_DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void leia_core_init_configuration_set_face_tracking_start(IntPtr ptr, Int32 start);
        [DllImport(Constants.SDK_DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void leia_core_init_configuration_set_face_tracking_check_permission(IntPtr ptr, Int32 checkPermission);
        [DllImport(Constants.SDK_DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void leia_core_init_configuration_set_face_tracking_permission_dialog_kill_process(IntPtr ptr, Int32 permissionDialogKillProcess);
        [DllImport(Constants.SDK_DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void leia_core_init_configuration_set_face_tracking_server_log_level(IntPtr ptr, LogLevel serverLogLevel);
        [DllImport(Constants.SDK_DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void leia_core_init_configuration_free(IntPtr ptr);
    }
    public class SDK : IDisposable
    {
        public SDK(SDKConfig config)
        {
            _sdk = leia_core_init_async(config.GetHandle());
            if (_sdk == IntPtr.Zero)
            {
                throw new Exception("Failed to initialize CNSDK");
            }
        }
        public void Dispose()
        {
            if (_sdk != IntPtr.Zero)
            {
                leia_core_release(_sdk);
                _sdk = IntPtr.Zero;
            }
        }
        public bool IsInitialized()
        {
            return leia_core_is_initialized(_sdk) != 0;
        }
        public bool IsValidationEnabled()
        {
            return leia_core_is_validation_enabled(_sdk) != 0;
        }
        public class ConfigHolder : IDisposable
        {
            private SDK sdk;
            private IntPtr configPtr;
            public ConfigHolder(SDK sdk, IntPtr configPtr)
            {
                this.sdk = sdk;
                this.configPtr = configPtr;
            }
            public void Sync()
            {
                SDK.leia_core_sync_device_config(sdk.GetNativePtr(), configPtr);
            }
            public void Dispose()
            {
                SDK.leia_core_release_device_config(sdk.GetNativePtr(), configPtr);
            }
            private enum DeviceConfigProperty
            {
                PIXEL_SIZE_MM = 0,
                DISPLAY_SIZE_MM = 1,
                VIEW_RESOLUTION_PX = 2,
                PANEL_RESOLUTION_PX = 3,
                DEVICE_NATURAL_ORIENTATION = 6,
            }
            public bool GetPixelSizeMm(out Vector2i pixelSizeMm)
            {
                return GetVector2i(DeviceConfigProperty.PIXEL_SIZE_MM, out pixelSizeMm);
            }
            public bool GetDisplaySizeMm(out Vector2i displaySizeMm)
            {
                return GetVector2i(DeviceConfigProperty.DISPLAY_SIZE_MM, out displaySizeMm);
            }
            public bool GetViewResolutionPx(out Vector2i viewResolutionPx)
            {
                return GetVector2i(DeviceConfigProperty.VIEW_RESOLUTION_PX, out viewResolutionPx);
            }
            public bool GetPanelResolutionPx(out Vector2i panelResolutionPx)
            {
                return GetVector2i(DeviceConfigProperty.PANEL_RESOLUTION_PX, out panelResolutionPx);
            }
            public bool SetPanelResolutionPx(Vector2i panelResolutionPx)
            {
                return SetVector2i(DeviceConfigProperty.PANEL_RESOLUTION_PX, panelResolutionPx);
            }
            public Orientation GetDeviceNaturalOrientation()
            {
                Int32[] array = new Int32[1];
                if (leia_device_config_get_i32(configPtr, DeviceConfigProperty.DEVICE_NATURAL_ORIENTATION, 1, array) != 0)
                {
                    return (Orientation)array[0];
                }
                return Orientation.Unspecified;
            }
            private bool GetVector2i(DeviceConfigProperty property, out Vector2i value)
            {
                Int32[] array = new Int32[2];
                if (leia_device_config_get_i32(configPtr, property, 2, array) != 0)
                {
                    value.x = array[0];
                    value.y = array[1];
                    return true;
                }
                value.x = 0;
                value.y = 0;
                return false;
            }
            private bool SetVector2i(DeviceConfigProperty property, Vector2i value)
            {
                Int32[] array = new Int32[2];
                array[0] = value.x;
                array[1] = value.y;
                return leia_device_config_set_i32(configPtr, property, 2, array) != 0;
            }

            [DllImport(Constants.SDK_DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
            private static extern int leia_device_config_get_i32(IntPtr configPtr, DeviceConfigProperty property, Int32 arraySize, Int32[] value);
            [DllImport(Constants.SDK_DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
            private static extern int leia_device_config_set_i32(IntPtr configPtr, DeviceConfigProperty property, Int32 arraySize, Int32[] value);
        }
        public ConfigHolder GetConfig()
        {
            IntPtr configPtr = leia_core_get_device_config(_sdk);
            if (configPtr == IntPtr.Zero)
            {
                return null;
            }
            return new ConfigHolder(this, configPtr);
        }
        public static bool GetLegalOrientations(out LegalOrientations legalOrientations)
        {
            return leia_core_get_legal_orientations(CoreLibrary.Get(), out legalOrientations) != 0;
        }
        public void Enable3D(bool enable)
        {
            leia_core_enable_3d(_sdk, Convert.ToInt32(enable));
        }
        public bool Is3DEnabled()
        {
            Int32 isEnabledInt = leia_core_is_3d_enabled(_sdk);
            return isEnabledInt != 0;
        }
        [Obsolete("SetBacklight is deprecated, please use Enable3D instead.")]
        public void SetBacklight(bool enable)
        {
            Enable3D(enable);
        }
        [Obsolete("GetBacklight is deprecated, please use Is3DEnabled instead.")]
        public bool GetBacklight()
        {
            return Is3DEnabled();
        }
        public bool EnableFacetracking(bool enable)
        {
            return leia_core_enable_face_tracking(_sdk, Convert.ToInt32(enable)) != 0;
        }
        public void StartFacetracking(bool start)
        {
            leia_core_start_face_tracking(_sdk, Convert.ToInt32(start));
        }
        public void SetFaceTrackingProfiling(bool enable)
        {
            if (leia_core_set_face_tracking_profiling != null)
            {
                leia_core_set_face_tracking_profiling(_sdk, Convert.ToInt32(enable));
            }
        }
        public bool GetFaceTrackingProfiling(out Leia.HeadTracking.FrameProfiling frameProfiling)
        {
            if (leia_core_get_face_tracking_profiling == null)
            {
                frameProfiling = new Leia.HeadTracking.FrameProfiling();
                return false;
            }
            return leia_core_get_face_tracking_profiling(_sdk, out frameProfiling, Marshal.SizeOf<Leia.HeadTracking.FrameProfiling>()) != 0;
        }
        public void SetFaceTrackingSharedCameraSink(SharedCameraSink sink)
        {
            IntPtr sinkNative = IntPtr.Zero;
            if (sink != null)
            {
                sinkNative = sink.ReleaseOwnership();
            }
            leia_core_set_face_tracking_shared_camera_sink(_sdk, sinkNative);
        }
        public bool GetPrimaryFace(out Vector3 position)
        {
            return leia_core_get_primary_face(_sdk, leiaSdkUnityVector3ToSlice(out position)) != 0;
        }
        public bool GetNonPredictedPrimaryFace(out Vector3 position)
        {
            return leia_core_get_non_predicted_primary_face(_sdk, leiaSdkUnityVector3ToSlice(out position)) != 0;
        }
        public void Resume()
        {
            leia_core_on_resume(_sdk);
        }
        public void Pause()
        {
            leia_core_on_pause(_sdk);
        }
        public IntPtr GetNativePtr()
        {
            return _sdk;
        }
        public bool GetViewingDistance(float ipdMM, out float viewingDistance)
        {
            return leia_core_get_viewing_distance(_sdk, ipdMM, out viewingDistance) != 0;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public struct DataSlice
        {
            IntPtr data;
            Int32  length;
        }

        private IntPtr _sdk;

        [DllImport(Constants.SDK_DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr leia_core_init_async(IntPtr config);
        [DllImport(Constants.SDK_DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern Int32 leia_core_is_initialized(IntPtr sdk);
        [DllImport(Constants.SDK_DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern Int32 leia_core_is_validation_enabled(IntPtr sdk);
        [DllImport(Constants.SDK_DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern Int32 leia_core_enable_face_tracking(IntPtr sdk, Int32 enable);
        [DllImport(Constants.SDK_DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void leia_core_start_face_tracking(IntPtr sdk, Int32 start);
        [DllImport(Constants.SDK_DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void leia_core_set_face_tracking_shared_camera_sink(IntPtr sdk, IntPtr sink);
        [DllImport(Constants.SDK_DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern Int32 leia_core_get_primary_face(IntPtr sdk, DataSlice position);
        [DllImport(Constants.SDK_DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern Int32 leia_core_get_non_predicted_primary_face(IntPtr sdk, DataSlice position);
        [DllImport(Constants.SDK_DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void leia_core_enable_3d(IntPtr sdk, Int32 enable);
        [DllImport(Constants.SDK_DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern Int32 leia_core_is_3d_enabled(IntPtr sdk);
        [DllImport(Constants.SDK_DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr leia_core_get_device_config(IntPtr sdk);
        [DllImport(Constants.SDK_DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void leia_core_sync_device_config(IntPtr sdk, IntPtr config);
        [DllImport(Constants.SDK_DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void leia_core_release_device_config(IntPtr sdk, IntPtr config);
        [DllImport(Constants.SDK_DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void leia_core_on_resume(IntPtr sdk);
        [DllImport(Constants.SDK_DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void leia_core_on_pause(IntPtr sdk);
        [DllImport(Constants.SDK_DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void leia_core_release(IntPtr sdk);
        [DllImport(Constants.SDK_DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern Int32 leia_core_get_legal_orientations(IntPtr coreLibrary, out LegalOrientations legalOrientations);
        [DllImport(Constants.SDK_DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern Int32 leia_core_get_viewing_distance(IntPtr sdk, float ipdMM, out float viewingDistance);
        [DllImport(Constants.SDK_DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern DataSlice leiaSdkUnityVector3ToSlice(out Vector3 v);

        const int leia_core_set_face_tracking_profiling_VERSION = 1;
        delegate void leia_core_set_face_tracking_profiling_t(IntPtr sdk, Int32 enable);
        leia_core_set_face_tracking_profiling_t leia_core_set_face_tracking_profiling;
        const int leia_core_get_face_tracking_profiling_VERSION = 1;
        delegate Int32 leia_core_get_face_tracking_profiling_t(IntPtr sdk, out Leia.HeadTracking.FrameProfiling profiling, Int32 profilingSizeof);
        leia_core_get_face_tracking_profiling_t leia_core_get_face_tracking_profiling;
        void LoadExperimental()
        {
            IntPtr coreLibrary = CoreLibrary.Get();
            leia_core_set_face_tracking_profiling = Marshal.GetDelegateForFunctionPointer<leia_core_set_face_tracking_profiling_t>(Experimental.Get(coreLibrary, "leia_core_set_face_tracking_profiling", leia_core_set_face_tracking_profiling_VERSION));
            leia_core_get_face_tracking_profiling = Marshal.GetDelegateForFunctionPointer<leia_core_get_face_tracking_profiling_t>(Experimental.Get(coreLibrary, "leia_core_get_face_tracking_profiling", leia_core_get_face_tracking_profiling_VERSION));
        }
    }
    class Experimental
    {
        public static IntPtr Get(IntPtr coreLibrary, String name, int version)
        {
            return leia_get_experimental_api(coreLibrary, name, version);
        }
        [DllImport(Constants.SDK_DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern IntPtr leia_get_experimental_api(IntPtr coreLibrary, [MarshalAs(UnmanagedType.LPStr)] string name, Int32 version);
    }
}
#endif