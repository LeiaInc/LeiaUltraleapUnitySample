/*!
 * Copyright (C) 2022  Dimenco
 *
 * This software has been provided under the Dimenco EULA. (End User License Agreement)
 * You can find the agreement at https://www.dimenco.eu/eula
 *
 * This source code is considered Protected Code under the definitions of the EULA.
 */

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if !UNITY_EDITOR && PLATFORM_STANDALONE_WIN
using SimulatedReality;
#endif
using UnityEditor;
using System.Threading;

namespace SRUnity
{
    // Core module that handles the SRContext and display informartion
    public class SRCore : SimulatedRealityModule<SRCore>
    {
        private IntPtr srContext = IntPtr.Zero;
        private IntPtr srDisplay = IntPtr.Zero;

        private IntPtr srSystemSense = IntPtr.Zero;
#if !UNITY_EDITOR && PLATFORM_STANDALONE_WIN
        private SimulatedReality.SRCore.acceptSystemSenseCallback srSystemSenseCallback;
#endif
        private IntPtr srSystemSenseListener = IntPtr.Zero;

        private object contextMutex = new object();
        private IntPtr newContext = IntPtr.Zero;

        private object systemEventsMutex = new object();
#if !UNITY_EDITOR && PLATFORM_STANDALONE_WIN
        private List<SR_systemEvent> systemEvents = new List<SR_systemEvent>();
#endif
        private static bool srRuntimeAvailable = true;

        public override void PreInitModule() 
        {
            SRUnity.SRUtility.Trace("SRCore::PreInit");
#if !UNITY_EDITOR && PLATFORM_STANDALONE_WIN
            String runtimeRoot = SRUnity.SRInternalPlatform.GetSimulatedRealityRuntimePath();
            SRUnity.SRUtility.Trace("SR Runtime location: " + runtimeRoot);

            if (runtimeRoot == "" || !Directory.Exists(runtimeRoot + "\\bin"))
            {
                SRUnity.SRUtility.Warning("SR not available!");
                srRuntimeAvailable = false;
                HandleContextChanged(SRContextChangeReason.SRUnavailable);
                if (!SRProjectSettings.Instance.allowStartWithoutSimulatedRealityRuntime) 
                {
                    SRUnity.SRUtility.Trace("Quitting due to allowStartWithoutSimulatedRealityRuntime");
                    Application.Quit();
                }
            }
#endif
        }

        public override void InitModule()
        {
            SRUnity.SRUtility.Trace("SRCore::Init");

            if (IsSimulatedRealityAvailable())
            {            
                StartContextThread();
            }
        }

        SrRenderModeHint eventRenderModeHint = new SrRenderModeHint();

        public override void UpdateModule()
        {
            lock(systemEventsMutex)
            {
#if !UNITY_EDITOR && PLATFORM_STANDALONE_WIN
                foreach (SR_systemEvent systemEvent in systemEvents)
                {
                    switch (systemEvent.eventType)
                    {
                        case SR_eventType.ContextInvalid:
                        {
                            SRUnity.SRUtility.Trace("SR system event: ContextInvalid");
                            if (GetSrContext() != IntPtr.Zero)
                            {
                                ReleaseSrContext();
                                StartContextThread();
                            }

                            break;
                        }
                        case SR_eventType.SRUnavailable:
                        {
                            SRUnity.SRUtility.Trace("SR system event: SRUnavailable");
                            eventRenderModeHint.Force2D();
                            break;
                        }
                        case SR_eventType.SRRestored:
                        {
                            SRUnity.SRUtility.Trace("SR system event: SRRestored");
                            eventRenderModeHint.BecomeIndifferent();
                            break;
                        }
                    }
                }

                systemEvents.Clear();
#endif
            }

            bool dispatchContextChange = false;

            lock(contextMutex)
            {
                if (newContext != IntPtr.Zero)
                {
                    srContext = newContext;
                    newContext = IntPtr.Zero;
                    dispatchContextChange = true;
                }
            }

            if (dispatchContextChange)
            {
                HandleContextChanged(SRContextChangeReason.SRAvailable);
            }
        }

        public override void DestroyModule()
        {
            SRUnity.SRUtility.Trace("SRCore::Destroy");

            if (srContext != IntPtr.Zero && IsSimulatedRealityAvailable())
            {
                ReleaseSrContext();
            }
        }

        private void ReleaseSrContext()
        {
            SRUnity.SRUtility.Debug("SRCore::ReleaseSrContext");
            IntPtr oldContext;

            lock(contextMutex)
            {
                oldContext = srContext;
                srContext = IntPtr.Zero;
            }

            HandleContextChanged(SRContextChangeReason.Shutdown);

            SRUnity.SRUtility.Debug("deleteSRContext");
#if !UNITY_EDITOR && PLATFORM_STANDALONE_WIN
            SimulatedReality.SRCore.deleteSRContext(oldContext);
#endif
        }

        public static bool IsSimulatedRealityAvailable()
        {
            return srRuntimeAvailable;
        }

        // Call SR::Context::Initialize
        public void InitializeContext()
        {
            if (srContext != IntPtr.Zero)
            {
                lock(contextMutex)
                {
#if !UNITY_EDITOR && PLATFORM_STANDALONE_WIN
                    SimulatedReality.SRCore.initializeSRContext(srContext);
#endif
                }
            }
        }

