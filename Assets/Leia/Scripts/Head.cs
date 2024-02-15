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

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static LeiaUnity.LeiaDisplay;
using UnityEngine.Rendering;

namespace LeiaUnity
{
    [ExecuteInEditMode]
    public class Head : MonoBehaviour
    {
        public LeiaDisplay leiaDisplay;
        public List<Eye> eyes;
        public Vector3 HeadPositionMM;
        public Camera headcamera;
        public LayerMask CullingMask;

        public List<Vector2> ViewConfig;

        private Vector3 InitPosition;

        private readonly Dictionary<Camera, List<System.Action>> beginViewRenderingEvents = new Dictionary<Camera, List<System.Action>>();
        private readonly Dictionary<Camera, List<System.Action>> endViewRenderingEvents = new Dictionary<Camera, List<System.Action>>();

        public void InitHead(List<Vector2> ViewConfig, LeiaDisplay leiaDisplay)
        {
            CullingMask = -1; //Show all layers by default
            headcamera = gameObject.AddComponent<Camera>();
            headcamera.depth = 1f;
#if LEIA_HDRP_DETECTED
            if (headcamera != null)
            {
                var additionalCameraData = gameObject.GetComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>();
                if (additionalCameraData == null)
                {
                    additionalCameraData = gameObject.AddComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>();
                }
            }
#endif
            //headcamera.enabled = false;
            //EditorUtility.CopySerialized(leiaDisplay.cam, gameObject);
            HeadPositionMM = new Vector3(0, 0, leiaDisplay.ViewingDistanceMM);

            this.leiaDisplay = leiaDisplay;
            this.ViewConfig = ViewConfig;

            eyes = new List<Eye>();

            int counter = 0;
            foreach (var offset in ViewConfig)
            {
                GameObject pivotGO;
                if (counter == 0)
                {
                    pivotGO = new GameObject("LeftEye");
                } 
                else if (counter == 1)
                {
                    pivotGO = new GameObject("RightEye");
                }
                else
                {
                    pivotGO = new GameObject("Eye");
                }
                pivotGO.transform.parent = transform;
                Eye newEye = pivotGO.AddComponent<Eye>();
                newEye.offset = offset;
                newEye.leiaDisplay = leiaDisplay;
                //newEye.eyecamera.nearClipPlane = headcamera.nearClipPlane;
                //newEye.eyecamera.farClipPlane = headcamera.farClipPlane;
                eyes.Add(newEye);
                newEye.Update();

                counter++;
            }
        }

        private void Start()
        {
            if (Application.isPlaying)
            {
                CullingMask = headcamera.cullingMask;
                headcamera.cullingMask = 0;
            }
            // this has to be Start, not OnEnable, because in OnEnable the Eyes.eyecamera is not set set
            if (RenderPipelineUtils.IsUnityRenderPipeline())
            {
                Debug.Log("Render Pipeline detected!");
                RenderPipelineManager.endFrameRendering += EndFrameRenderHook;
                // this code calls the camera pre-render events events every frame
                RenderPipelineManager.beginCameraRendering += BeginCameraRenderingHook;
                // this code calls the camera post-render events every frame
                RenderPipelineManager.endCameraRendering += EndCameraRenderingHook;

                // this code sets some pre-Render events for each eyecamera each frame
                for (int i = 0; i < eyes.Count; ++i)
                {
                    if (!beginViewRenderingEvents.ContainsKey(eyes[i].eyecamera))
                    {
                        beginViewRenderingEvents[eyes[i].eyecamera] = new List<System.Action>();
                    }

                    // have to not capture the value within the callback
                    float viewID = i * 1.0f;
                    // create the action event here here. will be called later
                    System.Action setViewID = () =>
                    {
                        Shader.SetGlobalFloat("_LeiaViewID", viewID);
                    };

                    beginViewRenderingEvents[eyes[i].eyecamera].Add(setViewID);
                }

                // this code sets some post-Render events for each LeiaView each frame
                for (int i = 0; i < eyes.Count; ++i)
                {
                    if (!endViewRenderingEvents.ContainsKey(eyes[i].eyecamera))
                    {
                        endViewRenderingEvents[eyes[i].eyecamera] = new List<System.Action>();
                    }

                    System.Action unsetViewID = () =>
                    {
                        Shader.SetGlobalFloat("_LeiaViewID", -1);
                    };

                    endViewRenderingEvents[eyes[i].eyecamera].Add(unsetViewID);
                }
            }

        }
        void OnDisable()
        {
            if (RenderPipelineUtils.IsUnityRenderPipeline())
            {
                RenderPipelineManager.endFrameRendering -= EndFrameRenderHook;
                RenderPipelineManager.beginCameraRendering -= BeginCameraRenderingHook;
            }
        }
        void BeginCameraRenderingHook(ScriptableRenderContext context, Camera renderingCam)
        {
            // call events for each view. Generally these will be shader SetFloat events
            if (beginViewRenderingEvents.ContainsKey(renderingCam))
            {
                foreach (System.Action action in beginViewRenderingEvents[renderingCam])
                {
                    action.Invoke();
                }
            }
        }

        void EndCameraRenderingHook(ScriptableRenderContext context, Camera renderingCam)
        {
            if (endViewRenderingEvents.ContainsKey(renderingCam))
            {
                foreach (System.Action action in endViewRenderingEvents[renderingCam])
                {
                    action.Invoke();
                }
            }
        }

        void EndFrameRenderHook(ScriptableRenderContext context, Camera[] cams)
        {
            if (cams != null && headcamera != null && cams[0] == headcamera)
            {
                // only need to run EndFramRenderHook for Head's headcamera
                Camera.SetupCurrent(headcamera);
                OnPostRender();
            }
        }
        
        public void Update()
        {
            if (leiaDisplay == null || leiaDisplay.ViewersHead != this)
            {
                DestroyImmediate(gameObject);
                return;
            }

            transform.localPosition = leiaDisplay.RealToVirtualCenterFacePosition(HeadPositionMM);
            transform.LookAt(leiaDisplay.transform.position, leiaDisplay.transform.up);

            Matrix4x4 p = leiaDisplay.GetProjectionMatrixForCamera(headcamera, Vector3.zero, false);

            headcamera.projectionMatrix = p;

            foreach (Eye eye in eyes)
            {
                eye.Update();
            }

            if (leiaDisplay.DriverCamera != null)
            {
                leiaDisplay.DriverCamera.enabled = false;
                LeiaUtils.CopyCameraParameters(leiaDisplay.DriverCamera, headcamera);
                CullingMask = leiaDisplay.DriverCamera.cullingMask;
            }

            if (Application.isPlaying && RenderTrackingDevice.Instance.DesiredLightfieldMode == RenderTrackingDevice.LightfieldMode.On)
            {
                //Set head camera culling mask to nothing so that only the Eye cameras render in 3d mode
                headcamera.cullingMask = 0;
            }
            else
            {
                //Render the head camera in 2D mode and in edit mode
                headcamera.cullingMask = CullingMask;
            }
        }

        private void OnDrawGizmos()
        {
            //LeiaDisplay.DrawFrustum(transform, leiaDisplay);
        }


        void OnPostRender()
        {
            if (Application.isPlaying && RenderTrackingDevice.Instance.DesiredLightfieldMode == RenderTrackingDevice.LightfieldMode.On)
            {
                leiaDisplay.RenderImage();
            }
        }
    }
}
