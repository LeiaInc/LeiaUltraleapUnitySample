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
using System.IO;
using UnityEngine;
using UnityEditor;
using System.Reflection;

[Serializable]
public class SRDisplayDefinition
{
    public String name;
    public Vector2 displaySize;
    public float viewingDistance;
} 

// Class to handle editor related settings. Saved in 'Library'.
public class SRUserSettings
{
    // Settings parameters
    public bool debugMode = false;
    public bool liveFrustum = true;
    public bool liveHands = false;
    public bool reportBorderBiolations = true;

    public List<SRDisplayDefinition> displayDefinitions = new List<SRDisplayDefinition>() 
    {
        new SRDisplayDefinition { name = "Devkit", displaySize = new Vector2(69, 39), viewingDistance = 70.0f },
        new SRDisplayDefinition { name = "Laptop", displaySize = new Vector2(35.6f, 20), viewingDistance = 45.0f },
        new SRDisplayDefinition { name = "65 Inch", displaySize = new Vector2(144, 81), viewingDistance = 150.0f }
    };

#if UNITY_EDITOR
    private static string assetPath = "Library\\SRUserSettings.asset";
#endif

    public void Save()
    {
#if UNITY_EDITOR
        string data = JsonUtility.ToJson(this);
        File.WriteAllText(Path.GetFullPath(Application.dataPath + "\\..\\" + assetPath), data);
        OnEditorSettingsChanged?.Invoke();
#endif
    }
    
    public delegate void OnEditorSettingsChangedDelegate();
    public static event OnEditorSettingsChangedDelegate OnEditorSettingsChanged;

    // Singleton interface
    private static SRUserSettings instance_;
    public static SRUserSettings Instance 
    {
        get
        {
#if UNITY_EDITOR
            if (instance_ == null)
            {
                string path = Path.GetFullPath(Application.dataPath + "\\..\\" + assetPath);
                if (File.Exists(path))
                {
                    string data = File.ReadAllText(path);
                    if (data.Length > 0)
                    {   
                        try
                        {
                            instance_ = new SRUserSettings();
                            JsonUtility.FromJsonOverwrite(data, instance_);
                            instance_.Save();
                        }
                        catch(Exception e)
                        {
                            SRUnity.SRUtility.Warning("Failed to load settings: " + e.ToString());
                            instance_ = null;
                        }
                    }
                }
            }
#endif
            // Create new asset if none was found or could be loaded
            if (instance_ == null)
            {
                instance_ = new SRUserSettings();
                instance_.Save();
            }

            return instance_;
        }
    }
}
