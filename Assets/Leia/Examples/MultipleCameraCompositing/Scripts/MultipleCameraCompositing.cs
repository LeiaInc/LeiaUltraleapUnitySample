using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using LeiaUnity;

namespace LeiaUnity.Examples
{
    public class MultipleCameraCompositing : MonoBehaviour
    {
        LeiaDisplay leiaDisplay;
        public LeiaDisplay FirstToRenderLeiaDisplay;
        bool initialized;
        int lastnunViews;

        void Start()
        {
            leiaDisplay = GetComponent<LeiaDisplay>();
        }

        public void LateUpdate()
        {
            if (initialized && leiaDisplay.GetEyeCamera(0).targetTexture != FirstToRenderLeiaDisplay.GetEyeCamera(0).targetTexture ||
                initialized && leiaDisplay.GetEyeCamera(1).targetTexture != FirstToRenderLeiaDisplay.GetEyeCamera(1).targetTexture)
            {
                initialized = false;
            }
            if (!initialized && FirstToRenderLeiaDisplay != null || lastnunViews != leiaDisplay.GetViewCount())
            {
                Debug.Log("share textures");
                for (int i = 0; i < FirstToRenderLeiaDisplay.GetViewCount(); i++)
                {
                    if (FirstToRenderLeiaDisplay.GetEyeCamera(i).targetTexture == null) return;
                    FirstToRenderLeiaDisplay.HeadCamera.depth = 0;
                    leiaDisplay.HeadCamera.depth = 1;
                    leiaDisplay.HeadCamera.clearFlags = CameraClearFlags.Depth;
                    leiaDisplay.GetEyeCamera(i).targetTexture.Release();
                    leiaDisplay.GetEyeCamera(i).targetTexture = FirstToRenderLeiaDisplay.GetEyeCamera(i).targetTexture;
                }
                lastnunViews = leiaDisplay.GetViewCount();
                initialized = true;
            }
        }
    }
}