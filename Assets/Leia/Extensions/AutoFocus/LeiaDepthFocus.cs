/*!
* Copyright (C) 2023  Leia, Inc.
*
* This software has been provided under the Leia license agreement.
* You can find the agreement at https://www.leiainc.com/legal/license-agreement
*
* This source code is considered Creator Materials under the definitions of the Leia license agreement.
*/
using UnityEngine;
#if UNITY_EDITOR 
using UnityEditor;
#endif

namespace LeiaUnity
{
    [RequireComponent(typeof(LeiaDisplay))]
    [HelpURL("https://docs.leialoft.com/developer/unity-sdk/modules/auto-focus#leiadepthfocus")]

    public class LeiaDepthFocus : LeiaFocus, IReleasable
    {
        private Camera _depthCamera;
        private Camera DepthCamera
        {
            get
            {
                Transform DepthTransform = ParentCamera.transform.Find("DepthCamera");
                if (!DepthTransform)
                {
                    _depthCamera = new GameObject("DepthCamera").AddComponent<Camera>();
                }
                else
                {
                    _depthCamera = DepthTransform.GetComponent<Camera>();
                }
                return _depthCamera;
            }
        }

        ComputeShader DepthStatsShader;
        int DepthStatsKernelID;
        private Camera LeiaViewCamera;
        private Camera ParentCamera;
        private readonly int DepthRenderWidth = 512;
        private readonly int DepthRenderHeight = 288;
        private float DepthRange;
        private readonly float[] resultFloats = new float[4];
        private bool systemSupportsComputeShaders = false;
        private RenderTexture DepthRenderTexture;
        private ComputeBuffer buffer;

        protected override void OnEnable()
{
            base.OnEnable();

#if UNITY_EDITOR
#if UNITY_2019_1_OR_NEWER
        //Compute shaders should work without issue in editor on Android build target
#else
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
            {
                LogUtil.Warning("Warning! Known issue: LeiaDepthFocus may not function correctly in Unity Editor Versions before 2019 if the build target is set to Android. (It may still work in Android builds though)");
            }
#endif
#endif

            // DepthStats Compute Shader
            DepthStatsShader = Resources.Load<ComputeShader>("LeiaLoft_DepthStats");

            systemSupportsComputeShaders = SystemInfo.supportsComputeShaders;

            if (!systemSupportsComputeShaders)
            {
                Debug.LogError("System doesn't support compute shaders! LeiaDepthFocus will not function correctly, disabling component.");
                this.enabled = false;
                return;
            }

            DepthStatsKernelID = DepthStatsShader.FindKernel(
                "DepthStats"
            );

            ParentCamera = leiaDisplay.DriverCamera;
            leiaDisplay = GetComponent<LeiaDisplay>();

            DepthRenderTexture = new RenderTexture(DepthRenderWidth, DepthRenderHeight, 16, RenderTextureFormat.ARGB32);
            DepthRenderTexture.name = "DepthRenderTexture";
            DepthRenderTexture.antiAliasing = 1;
            DepthRenderTexture.autoGenerateMips = false;
            DepthRenderTexture.filterMode = FilterMode.Point;
            DepthRenderTexture.anisoLevel = 0;

            // set depth camera to draw all objects with depth shader and render into depth render texture
            DepthCamera.transform.parent = ParentCamera.transform;
            DepthCamera.transform.localPosition = Vector3.zero;
            DepthCamera.transform.localRotation = Quaternion.identity;
            DepthCamera.transform.localScale = Vector3.one;
            DepthCamera.SetReplacementShader(Shader.Find("LeiaLoft/DepthFromViewCoords"), "");
            DepthCamera.depth = -1;
            DepthCamera.enabled = true;
            DepthCamera.allowMSAA = false;
            DepthCamera.allowHDR = false;
            DepthCamera.clearFlags = CameraClearFlags.SolidColor;
            DepthCamera.backgroundColor = new Color(1, .5f, 1, 1);
            DepthCamera.targetTexture = DepthRenderTexture;

            //DEPTH STATS
            //Set textures on Kernels
            DepthStatsShader.SetTexture(0, "DepthTexture", DepthRenderTexture);
            DepthStatsShader.SetTexture(DepthStatsKernelID, "DepthTexture", DepthRenderTexture);

            buffer = new ComputeBuffer(resultFloats.Length, sizeof(float));
        }

        // Update is called once per frame
        protected override void LateUpdate()
        {
            if (!systemSupportsComputeShaders)
            {
                return;
            }

            SyncCameraSettings();

            if (Time.frameCount % 2 == 0)
            {
                ComputeDepthFocusTargets();
            }

            base.LateUpdate();
        }

        void ComputeDepthFocusTargets()
        {
            float newTargetBaseline;
            float newTargetConvergence;
            float DepthMin;
            float DepthMax;
            float DepthAvg;
  
            buffer.SetData(resultFloats);
            this.DepthStatsShader.SetBuffer(DepthStatsKernelID, "resultFloats", buffer);
            DepthStatsShader.Dispatch(DepthStatsKernelID, 1, 1, 1);
            buffer.GetData(resultFloats);
   

            Color DepthStatsColor = new Color(resultFloats[0], resultFloats[1], resultFloats[2]);

            if (DepthStatsColor.r > 0.0f)
            {
                DepthMin = DepthStatsColor.r * DepthRange + DepthCamera.nearClipPlane;
                DepthMax = DepthStatsColor.g * DepthRange + DepthCamera.nearClipPlane;
                DepthAvg = DepthStatsColor.b * DepthRange + DepthCamera.nearClipPlane;

                newTargetConvergence = DepthAvg;
                float newTargetBaselineNearPlane =
                    LeiaDisplayUtils.GetRecommendedBaselineBasedOnNearPlane(
                        leiaDisplay,
                        DepthMin,
                        newTargetConvergence
                        ) * DepthScale
                    ;
                float newTargetBaselineFarPlane =
                    LeiaDisplayUtils.GetRecommendedBaselineBasedOnFarPlane(
                        leiaDisplay,
                        DepthMax,
                        newTargetConvergence
                        ) * DepthScale
                    ;

                newTargetBaseline = Mathf.Min(newTargetBaselineNearPlane, newTargetBaselineFarPlane);

                SetTargetConvergence(newTargetConvergence);
                SetTargetBaseline(newTargetBaseline);
            }
        }

        public void SyncCameraSettings()
        {
            DepthCamera.nearClipPlane = leiaDisplay.DriverCamera.nearClipPlane;
            DepthCamera.farClipPlane = leiaDisplay.DriverCamera.farClipPlane;
            DepthRange = DepthCamera.farClipPlane - DepthCamera.nearClipPlane;
            if (LeiaViewCamera == null)
            {
                if (GameObject.Find("LeiaView"))
                {
                    LeiaViewCamera = GameObject.Find("LeiaView").GetComponent<Camera>();
                }
            }
            else
            {
                DepthCamera.cullingMask = LeiaViewCamera.cullingMask;
            }
            DepthCamera.orthographic = ParentCamera.orthographic;
            if (DepthCamera.orthographic)
            {
                DepthCamera.orthographicSize = ParentCamera.orthographicSize;
            }
            else
            {
                DepthCamera.fieldOfView = leiaDisplay.DriverCamera.fieldOfView;
            }
        }


        private void OnDisable()
        {
            Release();
        }

        public void Release()
        {
            if (buffer != null)
            {
                buffer.Release();
            }

            if (DepthRenderTexture != null)
            {
                DepthRenderTexture.Release();
            }
        }
    }
}