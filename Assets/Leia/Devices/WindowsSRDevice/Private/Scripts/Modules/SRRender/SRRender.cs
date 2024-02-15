/*!
 * Copyright (C) 2022  Dimenco
 *
 * This software has been provided under the Dimenco EULA. (End User License Agreement)
 * You can find the agreement at https://www.dimenco.eu/eula
 *
 * This source code is considered Protected Code under the definitions of the EULA.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if !UNITY_EDITOR && PLATFORM_STANDALONE_WIN
using SimulatedReality;
#endif
using UnityEngine.SceneManagement;

namespace SRUnity
{
    // Render module that handles scenes events, weaving and resolution switching
    public class SRRender : SimulatedRealityModule<SRRender>
    {
        private SRCompositor compositor = new SRCompositor();
        private SRWeaver weaver = new SRWeaver();

        public override void InitModule()
        {
            SRUnity.SRCore.OnContextChanged += OnContextChanged;
            SystemHandler.OnSceneChanged += OnSceneChanged;
            SystemHandler.OnWindowFocus += OnFocusChanged;
            OnContextChanged(SRUnity.SRContextChangeReason.Unknown);
        }

        public override void UpdateModule()
        {
        }

        public override void DestroyModule()
        {
            SRUnity.SRCore.OnContextChanged -= OnContextChanged;
            SystemHandler.OnSceneChanged -= OnSceneChanged;
            SystemHandler.OnWindowFocus -= OnFocusChanged;
        }

        public void OnContextChanged(SRUnity.SRContextChangeReason contextChangeReason)
        {
            if (SRUnity.SRCore.IsSimulatedRealityAvailable())
            {
                IntPtr srContext = SRUnity.SRCore.Instance.GetSrContext();
                if (srContext != IntPtr.Zero)
                {
                    compositor.Init();
                    weaver.Init();
                    SetResolution();
                }
                else
                {
                    compositor.Destroy();
                    weaver.Destroy();
                }
            }
        }

        public void OnFocusChanged(bool hasFocus)
        {
            if (hasFocus)
            {
                SRUnity.SRUtility.Trace("SRRender focus gained");
                SetResolution();
            }
            else
            {
                SRUnity.SRUtility.Trace("SRRender focus lost");
            }
        }

        private void SetResolution()
        {
            if (SRUnity.SRCore.IsSimulatedRealityAvailable())
            {
                Vector2Int resolution = SRUnity.SRCore.Instance.getResolution();
                if (resolution.x > Vector2Int.one.x || resolution.y > Vector2Int.one.y)
                {            
                    SRUnity.SRUtility.Trace("Setting resolution: " + resolution.x + "x" + resolution.y);
                    Screen.SetResolution(resolution.x, resolution.y, weaver.CanWeave() ? FullScreenMode.FullScreenWindow : FullScreenMode.ExclusiveFullScreen);
                }
             }
        }

        public SRCompositor GetCompositor()
        {
            return compositor;
        }

        public SRWeaver GetWeaver()
        {
            return weaver;
        }

        private void OnSceneChanged(Scene scene)
        {
            weaver.Destroy();
            OnContextChanged(SRUnity.SRContextChangeReason.Unknown);
        }
    }
}