        public IntPtr GetSrContext()
        {
            IntPtr result;

            lock(contextMutex)
            {
                result = srContext;
            }
            return result;
        }

        public delegate void OnContextChangedDelegate(SRContextChangeReason contextChangeReason);
        public static event OnContextChangedDelegate OnContextChanged;

        // Handle receiving or losing a context
        private void HandleContextChanged(SRContextChangeReason contextChangeReason)
        {
            SRUnity.SRUtility.Trace("SRCore::OnContextChanged");
#if !UNITY_EDITOR && PLATFORM_STANDALONE_WIN
            try
            {
                IntPtr context = GetSrContext();
    
                if (context != IntPtr.Zero)
                {
                    srDisplay = SimulatedReality.SRDisplays.createScreen(context);

                    srSystemSense = SimulatedReality.SRCore.createSystemSense(context);
                    srSystemSenseCallback = new SimulatedReality.SRCore.acceptSystemSenseCallback(OnReceiveSystemEvent);
                    srSystemSenseListener = SimulatedReality.SRCore.createSystemEventListener(srSystemSense, srSystemSenseCallback);

                    IntPtr lensHint = SRDisplays.createSwitchableLensHint(context);
                    SrRenderModeHint.SetLensHintInstance(lensHint);

                    lastKnownPhysicalSize = new Vector2(SRDisplays.getPhysicalSizeWidth(srDisplay), SRDisplays.getPhysicalSizeHeight(srDisplay));
                    lastKnownResolution = new Vector2Int(SRDisplays.getResolutionWidth(srDisplay), SRDisplays.getResolutionHeight(srDisplay));
                    lastKnownPhysicalResolution = new Vector2Int(SRDisplays.getPhysicalResolutionWidth(srDisplay), SRDisplays.getPhysicalResolutionHeight(srDisplay));
                    lastKnownDotPitch = SRDisplays.getDotPitch(srDisplay);
                    
                    lastknownSlant = WeaverInterface.GetSlant();
                    lastknownPx = WeaverInterface.GetPx();
                    lastknownN = WeaverInterface.GetN();
                    lastknownDoN = WeaverInterface.GetDoN();

                    InitializeContext();
                }
                else
                {
                    if (srSystemSenseListener != IntPtr.Zero)
                    {
                        SimulatedReality.SRCore.deleteSystemEventListener(srSystemSenseListener);
                        srSystemSenseListener = IntPtr.Zero;
                    }

                    srSystemSenseCallback = null;

                    srSystemSense = IntPtr.Zero;

                    srDisplay = IntPtr.Zero;

                    SrRenderModeHint.SetLensHintInstance(IntPtr.Zero);
                }
            }
            catch (Exception e)
            {
                SRUnity.SRUtility.Trace("SRCore::OnContextChanged failed: " + e.Message);
            }

            OnContextChanged?.Invoke(contextChangeReason);
#endif
        }

        private Vector2 lastKnownPhysicalSize = Vector2.one;
        public Vector2 getPhysicalSize()
        {
            return lastKnownPhysicalSize;
        }

        private Vector2Int lastKnownResolution = Vector2Int.one;
        public Vector2Int getResolution()
        {
            return lastKnownResolution;
        }

        private Vector2Int lastKnownPhysicalResolution = Vector2Int.one;
        public Vector2Int getPhysicalResolution()
        {
            return lastKnownPhysicalResolution;
        }

        private float lastKnownDotPitch = 0;
        public float getDotPitch()
        {

            return lastKnownDotPitch;
        }
        private float lastknownSlant = 0;
        public float getSlant()
        {
            return lastknownSlant;
        }
        private float lastknownPx = 0;
        public float getPx()
        {
            return lastknownPx;
        }
        private float lastknownN = 0;
        public float getN()
        {
            return lastknownN;
        }
        private float lastknownDoN = 0;
        public float getDoN()
        {
            return lastknownDoN;
        }
#if !UNITY_EDITOR && PLATFORM_STANDALONE_WIN
        private void OnReceiveSystemEvent(SR_systemEvent systemEvent)
        {
            lock(systemEventsMutex)
            {
                systemEvents.Add(systemEvent);
            }
        }
#endif

        private static void StartContextThread()
        {
            Thread thread = new Thread(ObtainNewContext);
            thread.Start();
        }

        private static void ObtainNewContext()
        {
#if !UNITY_EDITOR && PLATFORM_STANDALONE_WIN
            while(true)
            {   
                IntPtr context = Instance.GetSrContext();

                if (context == IntPtr.Zero)
                {
                    SRUnity.SRUtility.Trace("Trying to obtain new SRContext");
                    context = SimulatedReality.SRCore.newSRContext();
                    if (context != IntPtr.Zero)
                    {
                        SRUnity.SRUtility.Trace("Obtained new SRContext");

                        lock(Instance.contextMutex)
                        {
                            Instance.newContext = context;
                        }
                        return;
                    }
                }
                else
                {
                    SRUnity.SRUtility.Debug("SRContext already valid");
                    return;
                }

                Thread.Sleep(100);
            }
#endif
        }
    }   

    public enum SRContextChangeReason
    {
        SRAvailable,
        SRUnavailable,
        Shutdown,
        Unknown
    }
}
