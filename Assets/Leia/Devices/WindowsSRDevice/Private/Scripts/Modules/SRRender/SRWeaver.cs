/*!
 * Copyright (C) 2022  Dimenco
 *
 * This software has been provided under the Dimenco EULA. (End User License Agreement)
 * You can find the agreement at https://www.dimenco.eu/eula
 *
 * This source code is considered Protected Code under the definitions of the EULA.
 */

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

#if !UNITY_2019_1_OR_NEWER
using UnityEngine.Experimental.Rendering;
#warning "SRUnity: weaving is not support before Unity 2019.1"
#endif

namespace SRUnity
{
    // Weaver class that handles weaving on all rendering pipeline. Default: hooks into OnRenderImage and calls the weaver logic. SRP: Executes the weaver logic on endFrameRendering.
    public class SRWeaver
    {
        [DllImport("SRUnityNative")]
        private static extern IntPtr GetNativeGraphicsEvent();

        [DllImport("SRUnityNative")]
        private static extern void SetWeaverContextPtr(IntPtr context);

        [DllImport("SRUnityNative")]
        private static extern void SetWeaverResourcePtr(IntPtr resource);

        [DllImport("SRUnityNative")]
        private static extern void SetWeaverOutputResolution(int Width, int Height);

        [DllImport("SRUnityNative")]
        private static extern bool GetWeaverEnabled();


        public void Init()
        {
            UpdateWeavingData(null);
        }

        public void Destroy()
        {
        }

        public bool CanWeave()
        {
            return GetWeaverEnabled();
        }

        public void WeaveToContext(ScriptableRenderContext context, Texture frameBuffer)
        {
#if UNITY_EDITOR
            if (Camera.current != null && Camera.current.cameraType != CameraType.Game) return;
#endif
            UpdateWeavingData(frameBuffer);               
                
            CommandBuffer cb = new CommandBuffer();
            cb.name = "SRWeave";
            cb.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
            cb.IssuePluginEvent(GetNativeGraphicsEvent(), 1);
            context.ExecuteCommandBuffer(cb);
        }

        public void WeaveToTarget(RenderTexture target, RenderTexture frameBuffer)
        {
            UpdateWeavingData(frameBuffer);

            RenderTexture.active = target;
            GL.IssuePluginEvent(GetNativeGraphicsEvent(), 1);

            // Clear framebuffer
            RenderTexture.active = frameBuffer;
            GL.Clear(true, true, Color.clear);
        }

        private void UpdateWeavingData(Texture frameBuffer)
        {
            SetWeaverContextPtr(SRCore.Instance.GetSrContext());

            if (frameBuffer != null)
            {
                SetWeaverResourcePtr(frameBuffer.GetNativeTexturePtr());
            }
            else
            {
                SetWeaverResourcePtr(IntPtr.Zero);
            }

            SetWeaverOutputResolution((int)Screen.width, (int)Screen.height);
        }
    }
}
