/*!
 * Copyright (C) 2022  Dimenco
 *
 * This software has been provided under the Dimenco EULA. (End User License Agreement)
 * You can find the agreement at https://www.dimenco.eu/eula
 *
 * This source code is considered Protected Code under the definitions of the EULA.
 */

/*!
 * A demo to read display parameters.
 * 
 * Attach this script to a GameObject in unity and use the SimulatedReality API.
 */

using System;
using UnityEngine;
#if !UNITY_EDITOR && PLATFORM_STANDALONE_WIN
using SimulatedReality;
#endif
namespace SRDemo
{
    class DisplayParameters : MonoBehaviour
    {        
        public int resolutionHeight;
        public int resolutionWidth;
        public int physicalResolutionHeight;
        public int physicalResolutionWidth;
        public float getPhysicalSizeHeight;
        public float getPhysicalSizeWidth;
        public float getDotPitch;

        private void Start()
        {
            SRUnity.SRCore.OnContextChanged += OnContextChanged;
            OnContextChanged(SRUnity.SRContextChangeReason.Unknown);
        }

        private void Destroy()
        {
            SRUnity.SRCore.OnContextChanged -= OnContextChanged;
        }

        private void OnContextChanged(SRUnity.SRContextChangeReason contextChangeReason)
        {
            resolutionHeight = SRUnity.SRCore.Instance.getResolution().y;
            resolutionWidth = SRUnity.SRCore.Instance.getResolution().x;
            physicalResolutionHeight = SRUnity.SRCore.Instance.getPhysicalResolution().y;
            physicalResolutionWidth = SRUnity.SRCore.Instance.getPhysicalResolution().x;
            getPhysicalSizeHeight = SRUnity.SRCore.Instance.getPhysicalSize().y;
            getPhysicalSizeWidth = SRUnity.SRCore.Instance.getPhysicalSize().x;
            getDotPitch = SRUnity.SRCore.Instance.getDotPitch();

            // Print the screen parameters
            Debug.Log("getResolutionHeight: " + resolutionHeight);
            Debug.Log("getResolutionWidth: " + resolutionWidth);
            Debug.Log("getPhysicalResolutionHeight: " + physicalResolutionHeight);
            Debug.Log("getPhysicalResolutionWidth: " + physicalResolutionWidth);
            Debug.Log("getPhysicalSizeHeight: " + getPhysicalSizeHeight);
            Debug.Log("getPhysicalSizeWidth: " + getPhysicalSizeWidth);
            Debug.Log("getDotPitch: " + getDotPitch);
        }
    }
}
