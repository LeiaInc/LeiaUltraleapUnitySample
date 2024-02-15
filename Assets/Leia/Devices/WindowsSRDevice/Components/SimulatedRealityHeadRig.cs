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
using UnityEngine;
using UnityEditor;
#if !UNITY_EDITOR && PLATFORM_STANDALONE_WIN
using SimulatedReality;
#endif
[ExecuteInEditMode]
[AddComponentMenu("Simulated Reality/Simulated Reality Head")]
// Component to easily get the SR eye positions in the world
public class SimulatedRealityHeadRig : MonoBehaviour
{
    private GameObject[] eyes = new GameObject[2] { null, null };
    private String[] eyeNames = new string[] { "SR_Eye_L", "SR_Eye_R" };

    private GameObject[] ears = new GameObject[2] { null, null };
    private String[] earsNames = new string[] { "SR_Ear_L", "SR_Ear_R" };

    private GameObject head = null;
    private String headName = "SR_Head";

    public void OnEnable()
    {
        CheckHierarchy();
    }

    public void Awake()
    {
        CheckHierarchy();
    }

    public void Start()
    {
        CheckHierarchy();
    }

    public void Update()
    {
        Vector3[] eyePositions = SRUnity.SRHead.Instance.GetEyes(ISRSettingsInterface.GetProjectSettings(null));
        Vector3[] earPositions = SRUnity.SRHead.Instance.GetEars(ISRSettingsInterface.GetProjectSettings(null));
        Vector3 headPosition = SRUnity.SRHead.Instance.GetHeadPosition(ISRSettingsInterface.GetProjectSettings(null));
        Quaternion headOrientation = SRUnity.SRHead.Instance.GetHeadOrientation();

        for (int i = 0; i < 2; i++)
        {
            if (eyes[i] != null)
            {
#if UNITY_EDITOR
                if (!EditorApplication.isPlaying)
                {
                    eyes[i].transform.localPosition = SRUnity.SRHead.Instance.GetDefaultHeadPosition(ISRSettingsInterface.GetProjectSettings(null));
                }
                else
#endif
                {
                    eyes[i].transform.localPosition = eyePositions[i];
                }
            }

            if (ears[i] != null)
            {
#if UNITY_EDITOR
                if (!EditorApplication.isPlaying)
                {
                    ears[i].transform.localPosition = SRUnity.SRHead.Instance.GetDefaultHeadPosition(ISRSettingsInterface.GetProjectSettings(null));
                }
                else
#endif
                {
                    ears[i].transform.localPosition = earPositions[i];
                }
            }
        }

        if (head != null)
        {
#if UNITY_EDITOR
                if (!EditorApplication.isPlaying)
                {
                    head.transform.localPosition = SRUnity.SRHead.Instance.GetDefaultHeadPosition(ISRSettingsInterface.GetProjectSettings(null));
                    head.transform.localRotation = Quaternion.identity;
                }
                else
#endif
            {
                head.transform.localPosition = headPosition;
                head.transform.localRotation = headOrientation;
            }
        }
    }

    public void CreateEyeRig()
    {
        for (int i = 0; i < 2; i++)
        {
            if (eyes[i] == null)
            {
                eyes[i] = new GameObject(); //TODO render icon in hierarchy
            }

            eyes[i].name = eyeNames[i];
            eyes[i].transform.parent = gameObject.transform;
            eyes[i].transform.localPosition = SRUnity.SREyes.Instance.GetDefaultEyePosition(ISRSettingsInterface.GetProjectSettings(null));
        }

        CheckHierarchy();
    }

    public void DestroyEyeRig()
    {
        for (int i = 0; i < 2; i++)
        {
            if (eyes[i] != null)
            {
                DestroyImmediate(eyes[i]);
                eyes[i] = null;
            }
        }

        CheckHierarchy();
    }

    public bool IsEyeRigPresent()
    {
        return eyes[0] != null || eyes[1] != null;
    }

    public void CreateEarRig()
    {
        for (int i = 0; i < 2; i++)
        {
            if (ears[i] == null)
            {
                ears[i] = new GameObject(); //TODO render icon in hierarchy
            }

            ears[i].name = earsNames[i];
            ears[i].transform.parent = gameObject.transform;
            ears[i].transform.localPosition = SRUnity.SREyes.Instance.GetDefaultEyePosition(ISRSettingsInterface.GetProjectSettings(null));
        }

        CheckHierarchy();
    }

    public void DestroyEarRig()
    {
        for (int i = 0; i < 2; i++)
        {
            if (ears[i] != null)
            {
                DestroyImmediate(ears[i]);
                ears[i] = null;
            }
        }

        CheckHierarchy();
    }

    public bool IsEarRigPresent()
    {
        return ears[0] != null || ears[1] != null;
    }

    public void CreateHeadRig()
    {
        if (head == null)
        {
            head = new GameObject(); //TODO render icon in hierarchy
        }

        head.name = headName;
        head.transform.parent = gameObject.transform;
        head.transform.localPosition = SRUnity.SREyes.Instance.GetDefaultEyePosition(ISRSettingsInterface.GetProjectSettings(null));

        CheckHierarchy();
    }

    public void DestroyHeadRig()
    {
        if (head != null)
        {
            DestroyImmediate(head);
            head = null;
        }
        
        CheckHierarchy();
    }

    public bool IsHeadRigPresent()
    {
        return head != null;
    }

    private void CheckHierarchy()
    {
        for (int i = 0; i < 2; i++)
        {
            eyes[i] = SRUnity.SRUtility.FindChildObject(gameObject, eyeNames[i]);
        }

        for (int i = 0; i < 2; i++)
        {
            ears[i] = SRUnity.SRUtility.FindChildObject(gameObject, earsNames[i]);
        }

        head = SRUnity.SRUtility.FindChildObject(gameObject, headName);
    }
}
