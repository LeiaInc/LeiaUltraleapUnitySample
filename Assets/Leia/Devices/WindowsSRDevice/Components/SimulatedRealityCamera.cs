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
using System.Linq;
using UnityEngine;
using UnityEditor;

using LeiaUnity;
#if !UNITY_EDITOR && PLATFORM_STANDALONE_WIN
using SimulatedReality;
#endif
using SRUnity;

[ExecuteInEditMode]
[AddComponentMenu("Simulated Reality/Simulated Reality Camera")]
// Component to handle SR rendering
public class SimulatedRealityCamera : MonoBehaviour
{
    [Header("Clearing")]

    public CameraClearFlags clearFlags = CameraClearFlags.Skybox;
    public Color backgroundColor = Color.black;

    [Header("Clipping")]

    [Tooltip("Near clipping plane distance in real world centimeters")]
    [Min(0.1f)]
    public float nearClipPlane = 10;

    [Tooltip("Far clipping plane distance in real world centimeters")]
    [Min(10.0f)]
    public float farClipPlane = 1000;

    [Header("Output")]

    public Rect viewport = new Rect(0, 0, 1, 1);
    public int depth = 0;

    [Header("Rendering")]

    public RenderingPath renderingPath = RenderingPath.UsePlayerSettings;
    public bool occlusionCulling = true;

    [Tooltip("Camera FOV to use when the Simulated Reality Runtime is not available")]
    public float fallbackFOV = 90;


    [Tooltip("Enable lookaround or show static 3D")]
    public bool enableLookaround = true;

    [Tooltip("Adjust the strength of the 3D effect")]
    [Rename("3D Strength")]
    [Min(0.0f)]
    public float ipdMultiplier = 1.0f;

    [Tooltip("These components will be replicated to the internal SR cameras")]
    public List<Component> replicatedComponents = new List<Component>();

    [Tooltip("Allow access to camera components")]
    [Header("Warning: if not used correctly, this setting can cause issues in the project.")]
    public bool exposeInternalComponents = false;

    private Camera[] cameraComponents = new Camera[2];
    private SRCompositor.SRFrameBuffer[] cameraFrameBuffers = new SRCompositor.SRFrameBuffer[2];


    LeiaDisplay _leiaDisplay;
    LeiaDisplay leiaDisplay
    {
        get
        {
            if (_leiaDisplay == null)
            {
                _leiaDisplay = FindObjectOfType<LeiaDisplay>();
            }
            if (_leiaDisplay == null)
            {
                Debug.LogError("SimulatedRealityCamera:: LeiaDisplay is not present in scene and is attempting to be accessed. Check call stack");
            }
            return _leiaDisplay;
        }
    }

    public void OnEnable()
    {
        SRUnity.SRUtility.Debug("SimulatedRealityCamera::OnEnable");
        Init();
        SRUnity.SRCompositor.OnCompositorChanged += OnCompositorChanged;
        SRUnity.SRCore.OnContextChanged += OnContextChanged;

        cameraFrameBuffers[0].Enabled = true;
        cameraFrameBuffers[1].Enabled = true;

#if UNITY_EDITOR
        EditorApplication.update += Update;
#endif
    }

    public void OnDisable()
    {
        SRUnity.SRUtility.Debug("SimulatedRealityCamera::OnDisable");
        SRUnity.SRCompositor.OnCompositorChanged -= OnCompositorChanged;
        SRUnity.SRCore.OnContextChanged -= OnContextChanged;

#if UNITY_EDITOR
        EditorApplication.update -= Update;
#endif

        // Release hidden camera hierarchy
        for (int i = 0; i < 2; i++)
        {
            if (cameraComponents[i] != null)
            {
                DestroyImmediate(cameraComponents[i].gameObject);
                cameraComponents[i] = null;
            }
        }

        cameraFrameBuffers[0].Enabled = false;
        cameraFrameBuffers[1].Enabled = false;
    }

    public void Awake()
    {
        SRUnity.SRUtility.Debug("SimulatedRealityCamera::Awake");
        Init();
    }

    public void Start()
    {
        SRUnity.SRUtility.Debug("SimulatedRealityCamera::Start");
        Init();
    }

    private void Init()
    {
        ConstructHierarchy();
        SetupCameraComponents();

        if (IsViewportValid() && IsLeiaValid()) UpdateProjection();
    }

    public void Update()
    {
        UpdateCameraSettings();
        UpdateViewports();

        ReplicateComponents();

        if (IsViewportValid() && IsLeiaValid()) UpdateProjection();
    }

