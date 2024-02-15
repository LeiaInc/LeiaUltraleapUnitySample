/*!
 * Copyright (C) 2022  Dimenco
 *
 * This software has been provided under the Dimenco EULA. (End User License Agreement)
 * You can find the agreement at https://www.dimenco.eu/eula
 *
 * This source code is considered Protected Code under the definitions of the EULA.
 */

#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

// SR Device previews window
public class SREditorDevicePreviews : EditorWindow
{
    public void OnEnable()
    {
        titleContent.text = "SR Device Previews";
        titleContent.tooltip = "This window wil assist the SRUnity plugin users understand how a scene will look on various devices.";
        titleContent.image = SRUnity.SRUtility.LoadPluginAsset<Texture2D>("DeviceIcon");
        minSize = new Vector2(250, 400);

        RenderViews(EditorApplication.timeSinceStartup);

        SRProjectSettings.OnProjectSettingsChanged += OnProjectSettingsChanged;
    }

    public void OnDisable()
    {
        SRProjectSettings.OnProjectSettingsChanged -= OnProjectSettingsChanged;
    }

    private class PreviewInfo
    {
        public SRDisplayDefinition definition;
        public Camera previewCamera = null;
        public RenderTexture framebuffer = null;
    }
    private List<PreviewInfo> previewCaptures = new List<PreviewInfo>();

    private List<String> viewpointNames = new List<String>();
    private List<SimulatedRealityCamera> viewpointCameras = new List<SimulatedRealityCamera>();

    private int selectedViewIndex = 0;

    private float availableWidth = 100;
    private float lastRenderedAvailableWidth = 0;

    private Vector2 scrollPosition = Vector2.zero;

    private bool shouldRedraw = true;

    private Vector3 lastViewpointPosition = Vector3.zero;
    private Quaternion lastViewpointRotation = Quaternion.identity;

    private void OnGUI()
    {
        EditorGUI.BeginChangeCheck();
        selectedViewIndex = EditorGUILayout.Popup(selectedViewIndex, viewpointNames.ToArray()); // Viewpont selection
        if (EditorGUI.EndChangeCheck())
        {
            shouldRedraw = true;
        }

        // Dummy layout element to obtain the width for scrollview width. Has to be calculated in all events
        Rect rect = GUILayoutUtility.GetRect(new GUIContent("Dummy"), GUIStyle.none, new GUILayoutOption[]{ GUILayout.ExpandWidth(true) });
        if (Event.current.type == EventType.Repaint) // Only valid during Repaint event
        {
            availableWidth = Mathf.Clamp(rect.width - 40, 100, 800);
        }

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        foreach (PreviewInfo info in previewCaptures)
        {
            GUILayout.Space(20);
            GUILayout.Label(info.definition.name);
            GUILayout.Box(info.framebuffer, new GUILayoutOption[]{ GUILayout.ExpandWidth(true) });
        }

        EditorGUILayout.EndScrollView();
    }

    private double lastUpdate = 0;
    public void Update()
    {
        // Force update every 2 seconds
        double now = EditorApplication.timeSinceStartup;
        if (now - lastUpdate > 2)
        {
            shouldRedraw = true;
        }

        // Update if window is resided
        if (Mathf.Abs(lastRenderedAvailableWidth - availableWidth) > 10)
        {
            shouldRedraw = true;
        }

        // Update if viewpoint has moved
        Vector3 position;
        Quaternion rotation;
        GetViewpointTransformation(out position, out rotation);
        if (lastViewpointPosition != position || lastViewpointRotation != rotation)
        {
            shouldRedraw = true;
        }

        if (shouldRedraw)
        {
            shouldRedraw = false;
            RenderViews(EditorApplication.timeSinceStartup);
            Repaint();
        }
    }

    private void OnProjectSettingsChanged()
    {
        shouldRedraw = true;
    }

    private void UpdateViewpointList()
    {
        SimulatedRealityCamera[] cameraComponents = GameObject.FindObjectsOfType<SimulatedRealityCamera>();

        viewpointNames.Clear();
        viewpointCameras.Clear();

        // Default 'scene view' camera
        viewpointNames.Add("[Scene view]");
        viewpointCameras.Add(null);

        foreach (SimulatedRealityCamera camera in cameraComponents)
        {
            viewpointNames.Add(camera.gameObject.name);
            viewpointCameras.Add(camera);
        }

        if (selectedViewIndex >= viewpointNames.Count)
        {
            selectedViewIndex = 0;
        }
    }

