/*!
 * Copyright (C) 2022  Dimenco
 *
 * This software has been provided under the Dimenco EULA. (End User License Agreement)
 * You can find the agreement at https://www.dimenco.eu/eula
 *
 * This source code is considered Protected Code under the definitions of the EULA.
 */

using System;
using UnityEngine;
using UnityEditor;

public class SRCameraSettingsContainer : ISRSettingsInterface
{
    public SRCameraSettingsContainer(SimulatedRealityCameraSettings inParent)
    {
        parent = inParent;
    }

    public override float GetUnityUnitsPerRealMeter()
    {
        if (parent.overrideUnitsPerMeter)
        {
            return parent.unityUnitsPerRealMeter;
        }
        else
        {
            return SRProjectSettings.Instance.unityUnitsPerRealMeter;
        }
    }

    public override ESimulatedRealityScaleType GetScaleType()
    {
        if (parent.overrideScaleType)
        {
            return parent.scaleType;
        }
        else
        {
            return SRProjectSettings.Instance.scaleType;
        }
    }

    public override Vector2 GetIntendedDisplaySize()
    {
        if (parent.overrideIntendedDisplaySize)
        {
            return parent.intendedDisplaySize;
        }
        else
        {
            return SRProjectSettings.Instance.intendedDisplaySize;
        }
    }

    private SimulatedRealityCameraSettings parent = null;
}

[DisallowMultipleComponent]
[AddComponentMenu("Simulated Reality/Simulated Reality Camera Settings")]
// Component to handle SR rendering
public class SimulatedRealityCameraSettings : MonoBehaviour, ISRSettingsProvider
{
    public bool overrideUnitsPerMeter = false;
    public bool overrideScaleType = false;
    public bool overrideIntendedDisplaySize = false;

    public float unityUnitsPerRealMeter = 100;

    public ESimulatedRealityScaleType scaleType = ESimulatedRealityScaleType.Realistic;
    public Vector2 intendedDisplaySize = new Vector2(69, 39);


    private SRCameraSettingsContainer settingsContainer = null;
    public ISRSettingsInterface GetSettings()
    {
        if (settingsContainer == null)
        {
            settingsContainer = new SRCameraSettingsContainer(this);
        }

        return settingsContainer;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(SimulatedRealityCameraSettings))]
public class SimulatedRealityCameraSettingsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        SimulatedRealityCameraSettings cameraSettings = (SimulatedRealityCameraSettings)target;

        cameraSettings.overrideUnitsPerMeter = EditorGUILayout.ToggleLeft("Unity Units Per Real Meter", cameraSettings.overrideUnitsPerMeter);
        if (cameraSettings.overrideUnitsPerMeter)
        {
            cameraSettings.unityUnitsPerRealMeter = Math.Max(0.01f, EditorGUILayout.FloatField("", cameraSettings.unityUnitsPerRealMeter));
        }
        EditorGUILayout.Space();

        cameraSettings.overrideScaleType = EditorGUILayout.ToggleLeft("Scale Type", cameraSettings.overrideScaleType);
        if (cameraSettings.overrideScaleType)
        {
            cameraSettings.scaleType = (ESimulatedRealityScaleType)EditorGUILayout.EnumPopup("", cameraSettings.scaleType);
        }
        EditorGUILayout.Space();

        cameraSettings.overrideIntendedDisplaySize = EditorGUILayout.ToggleLeft("Intended Display Size", cameraSettings.overrideIntendedDisplaySize);
        if (cameraSettings.overrideIntendedDisplaySize)
        {
            cameraSettings.intendedDisplaySize = EditorGUILayout.Vector2Field("", cameraSettings.intendedDisplaySize);
        }
        EditorGUILayout.Space();
    }
}
#endif
