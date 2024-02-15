/*!
 * Copyright (C) 2022  Dimenco
 *
 * This software has been provided under the Dimenco EULA. (End User License Agreement)
 * You can find the agreement at https://www.dimenco.eu/eula
 *
 * This source code is considered Protected Code under the definitions of the EULA.
 */

using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEditor;
using System.Reflection;

public enum ESimulatedRealityScaleType
{
    Realistic,
    Uniform
};

public abstract class ISRSettingsInterface
{
    public static ISRSettingsInterface GetProjectSettings(SimulatedRealityCamera camera)
    {
        if (camera != null)
        {
            SimulatedRealityCameraSettings settingsComponent =
                camera.GetComponentInParent<SimulatedRealityCameraSettings>();
            if (settingsComponent != null)
            {
                return settingsComponent.GetSettings();
            }
        }

        return SRProjectSettings.Instance.GetSettings();
    }

    public abstract float GetUnityUnitsPerRealMeter();
    public abstract ESimulatedRealityScaleType GetScaleType();
    public abstract Vector2 GetIntendedDisplaySize();

    private float srToMeters = 0.01f;

    // Settings utility functions
    public static float GetScaleForIntendedDisplaySize(Vector2 size, Vector2 intended)
    {
        return Math.Min(intended.x / size.x, intended.y / size.y);
    }

    public float GetScaleTypeResult(Vector2 displaySize)
    {
        switch (GetScaleType())
        {
            case ESimulatedRealityScaleType.Realistic:
            {
                return 1.0f;
            }
            case ESimulatedRealityScaleType.Uniform:
            {
                return GetScaleForIntendedDisplaySize(displaySize, GetIntendedDisplaySize());
            }
        }

        return 1;
    }

    public float GetScaleSrMetersToUnity(Vector2 displaySize)
    {
        return GetUnityUnitsPerRealMeter() * GetScaleTypeResult(displaySize);
    }

    public float GetScaleSrCmToUnity()
    {
        return GetScaleSrMetersToUnity(SRUnity.SRCore.Instance.getPhysicalSize()) * srToMeters;
    }

    public float GetScaleSrCmToUnity(Vector2 displaySize)
    {
        return GetScaleSrMetersToUnity(displaySize) * srToMeters;
    }

    public float GetScaleUnityToSrMeters()
    {
        return 1.0f / GetScaleSrMetersToUnity(SRUnity.SRCore.Instance.getPhysicalSize());
    }
}

public class SRProjectSettingsContainer : ISRSettingsInterface 
{
    public SRProjectSettingsContainer(SRProjectSettings inParent)
    {
        parent = inParent;
    }

    public override float GetUnityUnitsPerRealMeter()
    {
        return parent.unityUnitsPerRealMeter;
    }

    public override ESimulatedRealityScaleType GetScaleType()
    {
        return parent.scaleType;
    }

    public override Vector2 GetIntendedDisplaySize()
    {
        return parent.intendedDisplaySize;
    }

    private SRProjectSettings parent = null;
}

public interface ISRSettingsProvider
{
    ISRSettingsInterface GetSettings();
};

// Class to handle project related settings. Should be saved as an asset in 'Assets/Resources'.
public class SRProjectSettings : ScriptableObject, ISRSettingsProvider
{
    // Settings parameters
    [Min(0.01f)]
    public float unityUnitsPerRealMeter = 100;

    public ESimulatedRealityScaleType scaleType = ESimulatedRealityScaleType.Realistic;
    public Vector2 intendedDisplaySize = new Vector2(69, 39);

    public bool allowStartWithoutSimulatedRealityRuntime = false;

    public RenderTextureFormat frameBufferFormat = RenderTextureFormat.Default;

    [Min(0.01f)]
    public float renderResolution = 1.0f;

    public void Save()
    {
#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
        OnProjectSettingsChanged?.Invoke();
#endif
    }

    public delegate void OnProjectSettingsChangedDelegate();
    public static event OnProjectSettingsChangedDelegate OnProjectSettingsChanged;

    // Singleton interface
    private static SRProjectSettings instance_;
    public static SRProjectSettings Instance 
    {
        get
        {
            // Obtain all settings asset from AssetDatabase or Resources
            if (instance_ == null)
            {
#if UNITY_EDITOR
                string[] settingsAssets = AssetDatabase.FindAssets("t:" + MethodBase.GetCurrentMethod().DeclaringType.Name);

                for (int i = 0; i < settingsAssets.Length; i++)
                {
                    settingsAssets[i] = AssetDatabase.GUIDToAssetPath(settingsAssets[i]);

                    if (!settingsAssets[i].ToLower().Contains("assets/resources"))
                    {
                        Debug.LogWarning("SimulatedReality settings asset should be saved in \"Assets\\Resources\": " + settingsAssets[i]);
                    }
                }
#else
                UnityEngine.Object[] settingsAssets = Resources.LoadAll("", typeof(SRProjectSettings));
#endif

                if (settingsAssets.Length > 1)
                {
#if UNITY_EDITOR
                    Debug.LogWarning("Multiple SimulatedReality settings assets were found: " + String.Join(", ", settingsAssets));
#else
                    Debug.LogWarning("Multiple SimulatedReality settings assets were found");
#endif
                }

                if (settingsAssets.Length > 0)
                {
#if UNITY_EDITOR
                    instance_ = (SRProjectSettings)AssetDatabase.LoadAssetAtPath(settingsAssets[0], typeof(SRProjectSettings));
#else
                    instance_ = (SRProjectSettings)settingsAssets[0];
#endif
                }
            }

            // Create new asset if none was found or could be loaded
            if (instance_ == null)
            {
                instance_ = ScriptableObject.CreateInstance<SRProjectSettings>();
#if UNITY_EDITOR
                AssetDatabase.CreateFolder("Assets", "Resources");
                AssetDatabase.CreateAsset(instance_, "Assets/Resources/SRProjectSettings.asset");
#else
                Debug.LogWarning("No SRProjectSettings asset found in Resources. Using default settings.");
#endif
            }

            return instance_;
        }
    }

    private SRProjectSettingsContainer settingsContainer = null;
    public ISRSettingsInterface GetSettings()
    {
        if (settingsContainer == null)
        {
            settingsContainer = new SRProjectSettingsContainer(this);
        }

        return settingsContainer;
    }
}
