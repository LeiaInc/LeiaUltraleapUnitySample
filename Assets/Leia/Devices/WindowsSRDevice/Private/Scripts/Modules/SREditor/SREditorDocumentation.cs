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
using System.IO;
using UnityEngine;
using UnityEditor;
using System;

// SR documentation menu option
public class SREditorDocumentation : EditorWindow
{

    [MenuItem("Window/Simulated Reality/Documentation")]
    private static void Init()
    {
        string documentationPath = Application.dataPath + "/../" + SRUnity.SRUtility.GetPluginRoot() + "/Docs/Index.html";
        documentationPath = Path.GetFullPath(documentationPath);
        SRUnity.SRUtility.Debug("Opening docs: " + documentationPath);
        Application.OpenURL("file:///" + documentationPath);
    }
}
#endif
