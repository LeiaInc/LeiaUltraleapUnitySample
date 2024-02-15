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
    [ExecuteInEditMode]
    public class Eye : MonoBehaviour
    {
        public LeiaDisplay leiaDisplay;
        public Camera _eyecamera;
        public Camera eyecamera
        {
            get
            {
                if (_eyecamera == null)
                {
                    _eyecamera = transform.GetComponent<Camera>();
                }

                if (_eyecamera == null)
                {
                    _eyecamera = transform.gameObject.AddComponent<Camera>();
                }
                return _eyecamera;
            }
        }

        public RenderTexture TargetTexture
        {
            get { return !eyecamera ? null : eyecamera.targetTexture; }
            set { if (eyecamera) { eyecamera.targetTexture = value; } }
        }

        public Vector2 offset;

        void Start()
        {
            if (eyecamera == null)
            {
                _eyecamera = transform.gameObject.AddComponent<Camera>();
            }
        }

        /// <summary>
        /// Creates a renderTexture.
        /// </summary>
        /// <param name="width">Width of renderTexture in pixels</param>
        /// <param name="height">Height of renderTexture in pixels</param>
        /// <param name="viewName">Name of renderTexture</param>
        public void SetTextureParams(int width, int height, string viewName)
        {
            if (eyecamera == null)
            {
                return;
            }

            if (eyecamera.targetTexture == null)
            {
                TargetTexture = CreateRenderTexture(width, height, viewName, leiaDisplay.AntiAliasingLevel);
            }
            else
            {
                if (TargetTexture.width != width ||
                    TargetTexture.height != height)
                {
                    Release();
                    TargetTexture = CreateRenderTexture(width, height, viewName, leiaDisplay.AntiAliasingLevel);
                }
            }
        }
        private static RenderTexture CreateRenderTexture(int width, int height, string rtName, int antiAliasingLevel)
        {
            //Sanatizing variables to default to min requirements 
            width = width > 0 ? width : 1920;
            height = height > 0 ? height : 1200;
            antiAliasingLevel = antiAliasingLevel > 0 ? antiAliasingLevel : 1;

            var leiaViewSubTexture = new RenderTexture(width, height, 24)
            {
                name = rtName,
            };
            //leiaViewSubTexture.ApplyIntermediateTextureRecommendedProperties();
            //leiaViewSubTexture.ApplyLeiaViewRecommendedProperties();
            leiaViewSubTexture.antiAliasing = antiAliasingLevel;
            leiaViewSubTexture.Create();

            return leiaViewSubTexture;
        }

        public void Release()
        {
            // targetTexture can be null at this point in execution
            if (TargetTexture != null)
            {
                if (Application.isPlaying)
                {
                    TargetTexture.Release();
                    GameObject.Destroy(TargetTexture);
                }
                else
                {
                    TargetTexture.Release();
                    GameObject.DestroyImmediate(TargetTexture);
                }

                TargetTexture = null;
            }
        }

        public void Update()
        {
            if (leiaDisplay == null)
            {
                DestroyImmediate(gameObject);
                return;
            }
            float virtualBaseline = leiaDisplay.DepthFactor * leiaDisplay.IPDMM * leiaDisplay.MMToVirtual;
            if (leiaDisplay.DivideByPerspectiveFactor)
            {
                virtualBaseline /= leiaDisplay.FOVFactor;
            }
            transform.localPosition = offset * virtualBaseline;
            eyecamera.transform.rotation = leiaDisplay.transform.rotation;

            eyecamera.projectionMatrix = leiaDisplay.GetProjectionMatrixForCamera(eyecamera, transform.parent.localPosition, true);

            LeiaUtils.CopyCameraParameters(leiaDisplay.HeadCamera, eyecamera);
            if (Application.isPlaying)
            {
                eyecamera.enabled = true;
                eyecamera.cullingMask = leiaDisplay.ViewersHead.CullingMask;
            }
            else
            {
                //Disable eye camera in edit mode
                eyecamera.enabled = false;
            }
        }

        void SetPositionFromRealEyePosition(Vector3 EyePositionMM)
        {
            transform.localPosition = EyePositionMM * leiaDisplay.VirtualHeight / leiaDisplay.HeightMM;
        }

        private void OnDrawGizmos()
        {
            leiaDisplay.DrawFrustum(_eyecamera);
        }
    }
}
