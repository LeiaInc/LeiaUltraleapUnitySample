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
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct ImageDesc
    {
        public IntPtr data;
        public Int32 width;
        public Int32 height;
        public Int32 rotation;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct CameraIntrinsics
    {
        public Int32    width;
        public Int32    height;
        public float    ppx;
        public float    ppy;
        public float    fx;
        public float    fy;
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 8)]
        public double[] distortionCoeffs;
        public Int32    isMirrored;
    }
    public class SharedCameraSink : IDisposable
    {
        public SharedCameraSink()
        {
            _ptr = leia_shared_camera_sink_alloc(CoreLibrary.Get());
            _isPtrOwned = true;
        }
        public void Dispose()
        {
            if (_isPtrOwned && _ptr != IntPtr.Zero)
            {
                leia_shared_camera_sink_release(_ptr);
                _ptr = IntPtr.Zero;
                _isPtrOwned = false;
            }
        }
        public bool IsValid()
        {
            return leia_shared_camera_sink_is_valid(_ptr) != 0;
        }
        public void OnImage(in ImageDesc image, Timestamp timestamp)
        {
            leia_shared_camera_sink_on_image(_ptr, in image, timestamp);
        }
        public void OnIntrinsicsChange(in CameraIntrinsics intrinsics)
        {
            leia_shared_camera_sink_on_intrinsics_change(_ptr, in intrinsics);
        }
        public IntPtr ReleaseOwnership()
        {
            if (_isPtrOwned)
            {
                _isPtrOwned = false;
                return _ptr;
            }
            return IntPtr.Zero;
        }
        private bool _isPtrOwned;
        private IntPtr _ptr;
        [DllImport(Constants.SDK_DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr leia_shared_camera_sink_alloc(IntPtr coreLibrary);
        [DllImport(Constants.SDK_DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void leia_shared_camera_sink_release(IntPtr ptr);
        [DllImport(Constants.SDK_DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern Int32 leia_shared_camera_sink_is_valid(IntPtr ptr);
        [DllImport(Constants.SDK_DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void leia_shared_camera_sink_on_image(IntPtr ptr, in ImageDesc imageDesc, Timestamp timestamp);
        [DllImport(Constants.SDK_DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void leia_shared_camera_sink_on_intrinsics_change(IntPtr ptr, in CameraIntrinsics intrinsics);
    }
}
#endif