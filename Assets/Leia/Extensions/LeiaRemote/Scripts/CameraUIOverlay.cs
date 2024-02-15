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
    [RequireComponent(typeof(Camera))]
    public class CameraUIOverlay : MonoBehaviour
    {
        public RenderTexture UIRenderTexture;
        private Material OverlayMaterial;

        private void Start()
        {
            OverlayMaterial = new Material(Shader.Find("Custom/UIOverlay"));
        }

        void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            OverlayMaterial.SetTexture("_OverlayTex", UIRenderTexture);
            Graphics.Blit(src, dest, OverlayMaterial);
        }
    }
}
