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
using UnityEditor;

using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;

#if UNITY_2020_2_OR_NEWER
using UnityEngine.Rendering;
using System.Threading.Tasks;
#endif

#if UNITY_EDITOR
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
#endif

#if LEIA_HDRP_DETECTED
using UnityEngine.Rendering.HighDefinition;
#elif LEIA_URP_DETECTED
using UnityEngine.Rendering.Universal;
#endif

namespace LeiaUnity
{
    public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        private static T _instance = null;
        private static bool _isQuiting = false;
        public virtual string ObjectName { get { return typeof(T).Name; } }
        public static T Instance
        {
            get
            {
                if (_instance == null && !_isQuiting)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying && Application.isEditor)
                        return null;
#endif
                }
                return _instance;
            }
            set
            {
                _instance = value;
            }
        }
        public static bool InstanceIsNull
        { get { return _instance == null; } }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = (T)this;
            }

            if (_instance != null && _instance != this)
            {
                Destroy(this.gameObject);
            }
        }

        private void OnApplicationQuit()
        {
            _isQuiting = true;
        }
    }

    public static class BehaviourUtils
    {
        public static void CopyFieldsFrom(this Behaviour target, Behaviour original, Camera rootCamera, LeiaView view)
        {
            if (target == null || original == null) { return; }

            Debug.AssertFormat(original.GetType() == target.GetType(), "Possible type mismatch - {0} vs {1}", original.GetType(), target.GetType());
            var fields = target.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            for (int j = 0; j < fields.Length; j++)
            {
                if (fields[j].FieldType == typeof(Camera) && (fields[j].GetValue(original) as Camera) == rootCamera)
                {
                    fields[j].SetValue(target, target.GetComponent<Camera>());
                }
                else if (fields[j].FieldType == typeof(LeiaUnity.LeiaView))
                {
                    fields[j].SetValue(target, view);
                }

                else if (fields[j].FieldType != typeof(UnityEngine.Rendering.CommandBuffer))
                {
                    fields[j].SetValue(target, fields[j].GetValue(original));
                }
            }
        }
    }

    public struct UnityCameraParams
    {
        public float FieldOfView { get; set; }
        public float Near { get; set; }
        public float Far { get; set; }
        public float Depth { get; set; }
        public int CullingMask { get; set; }
        public CameraClearFlags ClearFlags { get; set; }
        public Color BackgroundColor { get; set; }
        public bool AllowHDR { get; set; }
        public bool Orthographic { get; set; }
        public float OrthographicSize { get; set; }
        public Rect ViewportRect { get; set; }
        public RenderingPath RenderingPath { get; set; }
        public bool UseOcclusionCulling { get; set; }
    }

    public class CameraCalculatedParams
    {
        public float ScreenHalfHeight { get; private set; }
        public float ScreenHalfWidth { get; private set; }
        public float EmissionRescalingFactor { get; private set; }
        public static float GetViewportAspectFor(Camera renderingCamera)
        {
            return renderingCamera.pixelRect.width * 1.0f / renderingCamera.pixelRect.height;
        }

        public static float GetViewportAspectFor(Head renderingCamera)
        {
            return GetViewportAspectFor(renderingCamera.headcamera);
        }
    }

    public static class RenderPipelineUtils
    {
        static readonly string assetTypeURP = "UniversalRenderPipelineAsset";
        static readonly string assetTypeHDRP = "HDRenderPipelineAsset";
        static string renderAssetName;

#if LEIA_URP_DETECTED
        public static int GetRendererIndex(this UnityEngine.Rendering.Universal.UniversalAdditionalCameraData camData)
        {
            try
            {
                return (int)typeof(UnityEngine.Rendering.Universal.UniversalAdditionalCameraData)
                    .GetField("m_RendererIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .GetValue(camData);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error accessing m_RendererIndex: {e.Message}");
                return -1;
            }
        }
#endif
        public static bool IsUnityRenderPipeline()
        {
            if (GraphicsSettings.renderPipelineAsset == null) { return false; }
            renderAssetName = GraphicsSettings.renderPipelineAsset.GetType().Name;
            return (renderAssetName.Contains(assetTypeURP) || renderAssetName.Contains(assetTypeHDRP));
        }
    }

    public static class CompileDefineUtil
    {
#if UNITY_EDITOR
        public static void RemoveCompileDefine(string defineCompileConstant, BuildTargetGroup[] targetGroups = null)
        {
            if (targetGroups == null)
            {
                targetGroups = (BuildTargetGroup[])System.Enum.GetValues(typeof(BuildTargetGroup));
            }

            char separateChar = ';';

            foreach (BuildTargetGroup grp in targetGroups)
            {
                var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(grp);
                string newDefines = "";

                if (!defines.Contains(defineCompileConstant))
                {
                    continue;
                }

                foreach (var define in defines.Split(separateChar))
                {
                    if (define != defineCompileConstant)
                    {
                        if (newDefines.Length != 0)
                        {
                            newDefines += separateChar;
                        }
                        newDefines += define;
                    }
                }

                PlayerSettings.SetScriptingDefineSymbolsForGroup(grp, newDefines);
            }
        }

        public static string[] GetCompileDefinesWithPrefix(string prefix, BuildTargetGroup platform)
        {
            char separateChar = ';';
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(platform);
            var result = new List<string>();

            if (!defines.Contains(prefix))
            {
                return new string[0];
            }

            foreach (var define in defines.Split(separateChar))
            {
                if (define.StartsWith(prefix))
                {
                    result.Add(define);
                }
            }

            return result.ToArray();
        }

        public static void AddCompileDefine(BuildTargetGroup platform, string newDefine)
        {
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(platform);

            char separateChar = ';';

            if (!defines.Split(separateChar).Contains(newDefine))
            {
                if (defines.Length != 0)
                {
                    defines += separateChar;
                }
                PlayerSettings.SetScriptingDefineSymbolsForGroup(platform, defines + newDefine);
            }
        }

        //Static call to be made from a console which will set windows compiler flags to contain "LeiaUnity_CONFIG_OVERRIDE"
        public static void AddCompileDefineConfigOverride()
        {
            AddCompileDefine(BuildTargetGroup.Standalone, "LeiaUnity_CONFIG_OVERRIDE");
            AddCompileDefine(BuildTargetGroup.Android, "LeiaUnity_CONFIG_OVERRIDE");
        }

        public static void RemoveCompileDefineConfigOverride()
        {
            RemoveCompileDefine("LeiaUnity_CONFIG_OVERRIDE", new BuildTargetGroup[] { BuildTargetGroup.Standalone, BuildTargetGroup.Android });
        }
#endif
    }

    public static class LeiaCameraBounds
    {
        public static LeiaCameraData ComputeLeiaCameraData(Camera camera, float convergenceDistance, float baselineScaling)
        {
            LeiaCameraData leiaCameraData = new LeiaCameraData();
            int viewResolutionY = 1280;// LeiaDisplay.Instance.GetDeviceViewResolution().y;
            float SystemDisparityPixels = 4.0f;// LeiaDisplay.Instance.GetDeviceSystemDisparityPixels();
            float screenRatio = (float)Screen.width / Screen.height;
            float f = viewResolutionY / 2f / Mathf.Tan(camera.fieldOfView * Mathf.PI / 360f);
            leiaCameraData.baseline = SystemDisparityPixels * baselineScaling * convergenceDistance / f;
            leiaCameraData.screenHalfHeight = convergenceDistance * Mathf.Tan(camera.fieldOfView * Mathf.PI / 360.0f);
            leiaCameraData.screenHalfWidth = screenRatio * leiaCameraData.screenHalfHeight;
            leiaCameraData.baselineScaling = baselineScaling;
            return leiaCameraData;
        }
        public static LeiaBoundsData ComputeLeiaBoundsData(Camera camera, LeiaCameraData leiaCamera, float convergenceDistance, Vector2 cameraShift)
        {
            LeiaBoundsData leiaBounds = new LeiaBoundsData();
            var localToWorldMatrix = camera.transform.localToWorldMatrix;

            localToWorldMatrix.SetColumn(0, localToWorldMatrix.GetColumn(0).normalized);
            localToWorldMatrix.SetColumn(1, localToWorldMatrix.GetColumn(1).normalized);
            localToWorldMatrix.SetColumn(2, localToWorldMatrix.GetColumn(2).normalized);
            Rect cameraRect = camera.rect;
            cameraRect.yMax = Mathf.Min(cameraRect.yMax, 1);
            cameraRect.yMin = Mathf.Max(cameraRect.yMin, 0);
            cameraRect.xMax = Mathf.Min(cameraRect.xMax, 1);
            cameraRect.xMin = Mathf.Max(cameraRect.xMin, 0);
            float rh = cameraRect.height;
            float rw = cameraRect.width / rh;

            float screenRatio = 1.6f;
#if !UNITY_EDITOR && UNITY_ANDROID
            screenRatio = (float) Screen.width / Screen.height;
#endif
            if (camera.orthographic)
            {
                // assumes baseline = (baseline scaling) * (width of view in world units) * (system disparity in pixels) * (convergence distance) / (view width in pixels)
                float halfSizeY = camera.orthographicSize;
                float halfSizeX = halfSizeY * screenRatio;

                float left = -halfSizeX * rw;
                float right = -left;

                Vector3 screenTopLeft = localToWorldMatrix.MultiplyPoint(new Vector3(left, halfSizeY, convergenceDistance));
                Vector3 screenTopRight = localToWorldMatrix.MultiplyPoint(new Vector3(right, halfSizeY, convergenceDistance));
                Vector3 screenBottomLeft = localToWorldMatrix.MultiplyPoint(new Vector3(left, -halfSizeY, convergenceDistance));
                Vector3 screenBottomRight = localToWorldMatrix.MultiplyPoint(new Vector3(right, -halfSizeY, convergenceDistance));
                float negativeSystemDisparityZ = convergenceDistance - 1.0f / leiaCamera.baselineScaling;
                Vector3 nearTopLeft = localToWorldMatrix.MultiplyPoint(new Vector3(left, halfSizeY, negativeSystemDisparityZ));
                Vector3 nearTopRight = localToWorldMatrix.MultiplyPoint(new Vector3(right, halfSizeY, negativeSystemDisparityZ));
                Vector3 nearBottomLeft = localToWorldMatrix.MultiplyPoint(new Vector3(left, -halfSizeY, negativeSystemDisparityZ));
                Vector3 nearBottomRight = localToWorldMatrix.MultiplyPoint(new Vector3(right, -halfSizeY, negativeSystemDisparityZ));
                float positiveSystemDisparityZ = convergenceDistance + 1.0f / leiaCamera.baselineScaling;
                Vector3 farTopLeft = localToWorldMatrix.MultiplyPoint(new Vector3(left, halfSizeY, positiveSystemDisparityZ));
                Vector3 farTopRight = localToWorldMatrix.MultiplyPoint(new Vector3(right, halfSizeY, positiveSystemDisparityZ));
                Vector3 farBottomLeft = localToWorldMatrix.MultiplyPoint(new Vector3(left, -halfSizeY, positiveSystemDisparityZ));
                Vector3 farBottomRight = localToWorldMatrix.MultiplyPoint(new Vector3(right, -halfSizeY, positiveSystemDisparityZ));
                leiaBounds.screen = new[] { screenTopLeft, screenTopRight, screenBottomRight, screenBottomLeft };
                leiaBounds.south = new[] { nearTopLeft, nearTopRight, nearBottomRight, nearBottomLeft };
                leiaBounds.north = new[] { farTopLeft, farTopRight, farBottomRight, farBottomLeft };
                leiaBounds.top = new[] { nearTopLeft, nearTopRight, farTopRight, farTopLeft };
                leiaBounds.bottom = new[] { nearBottomLeft, nearBottomRight, farBottomRight, farBottomLeft };
                leiaBounds.east = new[] { nearTopRight, nearBottomRight, farBottomRight, farTopRight };
                leiaBounds.west = new[] { nearTopLeft, nearBottomLeft, farBottomLeft, farTopLeft };
            }
            else
            {
                float halfSizeX = leiaCamera.screenHalfHeight;
                float left = -leiaCamera.screenHalfWidth * rw;
                float right = -left;

                cameraShift = leiaCamera.baseline * cameraShift;
                Vector3 screenTopLeft = localToWorldMatrix.MultiplyPoint(new Vector3(left, halfSizeX, convergenceDistance));
                Vector3 screenTopRight = localToWorldMatrix.MultiplyPoint(new Vector3(right, halfSizeX, convergenceDistance));
                Vector3 screenBottomLeft = localToWorldMatrix.MultiplyPoint(new Vector3(left, -halfSizeX, convergenceDistance));
                Vector3 screenBottomRight = localToWorldMatrix.MultiplyPoint(new Vector3(right, -halfSizeX, convergenceDistance));
                float nearPlaneZ = (leiaCamera.baselineScaling * convergenceDistance) / (leiaCamera.baselineScaling + 1f);
                float nearRatio = nearPlaneZ / convergenceDistance;
                float nearShiftRatio = 1f - nearRatio;
                Bounds localNearPlaneBounds = new Bounds(
                  new Vector3(nearShiftRatio * cameraShift.x, nearShiftRatio * cameraShift.y, nearPlaneZ),
                  new Vector3(right * nearRatio * 2, leiaCamera.screenHalfHeight * nearRatio * 2, 0));
                Vector3 nearTopLeft = localToWorldMatrix.MultiplyPoint(new Vector3(localNearPlaneBounds.min.x, localNearPlaneBounds.max.y, localNearPlaneBounds.center.z));
                Vector3 nearTopRight = localToWorldMatrix.MultiplyPoint(new Vector3(localNearPlaneBounds.max.x, localNearPlaneBounds.max.y, localNearPlaneBounds.center.z));
                Vector3 nearBottomLeft = localToWorldMatrix.MultiplyPoint(new Vector3(localNearPlaneBounds.min.x, localNearPlaneBounds.min.y, localNearPlaneBounds.center.z));
                Vector3 nearBottomRight = localToWorldMatrix.MultiplyPoint(new Vector3(localNearPlaneBounds.max.x, localNearPlaneBounds.min.y, localNearPlaneBounds.center.z));
                float farPlaneZ = (leiaCamera.baselineScaling * convergenceDistance) / (leiaCamera.baselineScaling - 1f);
                farPlaneZ = 1f / Mathf.Max(1f / farPlaneZ, 1e-5f);
                float farRatio = farPlaneZ / convergenceDistance;
                float farShiftRatio = 1f - farRatio;
                Bounds localFarPlaneBounds = new Bounds(
                  new Vector3(farShiftRatio * cameraShift.x, farShiftRatio * cameraShift.y, farPlaneZ),
                  new Vector3(right * farRatio * 2, halfSizeX * farRatio * 2, 0));

                Vector3 farTopLeft = localToWorldMatrix.MultiplyPoint(new Vector3(localFarPlaneBounds.min.x, localFarPlaneBounds.max.y, localFarPlaneBounds.center.z));
                Vector3 farTopRight = localToWorldMatrix.MultiplyPoint(new Vector3(localFarPlaneBounds.max.x, localFarPlaneBounds.max.y, localFarPlaneBounds.center.z));
                Vector3 farBottomLeft = localToWorldMatrix.MultiplyPoint(new Vector3(localFarPlaneBounds.min.x, localFarPlaneBounds.min.y, localFarPlaneBounds.center.z));
                Vector3 farBottomRight = localToWorldMatrix.MultiplyPoint(new Vector3(localFarPlaneBounds.max.x, localFarPlaneBounds.min.y, localFarPlaneBounds.center.z));

                leiaBounds.screen = new[] { screenTopLeft, screenTopRight, screenBottomRight, screenBottomLeft };
                leiaBounds.south = new[] { nearTopLeft, nearTopRight, nearBottomRight, nearBottomLeft };
                leiaBounds.north = new[] { farTopLeft, farTopRight, farBottomRight, farBottomLeft };
                leiaBounds.top = new[] { nearTopLeft, nearTopRight, farTopRight, farTopLeft };
                leiaBounds.bottom = new[] { nearBottomLeft, nearBottomRight, farBottomRight, farBottomLeft };
                leiaBounds.east = new[] { nearTopRight, nearBottomRight, farBottomRight, farTopRight };
                leiaBounds.west = new[] { nearTopLeft, nearBottomLeft, farBottomLeft, farTopLeft };
            }
            return leiaBounds;
        }
        private static readonly Color _leiaScreenPlaneColor = new Color(20 / 255.0f, 100 / 255.0f, 160 / 255.0f, 0.2f);
        private static readonly Color _leiaScreenWireColor = new Color(35 / 255.0f, 200 / 255.0f, 1.0f, 0.6f);
        private static readonly Color _leiaBoundsPlaneColor = new Color(1.0f, 1.0f, 1.0f, 0.1f);
        private static readonly Color _leiaBoundsWireColor = new Color(1.0f, 1.0f, 1.0f, 0.2f);
    }

    [System.Serializable]
    public struct LeiaCameraData
    {
        public float baseline { get; set; }
        public float screenHalfHeight { get; set; }
        public float screenHalfWidth { get; set; }
        public float baselineScaling { get; set; }
    }

    [System.Serializable]
    public struct LeiaBoundsData
    {
        public Vector3[] screen { get; set; }
        public Vector3[] north { get; set; }
        public Vector3[] south { get; set; }
        public Vector3[] top { get; set; }
        public Vector3[] bottom { get; set; }
        public Vector3[] east { get; set; }
        public Vector3[] west { get; set; }
    }