    private void UpdatePreviewData()
    {
        while(true)
        {
            if (previewCaptures.Count == SRUserSettings.Instance.displayDefinitions.Count)
            {
                break;
            }
            else if (previewCaptures.Count < SRUserSettings.Instance.displayDefinitions.Count)
            {
                previewCaptures.Add(new PreviewInfo());
            }
            else if (previewCaptures.Count > SRUserSettings.Instance.displayDefinitions.Count)
            {
                previewCaptures.RemoveAt(0);
            }
        }

        for (int i = 0; i < previewCaptures.Count; i++)
        {
            previewCaptures[i].definition = SRUserSettings.Instance.displayDefinitions[i];

            float displayAspect = previewCaptures[i].definition.displaySize.y / previewCaptures[i].definition.displaySize.x;
            Vector2Int textureSize = new Vector2Int((int)availableWidth, (int)(availableWidth * displayAspect));

            if (previewCaptures[i].framebuffer != null && (previewCaptures[i].framebuffer.width != textureSize.x || previewCaptures[i].framebuffer.height != textureSize.y))
            {
                previewCaptures[i].framebuffer = null;
            }

            if (previewCaptures[i].framebuffer == null && textureSize.x > 0 && textureSize.y > 0)
            {
                RenderTextureDescriptor frameBufferDesc = new RenderTextureDescriptor(textureSize.x, textureSize.y);
                frameBufferDesc.depthBufferBits = 24;
                previewCaptures[i].framebuffer = new RenderTexture(frameBufferDesc);
            }
        }

        UpdatePreviewHierarchy();
    }

    private void UpdatePreviewHierarchy()
    {
        String cameraObjectName = "SRDevicePreviewCapturer";
        GameObject cameraObject = SRUnity.SRUtility.FindChildObject(SRUnity.SystemHandler.Instance.gameObject, cameraObjectName);
        if (cameraObject == null)
        {
            cameraObject = new GameObject();
        }
        cameraObject.name = cameraObjectName;
        cameraObject.transform.parent = SRUnity.SystemHandler.Instance.transform;

        SRUnity.SRUtility.SetSrGameObjectVisibility(cameraObject);

        Camera[] cameraComponents = null;
        while(true)
        {
            cameraComponents = cameraObject.transform.GetComponentsInChildren<Camera>();

            if (cameraComponents.Length == previewCaptures.Count)
            {
                break;
            }
            else if (cameraComponents.Length < previewCaptures.Count)
            {
                GameObject child = new GameObject();
                child.transform.parent = cameraObject.transform;
                child.AddComponent<Camera>();
                SRUnity.SRUtility.SetSrGameObjectVisibility(child);
            }
            else if (cameraComponents.Length > previewCaptures.Count)
            {
                UnityEngine.Object.Destroy(cameraComponents[0].gameObject);
            }
        }

        for (int i = 0; i < previewCaptures.Count; i++)
        {
            cameraComponents[i].gameObject.name = previewCaptures[i].definition.name;
            cameraComponents[i].enabled = false;
            cameraComponents[i].targetTexture = previewCaptures[i].framebuffer;
            previewCaptures[i].previewCamera = cameraComponents[i];
        }
    }

    private void RenderViews(double now)
    {
        if (SRUnity.SystemHandler.Instance == null) return;

        lastUpdate = now;
        lastRenderedAvailableWidth = availableWidth;

        UpdateViewpointList();
        UpdatePreviewData();

        if (viewpointCameras.Count == 0 || previewCaptures.Count == 0) return;

        Vector3 position;
        Quaternion rotation;
        GetViewpointTransformation(out position, out rotation);

        lastViewpointPosition = position;
        lastViewpointRotation = rotation;

        foreach (PreviewInfo info in previewCaptures)
        {
            if (info.framebuffer == null || info.definition.viewingDistance <= 0) continue;

            float cmToUnityScale = ISRSettingsInterface.GetProjectSettings(null).GetScaleSrCmToUnity(info.definition.displaySize);
                
            info.previewCamera.transform.position = position + rotation * new Vector3(0, 0, -info.definition.viewingDistance * cmToUnityScale);
            info.previewCamera.transform.rotation = rotation;

            info.previewCamera.projectionMatrix = SRUnity.SRUtility.CalculateProjection(info.definition.displaySize / 2 * cmToUnityScale, new Vector3(0, 0, -info.definition.viewingDistance * cmToUnityScale), 0.01f, 9999.0f);
            info.previewCamera.Render();
        }
    }

    private void GetViewpointTransformation(out Vector3 position, out Quaternion rotation)
    {
        if (viewpointCameras.Count == 0) 
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
            return;
        }

        if (viewpointCameras[selectedViewIndex] == null)
        {
            if (SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.camera != null && SceneView.lastActiveSceneView.camera.transform != null)
            {
                position = SceneView.lastActiveSceneView.camera.transform.position;
                rotation = SceneView.lastActiveSceneView.camera.transform.rotation;
            }
            else
            {
                position = Vector3.zero;
                rotation = Quaternion.identity;
            }
        }
        else
        {
            position = viewpointCameras[selectedViewIndex].transform.position;
            rotation = viewpointCameras[selectedViewIndex].transform.rotation;
        }
    }

    [MenuItem("Window/Simulated Reality/SR Device Previews")]
    private static void Init()
    {
        // Get existing open window or if none, make a new one:
        SREditorDevicePreviews window = (SREditorDevicePreviews)EditorWindow.GetWindow(typeof(SREditorDevicePreviews));
        window.Show();
    }

}

#endif
