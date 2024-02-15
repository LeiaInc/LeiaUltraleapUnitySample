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

[ExecuteInEditMode]
public class SREditorBorderViolations : MonoBehaviour
{
    public static void SetupHierarchy(GameObject root)
    {
        String parentObjectName = "SRBorderViolations";
        GameObject parentObject = SRUnity.SRUtility.FindChildObject(root, parentObjectName);
        if (parentObject == null)
        {
            parentObject = new GameObject();
            parentObject.transform.parent = root.transform;
        }

        parentObject.transform.localRotation = Quaternion.identity;
        parentObject.transform.localScale = Vector3.one;
        parentObject.transform.localPosition = Vector3.zero;
        parentObject.name = parentObjectName;

        SRUnity.SRUtility.SetSrGameObjectVisibility(parentObject);

        SREditorBorderViolations component = parentObject.GetComponent<SREditorBorderViolations>();
        if (component == null)
        {
            component = parentObject.AddComponent<SREditorBorderViolations>();
        }
    }

    private Mesh[] meshes = null;
    private MeshCollider[] meshColliders = null;

    private Vector3[] vertices = new Vector3[6];
    private int[] indices = 
    {
        0, 1, 2, // Frustum plane
        3, 4, 5, // Offset plane for collision
    };

    private float timeUntilNextCheck = 0;

    public void OnEnable()
    {
        meshes = new Mesh[4];
        for (int i = 0; i < 4; i++)
        {
            meshes[i] = new Mesh();
        }

        meshColliders = gameObject.GetComponents<MeshCollider>();
        if (meshColliders.Length != 4)
        {
            meshColliders = new MeshCollider[4];
            for (int i = 0; i < 4; i++)
            {
                meshColliders[i] = gameObject.AddComponent<MeshCollider>();
            }
        }

        Rigidbody body = gameObject.GetComponent<Rigidbody>();
        if (body == null)
        {
            body = gameObject.AddComponent<Rigidbody>();
        }

        body.isKinematic = true;
        body.constraints = RigidbodyConstraints.FreezeAll;

        foreach (MeshCollider meshCollider in meshColliders)
        {
            meshCollider.convex = true;
            meshCollider.isTrigger = true;
        }

        UpdateMesh();

        SRUserSettings.OnEditorSettingsChanged += OnUserSettingsChanged;
    }

    public void OnDisable()
    {
        SRUserSettings.OnEditorSettingsChanged -= OnUserSettingsChanged;
    }

    public void Update()
    {
        timeUntilNextCheck -= Time.deltaTime;

        if (timeUntilNextCheck <= 0)
        {
            UpdateMesh();
            timeUntilNextCheck += 0.1f;
        }
    }

    private GameObject GetParent()
    {
        return gameObject.transform.parent.gameObject;
    }

    private void UpdateMesh()
    {
        if (!SRUserSettings.Instance.reportBorderBiolations) return;

        SimulatedRealityCamera camera = GetParent().GetComponent<SimulatedRealityCamera>();
        if (camera == null) return;

        bool liveFrustum = SRUserSettings.Instance.liveFrustum;
#if UNITY_EDITOR
        liveFrustum = liveFrustum || EditorApplication.isPlaying;
#endif

        Vector3[] eyes;
        if (liveFrustum && camera.enableLookaround)
        {
            eyes = SRUnity.SREyes.Instance.GetEyes(ISRSettingsInterface.GetProjectSettings(null));
        }
        else
        {
            eyes = SRUnity.SREyes.Instance.GetEyes(ISRSettingsInterface.GetProjectSettings(null));
            Vector3 defaultHeadPosition = SRUnity.SREyes.Instance.GetDefaultEyePosition(ISRSettingsInterface.GetProjectSettings(null));
            Vector3 trackedHeadPosition = (eyes[0] + eyes[1]) / 2;
            Vector3 headOffSet = defaultHeadPosition - trackedHeadPosition;

            eyes[0] += headOffSet;
            eyes[1] += headOffSet;
        }

        camera.CalculateCorrectedEyeCoordinates(ref eyes);

        if (eyes.Length != 2) return;
        if (eyes[0] == Vector3.zero || eyes[1] == Vector3.zero) return;

        Vector2 screenHalfSize = camera.GetScaledScreenHalfSize();

        if (screenHalfSize.x <= 0.1f || screenHalfSize.y <= 0.1f) return;

        Vector3 tl = new Vector3(-screenHalfSize.x, screenHalfSize.y, 0);
        Vector3 tr = new Vector3(screenHalfSize.x, screenHalfSize.y, 0);
        Vector3 bl = new Vector3(-screenHalfSize.x, -screenHalfSize.y, 0);
        Vector3 br = new Vector3(screenHalfSize.x, -screenHalfSize.y, 0);
        Vector3 head = (eyes[0] + eyes[1]) * 0.5f;

        // Ignore object scale
        Vector3 inverseScale = new Vector3(1.0f / transform.lossyScale.x, 1.0f / transform.lossyScale.y, 1.0f / transform.lossyScale.z);
        tl.Scale(inverseScale);
        tr.Scale(inverseScale);
        bl.Scale(inverseScale);
        br.Scale(inverseScale);
        head.Scale(inverseScale);

        for (int i = 0; i < 4; i++)
        {
            // Calculate frustum plane vertices
            if (i == 0) // Top
            {
                vertices[0] = tl;
                vertices[1] = tr;
            }
            else if (i == 1) // Bottom
            {
                vertices[0] = br;
                vertices[1] = bl;
            }
            else if (i == 2) // Left
            {
                vertices[0] = bl;
                vertices[1] = tl;
            }
            else if (i == 3) // Right
            {
                vertices[0] = tr;
                vertices[1] = br;
            }

            vertices[2] = head;

            // Calculate offset plane to allow for collision check
            Vector3 side1 = vertices[1] - vertices[0];
            Vector3 side2 = vertices[2] - vertices[0];
            Vector3 normal = Vector3.Cross(side1, side2);

            Vector3 offset = normal * 0.001f;

            for (int j = 0; j < 3; j++)
            {
                vertices[j + 3] = vertices[j] + offset;
            }

            meshes[i].vertices = vertices;
            meshes[i].triangles = indices;

            meshColliders[i].sharedMesh = meshes[i];
        }
    }

    public void OnTriggerEnter(Collider other)
    {
        OnBorderViolation(other.gameObject);
    }

    public void OnCollisionEnter(Collision other)
    {
        OnBorderViolation(other.gameObject);
    }

    private static Dictionary<GameObject, List<GameObject>> activeCollisions = new Dictionary<GameObject, List<GameObject>>();

    private void OnBorderViolation(GameObject other)
    {
        if (!SRUserSettings.Instance.reportBorderBiolations) return;

        if (!activeCollisions.ContainsKey(GetParent()))
        {
            activeCollisions.Add(GetParent(), new List<GameObject>());
        }

        if (!activeCollisions[GetParent()].Contains(other))
        {
            activeCollisions[GetParent()].Add(other);
            Debug.LogWarning("Border violation caused by <b>" + other.name + "</b> with <b>" + GetParent().name + "</b>", other);
        }
    }

    private void OnUserSettingsChanged()
    {
        timeUntilNextCheck = 0;
        activeCollisions.Clear();
    }
}

#endif
