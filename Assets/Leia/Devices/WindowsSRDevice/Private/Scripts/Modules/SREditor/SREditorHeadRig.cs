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
using UnityEditor;
using UnityEngine;

// Editor inspector for HeadRig component
[CustomEditor(typeof(SimulatedRealityHeadRig))]
public class EditorHeadRig : Editor 
{
    public override void OnInspectorGUI()
    {
        SimulatedRealityHeadRig headRig = (SimulatedRealityHeadRig)target;

        string label = headRig.IsEyeRigPresent() ? "Update" : "Create";
        label += " eyes";

        if (GUILayout.Button(label))
        {
            headRig.CreateEyeRig();
        }

        GUI.enabled = headRig.IsEyeRigPresent();
        if (GUILayout.Button("Destroy eyes"))
        {
            headRig.DestroyEyeRig();
        }

        GUILayout.Space(10);

        label = headRig.IsEarRigPresent() ? "Update" : "Create";
        label += " ears";

        GUI.enabled = true;
        if (GUILayout.Button(label))
        {
            headRig.CreateEarRig();
        }

        GUI.enabled = headRig.IsEarRigPresent();
        if (GUILayout.Button("Destroy ears"))
        {
            headRig.DestroyEarRig();
        }

        GUILayout.Space(10);

        label = headRig.IsHeadRigPresent() ? "Update" : "Create";
        label += " head";

        GUI.enabled = true;
        if (GUILayout.Button(label))
        {
            headRig.CreateHeadRig();
        }

        GUI.enabled = headRig.IsHeadRigPresent();
        if (GUILayout.Button("Destroy head"))
        {
            headRig.DestroyHeadRig();
        }
    }
}

#endif
