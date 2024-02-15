/*********************************************************************************************************
*
* Copyright (C) 2024  Leia, Inc.
*
* This software has been provided under the Leia license agreement.
* You can find the agreement at https://www.leiainc.com/legal/license-agreement
*
* This source code is considered Creator Materials under the definitions of the Leia license agreement.
*
*********************************************************************************************************
*/

using UnityEngine;
namespace LeiaUnity
{
    public class LeiaRemoteUIHandler : MonoBehaviour
    {
        private Camera uiCamera;
        private RenderTexture renderTexture;
        public void HandleScreenSpaceUI()
        {
            CreateUICamera();
            SetupRenderTexture();
            HandleOverlayUI();
            OverlayUIToLeiaViews();
        }

        void CreateUICamera()
        {
            GameObject uiCameraObject = new GameObject("UICamera");
            uiCameraObject.transform.SetParent(FindObjectOfType<LeiaDisplay>().transform);
            uiCamera = uiCameraObject.AddComponent<Camera>();
            uiCamera.clearFlags = CameraClearFlags.SolidColor;
            uiCamera.backgroundColor = Color.clear;
            uiCamera.cullingMask = (1 << LayerMask.NameToLayer("UI"));
        }

        void SetupRenderTexture()
        {
            if (renderTexture != null) return;
            renderTexture = new RenderTexture(Screen.width, Screen.height, 24);
            uiCamera.targetTexture = renderTexture;
        }

        void HandleOverlayUI()
        {
            Canvas[] canvases = FindObjectsOfType<Canvas>();
            if (canvases != null)
            {
                foreach (var canvas in canvases)
                {
                    if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                    {
                        canvas.renderMode = RenderMode.ScreenSpaceCamera;
                        canvas.worldCamera = uiCamera;
                    }
                }
            }
        }

        void OverlayUIToLeiaViews()
        {
            LeiaDisplay leiadisplay = FindObjectOfType<LeiaDisplay>();
            for (int i = 0; i < leiadisplay.GetViewCount(); i++)
            {
                CameraUIOverlay camUIOverlay = leiadisplay.GetEyeCamera(i).gameObject.AddComponent<CameraUIOverlay>();
                camUIOverlay.UIRenderTexture = renderTexture;
            }
        }

        private void OnDestroy()
        {
            if (renderTexture != null && uiCamera != null && renderTexture == uiCamera.targetTexture)
            {
                uiCamera.targetTexture = null;
            }

            if (renderTexture != null)
            {
                DestroyImmediate(renderTexture);
            }
        }
    }
}