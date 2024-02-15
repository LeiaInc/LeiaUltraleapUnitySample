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
namespace SRUnity
{
    // Eyes module that handles eye tracking
    public class SREyes : SimulatedRealityModule<SREyes>
    {
        public override void InitModule()
        {
            SRUnity.SRUtility.Debug("SREyes::Init");
        }

        public override void UpdateModule() { }

        public override void DestroyModule()
        {
            SRUnity.SRUtility.Debug("SREyes::Destroy");
        }

        public Vector3[] GetEyes(ISRSettingsInterface settings)
        {
            return SRHead.Instance.GetEyes(settings);
        }

        public static Vector3 GetDefaultEyePositionCM()
        {
            return SRHead.GetDefaultHeadPositionCM();
        }

        public Vector3 GetDefaultEyePosition(ISRSettingsInterface settings)
        {
            return SRHead.Instance.GetDefaultHeadPosition(settings);
        }
    }
}
