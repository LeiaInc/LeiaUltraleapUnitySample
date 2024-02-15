/*!
* Copyright (C) 2023  Leia, Inc.
*
* This software has been provided under the Leia license agreement.
* You can find the agreement at https://www.leiainc.com/legal/license-agreement
*
* This source code is considered Creator Materials under the definitions of the Leia license agreement.
*/
using UnityEngine;

namespace LeiaUnity
{
    /// <summary>
    /// Script takes a target game object and sets the LeiaCamera's convergence distance 
    /// and baseline scaling automatically to keep that game object in focus and 
    /// displayed with a comfortable amount of depth.
    /// Assumes game object uses mesh filters.
    /// If you have multiple game objects you want to keep in focus, put them both under 
    /// one parent game object and assign that as the target.
    /// </summary>
    /// 
    [RequireComponent(typeof(LeiaDisplay))]
    [DefaultExecutionOrder(-1000)]
    [HelpURL("https://docs.leialoft.com/developer/unity-sdk/modules/auto-focus#leiatargetfocus")]
    public class LeiaTargetFocus : LeiaFocus
    {
        [Tooltip("How many mesh vertices to take as sample points for determining baseline and convergence. Larger sample count will be more costly performancewise.")]
        [SerializeField, Range(1, 1000)] public int samples = 200;
        public int Samples
        {
            get
            {
                return samples;
            }
            set
            {
                samples = Mathf.Clamp(value, 1,1000);
            }
        }
        [SerializeField] private GameObject _target;
        public GameObject target
        {
            get
            {
                return _target;
            }
            set
            {
                _target = value;
                InitTarget();
            }
        }


        class MeshInfo
        {
            public Transform transform;
            public Vector3[] vertices;

            public MeshInfo(MeshFilter meshFilter)
            {
                transform = meshFilter.transform;

                // Use MeshFilter.sharedMesh instead of meshFilter.mesh
                if (meshFilter.sharedMesh != null)
                {
                    vertices = meshFilter.sharedMesh.vertices;
                }
                else
                {
                    vertices = new Vector3[0];
                }
            }
        }

        MeshInfo[] meshInfos;

        private GameObject previousTarget;
        private int increment;
        private int totalVertices;
        private int previousChildCount;

        protected override void OnEnable()
        {
            base.OnEnable();
            InitTarget();
        }

        public void InitTarget()
        {
            if (target == null)
            {
                Debug.LogError("No target GameObject set for LeiaAutoFocusOnTarget");
                return;
            }
            
            previousTarget = target;
            previousChildCount = target.transform.childCount;

            MeshFilter[] meshFilters = target.GetComponentsInChildren<MeshFilter>();

            if (meshFilters.Length > 0)
            {
                int count = meshFilters.Length;
                meshInfos = new MeshInfo[count];

                for (int i = 0; i < count; i++)
                {
                    meshInfos[i] = new MeshInfo(  meshFilters[i] );
                }

                for (int j = 0; j < meshInfos.Length; j++)
                {
                    totalVertices += meshInfos[j].vertices.Length;
                }
            }
        }
        
        
        protected override void LateUpdate()
        {
            if (target == null && target != previousTarget)
            {
                Debug.LogError("No target GameObject set for LeiaTargetFocus to focus on");
                this.enabled = false;
                return;
            }

            //If the autofocus target is changed, re-initialize
            if (target != previousTarget || target.transform.childCount != previousChildCount)
            {
                InitTarget();
            }

            if (meshInfos != null && meshInfos.Length > 0)
            {
                increment = Mathf.Max(1, totalVertices / samples);
                float sumDistances = 0;
                float sumCounts = 0;

                float sumDistancesCurrent = 0;
                float sumCountsCurrent = 0;

                float closest = float.MaxValue;

                for (int j = 0; j < meshInfos.Length; j++)
                {
                    int count = meshInfos[j].vertices.Length;
                    for (int i = 0; i < count; i += increment)
                    {
                        Vector3 worldPoint = meshInfos[j].transform.TransformPoint(meshInfos[j].vertices[i]);
                        Vector3 screenPos = leiaDisplay.HeadCamera.WorldToViewportPoint(worldPoint);

                        if (screenPos.x > 0 && screenPos.x < 1
                            && screenPos.y > 0 && screenPos.y < 1
                            && screenPos.z > 0)
                        {
                            sumCountsCurrent++;
                            float distance = Vector3.Distance(worldPoint, leiaDisplay.DriverCamera.transform.position);
                            sumDistancesCurrent += distance;
                                sumCounts += 1;
                                sumDistances += distance;
                                if (distance < closest)
                                {
                                    closest = distance;
                                }
                        }
                    }
                }

                if (sumCounts > 0)
                {
                    float averageDistance = sumDistances / Mathf.Max(sumCounts, 1);

                    float newTargetConvergenceDistance = Mathf.Clamp(averageDistance, 1f, 1000000f);
                    
                    SetTargetConvergence(newTargetConvergenceDistance);

                    float nearPlaneBestBaseline =
                        LeiaDisplayUtils.GetRecommendedBaselineBasedOnNearPlane(
                        leiaDisplay,
                        closest,
                        averageDistance
                    );

                    SetTargetBaseline(nearPlaneBestBaseline);
                }
            }
            else
            {
                SetTargetConvergence(Vector3.Distance(leiaDisplay.DriverCamera.transform.position, target.transform.position));
            }

            base.LateUpdate();
        }
    }
}