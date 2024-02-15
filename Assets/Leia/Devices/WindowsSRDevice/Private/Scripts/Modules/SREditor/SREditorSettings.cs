/*!
 * Copyright (C) 2022  Dimenco
 *
 * This software has been provided under the Dimenco EULA. (End User License Agreement)
 * You can find the agreement at https://www.dimenco.eu/eula
 *
 * This source code is considered Protected Code under the definitions of the EULA.
 */

#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

// Asset inspector panel
[CustomEditor(typeof(SRProjectSettings))]
public class EditorProjectSettingsHidden : Editor 
{
    public override void OnInspectorGUI()
    {
        // Leave empty to create empty asset
    }
}

// Project settings section
public static class EditorProjectSettings
{
    [SettingsProvider]
    public static SettingsProvider CreateProjectLocationSettingsProvider()
    {
        SRUnity.SRUtility.Debug("EditorProjectSettings");

        SettingsProvider provider = new SettingsProvider("Project/Simulated Reality", SettingsScope.Project)
        {
            guiHandler = (searchContext) =>
            {
                EditorGUIUtility.labelWidth = 200;
#if !UNITY_EDITOR && PLATFORM_STANDALONE_WIN
                EditorGUILayout.LabelField("SRUnity " + SRUnity.PluginVersion.GetFullVersion());
#endif
                EditorGUILayout.Space();

                EditorGUI.BeginChangeCheck();

                SRProjectSettings.Instance.unityUnitsPerRealMeter = Mathf.Max(0.01f, EditorGUILayout.FloatField("Unity units per real world meter", SRProjectSettings.Instance.unityUnitsPerRealMeter));

                SRProjectSettings.Instance.allowStartWithoutSimulatedRealityRuntime = EditorGUILayout.Toggle("Allow start without SR runtime", SRProjectSettings.Instance.allowStartWithoutSimulatedRealityRuntime);

                EditorGUILayout.Space();

                SRProjectSettings.Instance.scaleType = (ESimulatedRealityScaleType)EditorGUILayout.EnumPopup("Scale type", SRProjectSettings.Instance.scaleType);
 
                if (SRProjectSettings.Instance.scaleType == ESimulatedRealityScaleType.Uniform)
                {
                    SRProjectSettings.Instance.intendedDisplaySize = EditorGUILayout.Vector2Field("Intended display size (cm)", SRProjectSettings.Instance.intendedDisplaySize);
                }

                EditorGUILayout.Space();
                SRProjectSettings.Instance.frameBufferFormat = (RenderTextureFormat)EditorGUILayout.EnumPopup("Framebuffer format", SRProjectSettings.Instance.frameBufferFormat);

                SRProjectSettings.Instance.renderResolution = Mathf.Max(0.1f, EditorGUILayout.FloatField("Render resolution multiplier", SRProjectSettings.Instance.renderResolution));

                if (EditorGUI.EndChangeCheck())
                {
                    SRProjectSettings.Instance.Save();
                }
            }
        };

        return provider;
    }
}

// Editor user settings section
public static class EditorUserSettings
{
    [SettingsProvider]
    public static SettingsProvider CreateProjectLocationSettingsProvider()
    {
        SRUnity.SRUtility.Debug("EditorUserSettings");

        SettingsProvider provider = new SettingsProvider("Preferences/Simulated Reality", SettingsScope.User)
        {
            guiHandler = (searchContext) =>
            {
                EditorGUIUtility.labelWidth = 170;
#if !UNITY_EDITOR && PLATFORM_STANDALONE_WIN
                EditorGUILayout.LabelField("SRUnity " + SRUnity.PluginVersion.GetFullVersion());
#endif
                EditorGUILayout.Space();

                EditorGUI.BeginChangeCheck();
                
                SRUserSettings.Instance.debugMode = EditorGUILayout.Toggle("Debug mode", SRUserSettings.Instance.debugMode);
                SRUserSettings.Instance.liveFrustum = EditorGUILayout.Toggle("Live frustum visualization", SRUserSettings.Instance.liveFrustum);
                SRUserSettings.Instance.liveHands = EditorGUILayout.Toggle("Editor hand visualization", SRUserSettings.Instance.liveHands);
                SRUserSettings.Instance.reportBorderBiolations = EditorGUILayout.Toggle("Report border violations", SRUserSettings.Instance.reportBorderBiolations);

                GUILayout.Space(20);
                EditorGUI.indentLevel++;

                foreach (SRDisplayDefinition displayDefinition in SRUserSettings.Instance.displayDefinitions)
                {
                    displayDefinition.name = EditorGUILayout.TextField("Name", displayDefinition.name);
                    displayDefinition.displaySize = EditorGUILayout.Vector2Field("Display size (cm)", displayDefinition.displaySize);
                    displayDefinition.viewingDistance = EditorGUILayout.FloatField("Viewing distance (cm)", displayDefinition.viewingDistance);
                    if (GUILayout.Button("Delete"))
                    {
                        SRUserSettings.Instance.displayDefinitions.Remove(displayDefinition);
                        break;
                    }
                    GUILayout.Space(20);
                }

                if (GUILayout.Button("Add new display"))
                {
                    SRUserSettings.Instance.displayDefinitions.Add(new SRDisplayDefinition());
                }

                EditorGUI.indentLevel--;

                if (EditorGUI.EndChangeCheck())
                {
                    SRUserSettings.Instance.Save();
                }
            }
        };

        return provider;
    }
}

#endif