    public void OnValidate()
    {
        // Update camera parameters when a setting has been changed
        SetupCameraComponents();

        if (cameraComponents[0] != null) UpdateComponentsVisibility(cameraComponents[0].gameObject, false);
        if (cameraComponents[1] != null) UpdateComponentsVisibility(cameraComponents[1].gameObject, false);

        ReplicateComponents();
    }

    public void UpdateComponentsVisibility(GameObject camera, bool SRlessMode)
    {
        if (SRUnity.SRCore.IsSimulatedRealityAvailable())
        {
            if (!exposeInternalComponents)
            {
                SRUnity.SRUtility.SetSrGameObjectVisibility(camera);
            }
            else
            {
                camera.hideFlags = HideFlags.None;
            }
        }
        else
        {
            if (SRlessMode) // Only use left camera in SR-less mode
            {
                camera.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;
            }
            else
            {
                camera.hideFlags = HideFlags.HideAndDontSave;
            }
        }
    }

    // Construct the (hidden) sub objects
    private void ConstructHierarchy()
    {
        String[] cameraNames;
        if (SRUnity.SRCore.IsSimulatedRealityAvailable())
        {
            cameraNames = new string[] { "SR_Camera_L", "SR_Camera_R" };
        }
        else
        {
            cameraNames = new string[] { "SR_Camera", "SR_Camera_R" };
        }

        for (int i = 0; i < 2; i++)
        {
            GameObject cameraObject = SRUnity.SRUtility.FindChildObject(gameObject, cameraNames[i]);
            if (cameraObject == null)
            {
                cameraObject = new GameObject();
                cameraObject.transform.parent = gameObject.transform;
            }

            cameraObject.name = cameraNames[i];
            cameraObject.transform.localRotation = Quaternion.identity;
            cameraObject.transform.localScale = Vector3.one;

            if (i == 0)
            {
                UpdateComponentsVisibility(cameraObject, true);
            }
            else
            {
                UpdateComponentsVisibility(cameraObject, false);
            }

            cameraComponents[i] = cameraObject.GetComponent<Camera>();
            if (cameraComponents[i] == null)
            {
                cameraComponents[i] = cameraObject.AddComponent<Camera>();
            }

            cameraFrameBuffers[i] = SRUnity.SRRender.Instance.GetCompositor().GetFrameBuffer(cameraComponents[i].GetInstanceID().ToString());
            cameraFrameBuffers[i].viewIndex = i;
            cameraFrameBuffers[i].Enabled = true;
        }

#if UNITY_EDITOR
        SREditorBorderViolations.SetupHierarchy(gameObject);
#endif

        // Create a hidden dummy camera. This is needed to allow some components to be added for replication.
        Camera dummyCamera = transform.gameObject.GetComponent<Camera>();
        if (dummyCamera == null)
        {
            dummyCamera = transform.gameObject.AddComponent<Camera>();
        }
        dummyCamera.enabled = false;
        dummyCamera.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy | HideFlags.HideInInspector;

        ReplicateComponents();
    }

    public bool IsViewportValid()
    {
        if (viewport.width <= 0 || viewport.height <= 0)
        {
            return false;
        }

        return true;
    }
    public bool IsLeiaValid()
    {
        if (leiaDisplay == null)
        {
            return false;
        }
        if(leiaDisplay.GetViewCount() != 2)
        {
            return false;
        }
        return true;
    }
    // Returns the screen halfsize scaled to the 'viewport' field
    public Vector2 GetScaledScreenHalfSize()
    {
        float cmToUnityScale = ISRSettingsInterface.GetProjectSettings(this).GetScaleSrCmToUnity();
        return (SRUnity.SRCore.Instance.getPhysicalSize() / 2.0f * cmToUnityScale) * new Vector2(viewport.width, viewport.height);
    }

    // Adjust the eye positions according to the 'viewport' field
    public void CalculateCorrectedEyeCoordinates(ref Vector3[] Eyes)
    {
        float cmToUnityScale = ISRSettingsInterface.GetProjectSettings(this).GetScaleSrCmToUnity();
        Vector2 scaledScreenSize = SRUnity.SRCore.Instance.getPhysicalSize() * cmToUnityScale;
        Vector2 viewportOffset = (new Vector2(viewport.xMin, viewport.yMin) + new Vector2(viewport.xMax, viewport.yMax)) / 2 - new Vector2(.5f, .5f);
        Vector2 viewOffset = viewportOffset * scaledScreenSize;

        for (int i = 0; i < Eyes.Length; i++)
        {
            Eyes[i] -= new Vector3(viewOffset.x, viewOffset.y, 0);
        }
    }

