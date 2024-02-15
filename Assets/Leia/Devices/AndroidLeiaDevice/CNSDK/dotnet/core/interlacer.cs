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

using UnityEngine;

namespace Leia
{
    public class Interlacer
    {
        private const int kRenderCallbackEvent_Init = 1;
        private const int kRenderCallbackEvent_Draw = 2;

        private IntPtr _renderCallback;
        public Interlacer(SDK sdk)
        {
            int status = leiaSdkUnityRenderingPluginInitialize(sdk.GetNativePtr());
            if (status != 0)
            {
                throw new Exception("Failed to initialize: " + status);
            }

            _renderCallback = leiaSdkUnityRenderingPluginGetRenderEventFunc();
            if (_renderCallback == null)
            {
                throw new Exception("No render callback");
            }

            IssuePluginEvent(kRenderCallbackEvent_Init);
        }
        public void Dispose(SDK sdk)
        {
            leiaSdkUnityRenderingPluginRelease(sdk.GetNativePtr());
        }
        public bool SetInputViews(Texture[] viewTextures, int layer)
        {
            IntPtr[] nativePtrs = Array.ConvertAll(viewTextures, texture => texture.GetNativeTexturePtr());
            int error = leiaSdkUnityRenderingPluginSetInputViews(viewTextures.Length, nativePtrs, layer);
            return error == 0;
        }
        public void SetOutput(RenderTexture output)
        {
            leiaSdkUnityRenderingPluginSetOutput(output.GetNativeTexturePtr(), output.width, output.height);
        }
        public void SetLayerCount(int layerCount)
        {
            leiaSdkUnityRenderingPluginSetLayerCount(layerCount);
        }
        public int GetLayerCount()
        {
            return leiaSdkUnityRenderingPluginGetLayerCount();
        }
        public void Render()
        {
            IssuePluginEvent(kRenderCallbackEvent_Draw);
        }
        private void IssuePluginEvent(int eventType)
        {
            GL.IssuePluginEvent(_renderCallback, eventType);
        }
        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public struct Config
        {
            public bool singleViewMode;
            public bool outputAsTiles;
            public bool showGui;
        }
        public Config GetConfig()
        {
            Config config;
            leiaSdkUnityRenderingPluginGetConfig(out config);
            return config;
        }
        public void SetConfig(in Config config)
        {
            leiaSdkUnityRenderingPluginSetConfig(in config);
        }
#if UNITY_ANDROID
        /// \p androidMotionEvent a jobject of android.view.MotionEvent class.
        /// \returns true if the event was handled, false otherwise.
        public bool ProcessGuiInput(IntPtr androidMotionEvent)
        {
            return leiaSdkUnityRenderingPluginProcessGuiInput_MotionEvent(androidMotionEvent) != 0;
        }
#endif

        public struct Properties
        {
            public const string Baseline = @"baseline";
            public const string ConvergenceDistance = @"convergence";
        }
        public bool GetProperty(string propertyName, out float value)
        {
            return leiaSdkUnityRenderingPluginGetFloatProperty(propertyName, out value) == 1;
        }
        public bool SetProperty(string propertyName, float value)
        {
            return leiaSdkUnityRenderingPluginSetFloatProperty(propertyName, value) == 1;
        }

        [DllImport(Constants.SDK_DLL_NAME)]
        private static extern int leiaSdkUnityRenderingPluginInitialize(IntPtr sdk);
        [DllImport(Constants.SDK_DLL_NAME)]
        private static extern int leiaSdkUnityRenderingPluginSetInputViews(int numViews, IntPtr[] nativePtrs, int layer);
        [DllImport(Constants.SDK_DLL_NAME)]
        private static extern void leiaSdkUnityRenderingPluginSetOutput(IntPtr nativePtr, int width, int height);
        [DllImport(Constants.SDK_DLL_NAME)]
        private static extern void leiaSdkUnityRenderingPluginGetConfig(out Config config);
        [DllImport(Constants.SDK_DLL_NAME)]
        private static extern void leiaSdkUnityRenderingPluginSetConfig(in Config config);
        [DllImport(Constants.SDK_DLL_NAME)]
        private static extern IntPtr leiaSdkUnityRenderingPluginGetRenderEventFunc();
        [DllImport(Constants.SDK_DLL_NAME)]
        private static extern void leiaSdkUnityRenderingPluginRelease(IntPtr sdk);
        [DllImport(Constants.SDK_DLL_NAME)]
        private static extern void leiaSdkUnityRenderingPluginSetLayerCount(int layerCount);
        [DllImport(Constants.SDK_DLL_NAME)]
        private static extern int leiaSdkUnityRenderingPluginGetLayerCount();
#if UNITY_ANDROID
        [DllImport(Constants.SDK_DLL_NAME)]
        private static extern int leiaSdkUnityRenderingPluginProcessGuiInput_MotionEvent(IntPtr androidMotionEvent);
#endif
        [DllImport(Constants.SDK_DLL_NAME, CharSet = CharSet.Ansi)]
        private static extern int leiaSdkUnityRenderingPluginGetFloatProperty([MarshalAs(UnmanagedType.LPStr)] string propertyName, out float value);
        [DllImport(Constants.SDK_DLL_NAME, CharSet = CharSet.Ansi)]
        private static extern int leiaSdkUnityRenderingPluginSetFloatProperty([MarshalAs(UnmanagedType.LPStr)] string propertyName, float value);
    }
}
#endif