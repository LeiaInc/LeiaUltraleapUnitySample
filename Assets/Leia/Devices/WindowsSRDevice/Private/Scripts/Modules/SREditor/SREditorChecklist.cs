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

// SR Checklist window
public class SREditorChecklist : EditorWindow
{
    public Texture imgDocs, imgAutoFix, imgCorrect, imgError, imgInfo;

    private struct ChecklistEntry
    {
#pragma warning disable CS0649 // Hide 'Unused variable' warnings
        public String name;
        public String tooltip;

        public String supportURL;
        public String supportURLTooltip;

        public Func<bool> validationCheck;
        public String validationCheckTooltip;

        public Action autoFix;
        public String autoFixTooltip;
#pragma warning restore CS0649
    };

    private static List<ChecklistEntry> settingsEntries = new List<ChecklistEntry>()
    {
        new ChecklistEntry 
        {
            name = "Build Settings",
            tooltip = "The project should be build as x64 Standalone",
            supportURL = "https://docs.unity3d.com/Manual/BuildSettings.html",
            supportURLTooltip = "Open manual",
            validationCheck = ()=>{
                return EditorUserBuildSettings.selectedBuildTargetGroup == BuildTargetGroup.Standalone && EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows64; 
            }, 
            autoFix = ()=>{
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
            }
        },
        new ChecklistEntry 
        {
            name = "Scripting backend",
            supportURL = "https://docs.unity3d.com/Manual/scripting-backends.html",
            supportURLTooltip = "Open manual", 
            validationCheck = ()=>{
                return PlayerSettings.GetScriptingBackend(EditorUserBuildSettings.selectedBuildTargetGroup) == ScriptingImplementation.Mono2x;
            },
            autoFix = ()=>{ 
                PlayerSettings.SetScriptingBackend(EditorUserBuildSettings.selectedBuildTargetGroup, ScriptingImplementation.Mono2x);
            }
        }
    };

    public void OnEnable()
    {
        imgCorrect = SRUnity.SRUtility.LoadPluginAsset<Texture2D>("CheckMark");
        imgError = SRUnity.SRUtility.LoadPluginAsset<Texture2D>("CheckError");
        imgInfo = SRUnity.SRUtility.LoadPluginAsset<Texture2D>("CheckInfo");
        imgAutoFix = SRUnity.SRUtility.LoadPluginAsset<Texture2D>("CheckFix");
        imgDocs = SRUnity.SRUtility.LoadPluginAsset<Texture2D>("CheckDocs");
        titleContent.text = "SR Checklist";
        titleContent.tooltip = "This window wil assist the SRUnity plugin users in setting the unity settings, making sure a build can be made.";
        titleContent.image = SRUnity.SRUtility.LoadPluginAsset<Texture2D>("CheckIcon");
        minSize = new Vector2(250, 400);
    }

    private GUILayoutOption[] imgSize = new GUILayoutOption[] { GUILayout.Width(32), GUILayout.Height(32) };

    private void OnGUI()
    {
        foreach (ChecklistEntry entry in settingsEntries)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent(entry.name, entry.tooltip));

            // Support URL button
            if (entry.supportURL != null && entry.supportURL.Length > 0)
            {
                if (GUILayout.Button(new GUIContent(){ image = imgDocs, tooltip = entry.supportURLTooltip }, imgSize))
                {
                    Application.OpenURL(entry.supportURL);
                }
            }

            bool canValidate = entry.validationCheck != null;
            bool entryValid = false;
            if (canValidate) entryValid = entry.validationCheck.Invoke();

            // Autofix button
            if (entry.autoFix != null)
            {
                GUI.enabled = !entryValid;
                if (GUILayout.Button(new GUIContent(){ image = imgAutoFix, tooltip = entry.autoFixTooltip }, imgSize))
                {
                    entry.autoFix.Invoke();
                }
                GUI.enabled = true;
            }

            // Status logo
            Texture tex = null;
            if (entryValid)
            {
                tex = imgCorrect;
            }
            else if (canValidate)
            {
                tex = imgError;
            }
            else
            {
                tex = imgInfo;
            }
            Rect rect = GUILayoutUtility.GetRect(new GUIContent(){ image = tex, tooltip = entry.autoFixTooltip }, GUIStyle.none, imgSize);
            GUI.DrawTexture(rect, tex);

            GUILayout.EndHorizontal();
        }
    }

    private double lastUpdate = 0;
    public void Update()
    {
        // Update every 2 seconds
        double now = EditorApplication.timeSinceStartup;
        if (now - lastUpdate > 2)
        {
            lastUpdate = now;
            Repaint();
        }
    }

    [MenuItem("Window/Simulated Reality/SR Checklist")]
    private static void Init()
    {
        // Get existing open window or if none, make a new one:
        SREditorChecklist window = (SREditorChecklist)EditorWindow.GetWindow(typeof(SREditorChecklist));
        window.Show();
    }
}
#endif
