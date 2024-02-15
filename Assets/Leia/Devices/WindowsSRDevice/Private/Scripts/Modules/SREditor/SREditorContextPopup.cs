/*!
 * Copyright (C) 2022  Dimenco
 *
 * This software has been provided under the Dimenco EULA. (End User License Agreement)
 * You can find the agreement at https://www.dimenco.eu/eula
 *
 * This source code is considered Protected Code under the definitions of the EULA.
 */

#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System;

namespace SRUnity
{
    // Editor popup when (re)connecting to SRService
    [InitializeOnLoad]
    public class SrContextPopup
    {
        static SrContextPopup()
        {
            SRUtility.Debug("SrContextPopup::static");

            SRCore.OnContextChanged += OnContextChange;
            EditorApplication.update += RunOnce;
        }

        private static void RunOnce()
        {
            SRUtility.Debug("SrContextPopup::RunOnce");
            EditorApplication.update -= RunOnce;
            OnContextChange(SRUnity.SRContextChangeReason.Unknown);

            AppDomain.CurrentDomain.DomainUnload += new EventHandler(OnDomainUnload);
        }

        private static bool showingProgressBar = false;

#if UNITY_2020_1_OR_NEWER
        private static int progressTaskId = -1;
#endif

        private static void OnContextChange(SRUnity.SRContextChangeReason contextChangeReason)
        {
            SRUtility.Debug("SrContextPopup::OnContextChange");

            if (contextChangeReason != SRUnity.SRContextChangeReason.Shutdown)
            {

#if UNITY_2020_1_OR_NEWER
                if (SRCore.Instance.GetSrContext() == IntPtr.Zero && SRCore.IsSimulatedRealityAvailable()) 
                {
                    if (!showingProgressBar)
                    {
                        progressTaskId = Progress.Start("SRUnity", "Establishing connection to SRService", Progress.Options.Indefinite);
                        showingProgressBar = true;
                    }
                }
                else
                {
                    if (showingProgressBar)
                    {
                        Progress.Remove(progressTaskId);
                        progressTaskId = -1;
                        showingProgressBar = false;
                    }
                }
#else
                if (SRCore.Instance.GetSrContext() == IntPtr.Zero && SRCore.IsSimulatedRealityAvailable())
                {
                    if (!showingProgressBar)
                    {
                        EditorUtility.DisplayProgressBar("SRUnity", "Establishing connection to SRService", 0.0f);
                        showingProgressBar = true;
                    }
                }
                else
                {
                    if (showingProgressBar)
                    {
                        EditorUtility.ClearProgressBar();
                        showingProgressBar = false;
                    }
                }
#endif
            }
        }

        private static void OnDomainUnload(object sender, EventArgs e)
        {
            if (showingProgressBar)
            {
#if UNITY_2020_1_OR_NEWER
                Progress.Remove(progressTaskId);
                progressTaskId = -1;
                showingProgressBar = false;
#else
                EditorUtility.ClearProgressBar();
#endif
                showingProgressBar = false;
            }
        }
    }
}
#endif