#if UNITY_EDITOR
    public static class UndoableInputFieldUtils
    {
        public static void ImmediateFloatField(Func<float> getter, Action<float> setter, string label, UnityEngine.Object obj)
        {
            var value = getter();
            var newValue = EditorGUILayout.FloatField(label, value);

            if (!value.Equals(newValue))
            {
                if (obj != null)
                {
                    Undo.RecordObject(obj, label);
                }

                setter(newValue);

                if (obj != null)
                {
                    EditorUtility.SetDirty(obj);
                }
            }
        }
        public static void BoolField(Func<bool> getter, Action<bool> setter, string label, UnityEngine.Object obj)
        {
            var value = getter();
            var newValue = GUILayout.Toggle(value, label);

            if (!value.Equals(newValue))
            {
                if (obj != null)
                {
                    Undo.RecordObject(obj, label);
                }

                setter(newValue);

                if (obj != null)
                {
                    EditorUtility.SetDirty(obj);
                }
            }
        }

        public static void PopupLabeled(Action<int> setter, string label, int index, string[] options)
        {
            PopupLabeled(setter, label, index, options, null);
        }
        public static void PopupLabeled(Action<int> setter, string label, int index, string[] options, UnityEngine.Object obj)
        {
            var newIndex = EditorGUILayout.Popup(label, index, options);

            if (index >= 0 && index != newIndex)
            {
                if (obj != null)
                {
                    Undo.RecordObject(obj, label);
                }

                setter(newIndex);

                if (obj != null)
                {
                    EditorUtility.SetDirty(obj);
                }
            }
        }
        public static void BoolFieldWithTooltip(Func<bool> getter, Action<bool> setter, string label, string tooltip)
        {
            BoolFieldWithTooltip(getter, setter, label, tooltip, null);
        }
        public static void BoolFieldWithTooltip(Func<bool> getter, Action<bool> setter, string label, string tooltip, UnityEngine.Object obj)
        {
            var value = getter();
            var newValue = GUILayout.Toggle(value, new GUIContent(label, tooltip));

            if (!value.Equals(newValue))
            {
                if (obj != null)
                {
                    Undo.RecordObject(obj, label);
                }

                setter(newValue);

                if (obj != null)
                {
                    EditorUtility.SetDirty(obj);
                }
            }
        }
    }