    // Setup camera object settings
    private void SetupCameraComponents()
    {
        if (cameraComponents[0] == null || cameraComponents[1] == null) return;

        foreach (Camera camera in cameraComponents)
        {
            camera.transform.position = gameObject.transform.position;
        }

        UpdateCameraSettings();

        UpdateViewports();
    }

    private void UpdateCameraSettings()
    {
        foreach (Camera camera in cameraComponents)
        {
            camera.depth = depth;
            camera.clearFlags = clearFlags;
            camera.backgroundColor = backgroundColor;
            camera.useOcclusionCulling = occlusionCulling;
            camera.renderingPath = renderingPath;
        }
    }

    private void UpdateViewports()
    {
        if (cameraComponents != null && cameraComponents[0] != null && cameraComponents[1] != null)
        {
            if (SRUnity.SRCore.IsSimulatedRealityAvailable())
            {
                cameraComponents[0].enabled = true;
                cameraComponents[1].enabled = true;
                cameraFrameBuffers[0].screenRect = new Rect(viewport.x, viewport.y, viewport.width, viewport.height);
                cameraFrameBuffers[1].screenRect = new Rect(viewport.x, viewport.y, viewport.width, viewport.height);
                cameraFrameBuffers[0].Update();
                cameraFrameBuffers[1].Update();
                cameraComponents[0].targetTexture = cameraFrameBuffers[0].frameBuffer;
                cameraComponents[1].targetTexture = cameraFrameBuffers[1].frameBuffer;
            }
            else
            {
                cameraComponents[0].enabled = true;
                cameraComponents[1].enabled = false;
                cameraComponents[0].rect = new Rect(viewport.x, viewport.y, viewport.width, viewport.height);
            }
        }
    }

    // Debug draw frustum and eyes
    public void OnDrawGizmos()
    {
        if (SRUnity.SRCore.IsSimulatedRealityAvailable())
        {
            bool liveFrustum = SRUserSettings.Instance.liveFrustum;
#if UNITY_EDITOR
            liveFrustum = liveFrustum || EditorApplication.isPlaying;
#endif

            Vector3[] Eyes;
            if (liveFrustum && enableLookaround)
            {
                Eyes = SRUnity.SREyes.Instance.GetEyes(ISRSettingsInterface.GetProjectSettings(this));
            }
            else
            {
                Eyes = SRUnity.SREyes.Instance.GetEyes(ISRSettingsInterface.GetProjectSettings(this));
                Vector3 defaultHeadPosition = SRUnity.SREyes.Instance.GetDefaultEyePosition(ISRSettingsInterface.GetProjectSettings(this));
                Vector3 trackedHeadPosition = (Eyes[0] + Eyes[1]) / 2;
                Vector3 headOffSet = defaultHeadPosition - trackedHeadPosition;

                Eyes[0] += headOffSet;
                Eyes[1] += headOffSet;
            }

            CalculateCorrectedEyeCoordinates(ref Eyes);

            if (Eyes.Length != 2) return;
            if (Eyes[0] == Vector3.zero || Eyes[1] == Vector3.zero) return;

            // Calculate frustum vertices
            Vector2 screenHalfSize = GetScaledScreenHalfSize();
            Vector3 tl = new Vector3(-screenHalfSize.x, screenHalfSize.y, 0);
            Vector3 tr = new Vector3(screenHalfSize.x, screenHalfSize.y, 0);
            Vector3 bl = new Vector3(-screenHalfSize.x, -screenHalfSize.y, 0);
            Vector3 br = new Vector3(screenHalfSize.x, -screenHalfSize.y, 0);
            Vector3 Head = (Eyes[0] + Eyes[1]) * 0.5f;
            float distanceFromScreen = Mathf.Abs(Head.z);

            Transform transform = gameObject.transform;

            // Transform vertices from local to world space
            tl = transform.position + transform.rotation * tl;
            tr = transform.position + transform.rotation * tr;
            bl = transform.position + transform.rotation * bl;
            br = transform.position + transform.rotation * br;
            Head = transform.position + transform.rotation * Head;

            // Render frustum debug lines
            Gizmos.color = Color.green;

            Gizmos.color = Color.red;
            Gizmos.DrawLine(tl, tr);
            Gizmos.DrawLine(tr, br);
            Gizmos.DrawLine(br, bl);
            Gizmos.DrawLine(bl, tl);

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(Head, tr);
            Gizmos.DrawLine(Head, br);
            Gizmos.DrawLine(Head, bl);
            Gizmos.DrawLine(Head, tl);

            // Render near clipping plane
            float nearPlaneDistance = (nearClipPlane * ISRSettingsInterface.GetProjectSettings(this).GetScaleSrCmToUnity()) / distanceFromScreen;
            tl = Vector3.Lerp(Head, tl, nearPlaneDistance);
            tr = Vector3.Lerp(Head, tr, nearPlaneDistance);
            bl = Vector3.Lerp(Head, bl, nearPlaneDistance);
            br = Vector3.Lerp(Head, br, nearPlaneDistance);

            Gizmos.color = new Color(1, 0, 0, 0.3f);
            Gizmos.DrawLine(tl, tr);
            Gizmos.DrawLine(tr, br);
            Gizmos.DrawLine(br, bl);
            Gizmos.DrawLine(bl, tl);
        }
    }