#endif

    public class LeiaUtils
    {
        public static void CopyCameraParameters(Camera sourceCamera, Camera targetCamera)
        {
            if (targetCamera == null)
            {
                return;
            }
            targetCamera.tag = sourceCamera.tag;
            targetCamera.clearFlags = sourceCamera.clearFlags;
            targetCamera.cullingMask = sourceCamera.cullingMask;
            targetCamera.depth = sourceCamera.depth;
            targetCamera.backgroundColor = sourceCamera.backgroundColor;
            targetCamera.orthographic = sourceCamera.orthographic;
            targetCamera.orthographicSize = sourceCamera.orthographicSize;
            targetCamera.fieldOfView = sourceCamera.fieldOfView;
            targetCamera.rect = sourceCamera.rect;
            targetCamera.nearClipPlane = sourceCamera.nearClipPlane;
            targetCamera.farClipPlane = sourceCamera.farClipPlane;
            targetCamera.allowHDR = sourceCamera.allowHDR;
            targetCamera.renderingPath = sourceCamera.renderingPath;
            targetCamera.useOcclusionCulling = sourceCamera.useOcclusionCulling;

#if LEIA_URP_DETECTED
#if UNITY_2019_3_OR_NEWER
            if (!RenderPipelineUtils.IsUnityRenderPipeline())
            {
                return;
            }

            UniversalAdditionalCameraData sourceURPData = sourceCamera.GetUniversalAdditionalCameraData();
            UniversalAdditionalCameraData targetURPData = targetCamera.GetUniversalAdditionalCameraData();

            LeiaDisplay displayComponentInParent = sourceCamera.GetComponentInParent<LeiaDisplay>();
            LeiaDisplay displayComponentInChildren = sourceCamera.GetComponentInChildren<LeiaDisplay>();

            if (displayComponentInParent != null)
            {
                targetURPData.SetRenderer(displayComponentInParent.scriptableRendererIndex);
            }
            else if (displayComponentInChildren != null)
            {
                targetURPData.SetRenderer(displayComponentInChildren.scriptableRendererIndex);
            }

            targetURPData.antialiasing = sourceURPData.antialiasing;
            targetURPData.antialiasingQuality = sourceURPData.antialiasingQuality;
            targetURPData.dithering = sourceURPData.dithering;
            targetURPData.renderPostProcessing = sourceURPData.renderPostProcessing;
            targetURPData.renderShadows = sourceURPData.renderShadows;
            targetURPData.requiresColorOption = sourceURPData.requiresColorOption;
            targetURPData.requiresColorTexture = sourceURPData.requiresColorTexture;
            targetURPData.requiresDepthOption = sourceURPData.requiresDepthOption;
            targetURPData.requiresDepthTexture = sourceURPData.requiresDepthTexture;
            targetURPData.stopNaN = sourceURPData.stopNaN;
            targetURPData.volumeLayerMask = sourceURPData.volumeLayerMask;
            targetURPData.renderType = sourceURPData.renderType;
#endif

#if UNITY_2020_2_OR_NEWER
            targetURPData.allowXRRendering = sourceURPData.allowXRRendering;
            targetURPData.volumeStack = sourceURPData.volumeStack;
#endif

#if UNITY_2022_2_OR_NEWER
            targetURPData.allowHDROutput = sourceURPData.allowHDROutput;
            targetURPData.resetHistory = sourceURPData.resetHistory;
            targetURPData.screenCoordScaleBias = sourceURPData.screenCoordScaleBias;
            targetURPData.screenSizeOverride = sourceURPData.screenSizeOverride;
            targetURPData.useScreenCoordOverride = sourceURPData.useScreenCoordOverride;
#endif

#if UNITY_2023_3_OR_NEWER
            targetURPData.history = sourceURPData.history;
#endif
#endif

#if LEIA_HDRP_DETECTED

            HDAdditionalCameraData sourceHDRPData = sourceCamera.GetComponent<HDAdditionalCameraData>();
            CopyComponent(sourceHDRPData, targetCamera.gameObject);
#endif
        }
        static T CopyComponent<T>(T original, GameObject destination) where T : Component
        {
            System.Type type = original.GetType();
            var dst = destination.GetComponent(type) as T;
            if (!dst) dst = destination.AddComponent(type) as T;
            var fields = type.GetFields();
            foreach (var field in fields)
            {
                if (field.IsStatic) continue;
                field.SetValue(dst, field.GetValue(original));
            }
            var props = type.GetProperties();
            foreach (var prop in props)
            {
                if (!prop.CanWrite || !prop.CanWrite || prop.Name == "name") continue;
                prop.SetValue(dst, prop.GetValue(original, null), null);
            }
            return dst as T;
        }
    }
}