    // Update the camera projection matrices
    private void UpdateProjection()
    {
        if (SRUnity.SRCore.IsSimulatedRealityAvailable())
        {
            Vector2 screenHalfSize = GetScaledScreenHalfSize();

            Vector3[] Eyes;
#if UNITY_EDITOR
            if ((SRUserSettings.Instance.liveFrustum || EditorApplication.isPlaying) && enableLookaround)
            {
                Eyes = SRUnity.SREyes.Instance.GetEyes(ISRSettingsInterface.GetProjectSettings(this));
            }
            else
            {
                Eyes = SRUnity.SREyes.Instance.GetEyes(ISRSettingsInterface.GetProjectSettings(this));
                Vector3 defaultHeadPosition = SRUnity.SREyes.Instance.GetDefaultEyePosition(ISRSettingsInterface.GetProjectSettings(this));
                Vector3 trackedHeadPosition = (Eyes[0] + Eyes[1]) / 2;
                Vector3 headOffSet = defaultHeadPosition - trackedHeadPosition;

                Eyes[0] += headOffSet;
                Eyes[1] += headOffSet;
            }
#else
            if (enableLookaround)
            {
                Eyes = SRUnity.SREyes.Instance.GetEyes(ISRSettingsInterface.GetProjectSettings(this));
            }
            else
            {
                Eyes = SRUnity.SREyes.Instance.GetEyes(ISRSettingsInterface.GetProjectSettings(this));
                Vector3 defaultHeadPosition = SRUnity.SREyes.Instance.GetDefaultEyePosition(ISRSettingsInterface.GetProjectSettings(this));
                Vector3 trackedHeadPosition = (Eyes[0] + Eyes[1]) / 2;
                Vector3 headOffSet = defaultHeadPosition - trackedHeadPosition;

                Eyes[0] += headOffSet;
                Eyes[1] += headOffSet;
            }
#endif

            CalculateCorrectedEyeCoordinates(ref Eyes);

            if (Eyes.Length != 2) return;
            if (Eyes[0] == Vector3.zero || Eyes[1] == Vector3.zero) return;

            Vector3 Center = (Eyes[0] + Eyes[1]) / 2;
            Vector3 HalfIPD = Eyes[0] - Center;

            ipdMultiplier = Math.Max(0.0f, ipdMultiplier);

            if (!SRUnity.SrRenderModeHint.ShouldRender3D())
            {
                Eyes[0] = Center;
                Eyes[1] = Center;
            }
            else
            {
                Eyes[0] = Center + HalfIPD * ipdMultiplier;
                Eyes[1] = Center - HalfIPD * ipdMultiplier;
            }

            float cmToUnityScale = ISRSettingsInterface.GetProjectSettings(this).GetScaleSrCmToUnity();

            for (int i = 0; i < Eyes.Length; i++)
            {

                //Sync SR views to Leia views
                cameraComponents[i].transform.position = leiaDisplay.GetEyeCamera(i).transform.position;
                cameraComponents[i].transform.rotation = leiaDisplay.GetEyeCamera(i).transform.rotation;
                cameraComponents[i].projectionMatrix = leiaDisplay.GetEyeCamera(i).projectionMatrix;

                LeiaUtils.CopyCameraParameters(leiaDisplay.GetEyeCamera(i), cameraComponents[i]);

                //Standard SR behavior
                //cameraComponents[i].transform.position = transform.position + transform.rotation * Eyes[i];
                //cameraComponents[i].projectionMatrix = SRUnity.SRUtility.CalculateProjection(screenHalfSize, Eyes[i], nearClipPlane * cmToUnityScale, farClipPlane * cmToUnityScale);
            }

            // Notify editor to redraw the views when not in play-mode
#if UNITY_EDITOR
            EditorApplication.QueuePlayerLoopUpdate();
#endif
        }
        else
        {
            cameraComponents[0].nearClipPlane = nearClipPlane;
            cameraComponents[0].farClipPlane = farClipPlane;
            cameraComponents[0].fieldOfView = fallbackFOV;
        }
    }

    private void OnContextChanged(SRUnity.SRContextChangeReason contextChangeReason)
    {
        SetupCameraComponents();
    }

    // Attached or detach this camera when the SRWeaver is created or destroyed
    private void OnCompositorChanged()
    {
        SetupCameraComponents();
    }

    // Copy all components specified in replicatedComponents to the internal camera objects
    private void ReplicateComponents()
    {
        if (cameraComponents == null) return;
        if (replicatedComponents == null) return;
        if (replicatedComponents.Count == 0) return;

        for (int cameraIndex = 0; cameraIndex < 2; cameraIndex++)
        {
            if (cameraComponents[cameraIndex] != null)
            {
                GameObject cameraObject = cameraComponents[cameraIndex].transform.gameObject;

                List<Component> staleComponents = cameraObject.GetComponents<Component>().ToList();
                staleComponents.Remove(cameraComponents[cameraIndex]);
                staleComponents.Remove(cameraComponents[cameraIndex].transform);

                foreach (Component component in replicatedComponents)
                {
                    if (component == null) continue;
                    
                    System.Type type = component.GetType();
                    Component targetComponent = cameraObject.GetComponent(type);
                    if (targetComponent == null)
                    {
                        targetComponent = cameraObject.AddComponent(type);
                    }

                    staleComponents.Remove(targetComponent);

                    targetComponent.hideFlags = HideFlags.DontSave;

                    System.Reflection.FieldInfo[] fields = type.GetFields();
                    foreach (System.Reflection.FieldInfo field in fields)
                    {
                        field.SetValue(targetComponent, field.GetValue(component));
                    }
                }

                foreach (Component staleComponent in staleComponents)
                {
                    DestroyImmediate(staleComponent);
                }
            }
        }
    }

    // Map a value from one range to another
    private float GetMappedRangeValueClamped(Vector2 inputRange, Vector2 outputRange, float value)
    {
        float percentage = Mathf.InverseLerp(inputRange.x, inputRange.y, value);
        float clampedPct = Mathf.Clamp(percentage, 0, 1);

        return Mathf.Lerp(outputRange.x, outputRange.y, clampedPct);
    }

    public Vector3 ProjectScreenPositionToWorld(Vector2 screenPosition)
    {
        //Width and height of the camera viewport in pixels
        Vector2 viewportSizePixels = SRUnity.SRCore.Instance.getResolution() * new Vector2(viewport.width, viewport.height);

        Vector2 halfDisplaySizeCM = SRUnity.SRCore.Instance.getPhysicalSize() / 2;

        float x = GetMappedRangeValueClamped(new Vector2(0, viewportSizePixels.x), new Vector2(-halfDisplaySizeCM.x, halfDisplaySizeCM.x), screenPosition.x);

        float y = GetMappedRangeValueClamped(new Vector2(0, viewportSizePixels.y), new Vector2(-halfDisplaySizeCM.y, halfDisplaySizeCM.y), screenPosition.y);
        Vector3 worldPosition = new Vector3(x, y, 0);

        return transform.TransformPoint(worldPosition);
    }

    // Get the world position of the mouse cursor
    public Vector3 GetMouseWorldPosition()
    {
        return ProjectScreenPositionToWorld(new Vector2(Input.mousePosition.x, Input.mousePosition.y));
    }

    // Get a ray that starts a the users eye and points towards the mouse
    public void GetHeadToMouseRay(out Vector3 start, out Vector3 direction)
    {
        Vector3[] eyes = SRUnity.SREyes.Instance.GetEyes(ISRSettingsInterface.GetProjectSettings(this));
        Vector3 eyeCenter = (eyes[0] + eyes[1]) / 2;

        start = transform.TransformPoint(eyeCenter);
        Vector3 mouseWorldPosition = GetMouseWorldPosition();
        direction = (mouseWorldPosition - start).normalized;
    }
}
