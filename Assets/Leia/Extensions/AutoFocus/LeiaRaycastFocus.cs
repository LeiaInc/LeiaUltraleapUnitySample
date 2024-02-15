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
    [RequireComponent(typeof(LeiaDisplay))]
    [DisallowMultipleComponent]
    [HelpURL("https://docs.leialoft.com/developer/unity-sdk/modules/auto-focus#leiaraycastfocus")]
    public class LeiaRaycastFocus : LeiaFocus
    {
        [Tooltip("Layers the camera should focus on"), SerializeField] private LayerMask _layers = ~0;
        public LayerMask layers
        {
            get
            {
                return _layers;
            }
            set
            {
                _layers = value;
            }
        }

        [SerializeField, Tooltip("The maximum distance the auto focus algorithm is allowed to raycast to collect sample points. Note that if this is smaller than the convergence range max value, then it will take precedence over the convergence range max value when determining the max convergence distance.")]
        private float _maxRaycastDistance = 1000f;
        [Range(1, 1000), Tooltip("Raycast samples to take")]
        [SerializeField] public int samples = 500;
        private int previous_samples = -1;
        private Vector2[] cameraNearPlaneRaysOrigins = new Vector2[500];

        [Tooltip("Show debug rays in the scene editor")]
        [SerializeField] private bool showDebugRaycasts = false;

        private float furthestDistance;
        private float closestDistance;
        private float avgDistance;
        private float hits = 0;

        /// <summary>
        /// A higher than 1 distanceWeightPower value will result in centermost raycasts being more important.
        /// A closer to 1 distanceWeightPower will result in more uniform significance of raycasts across the view.
        /// </summary>
        const float distanceWeightPower = 1.1f;
        /// <summary>
        /// The minimum distancePowered a sample can have (prevents division by zero, sets cap on how significant 
        /// the centermost point is, lower values mean the centermost point is more significant, do not set to 0)
        /// </summary>
        const float minDistancePowered = .1f;

        protected override void OnEnable()
        {
            base.OnEnable();
        }

        protected override void LateUpdate()
        {
            CalculateTargetConvergenceAndBaseline();
            base.LateUpdate();
        }

        public float MaxRaycastDistance
        {
            get
            {
                return _maxRaycastDistance;
            }
            set
            {
                _maxRaycastDistance = Mathf.Max(value, 0f);
            }
        }

        readonly Vector3[] frustrumCorners = new Vector3[4];
        private Matrix4x4 nearPlaneTransformMatrix;
        private Matrix4x4 offsettedNearPlaneTM;
        readonly float crossScale = 0.01f;

        private void CalculateTargetConvergenceAndBaseline()
        {
            samples = Mathf.Max(1, samples);
            if (samples != previous_samples) {
                System.Array.Resize(ref cameraNearPlaneRaysOrigins, samples);
                int _sqrtSamples = Mathf.CeilToInt(Mathf.Sqrt(samples));
                int spaceSegments = _sqrtSamples + 1;
                float percentagePerSegment = 1f / spaceSegments;
                int samplesCounter = 0;
                //Always perform a sample in the direct center of the view
                cameraNearPlaneRaysOrigins[samplesCounter++] = new Vector2(0, 0);
                float offset = -0.5f + percentagePerSegment;
                for (int x = 0; x < _sqrtSamples; x++)
                {
                    for (int y = 0; y < _sqrtSamples; y++)
                    {
                        if (samplesCounter < samples)
                        {
                            cameraNearPlaneRaysOrigins[samplesCounter] = new Vector2(offset + x * percentagePerSegment, offset + y * percentagePerSegment);
                            samplesCounter++;
                        }
                    }
                }

                previous_samples = samples;
            }

            Vector3 frustumCorner0;
            Vector3 frustumCorner1;
            Vector3 frustumCorner2;
            Vector3 frustumCorner3;
            Matrix4x4 localToWorldMatrix = transform.localToWorldMatrix;
            Vector3 pos = leiaDisplay.DriverCamera.transform.position;
            Vector3 right = leiaDisplay.DriverCamera.transform.right;
            Vector3 forward = leiaDisplay.DriverCamera.transform.forward;
            Vector3 up = leiaDisplay.DriverCamera.transform.up;

            float forwardOffset = leiaDisplay.DriverCamera.nearClipPlane;
            if (leiaDisplay.DriverCamera.orthographic)
            {
                float ysize = leiaDisplay.DriverCamera.orthographicSize;
                float xsize = ysize * leiaDisplay.DriverCamera.aspect;
                frustumCorner0 = pos + forward * forwardOffset - up * ysize - right * xsize;
                frustumCorner1 = pos + forward * forwardOffset + up * ysize - right * xsize;
                frustumCorner2 = pos + forward * forwardOffset + up * ysize + right * xsize;
                frustumCorner3 = pos + forward * forwardOffset - up * ysize + right * xsize;
            }
            else
            {
                leiaDisplay.DriverCamera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), forwardOffset, Camera.MonoOrStereoscopicEye.Mono, frustrumCorners);
                frustumCorner0 = pos + localToWorldMatrix.MultiplyVector(frustrumCorners[0]);
                frustumCorner1 = pos + localToWorldMatrix.MultiplyVector(frustrumCorners[1]);
                frustumCorner2 = pos + localToWorldMatrix.MultiplyVector(frustrumCorners[2]);
                frustumCorner3 = pos + localToWorldMatrix.MultiplyVector(frustrumCorners[3]);
            }

            nearPlaneTransformMatrix = Matrix4x4.Translate( Vector3.LerpUnclamped( frustumCorner0, frustumCorner2, 0.5f) );
            nearPlaneTransformMatrix.SetColumn(0, frustumCorner3 - frustumCorner0 );
            nearPlaneTransformMatrix.SetColumn(1, frustumCorner1 - frustumCorner0 );
            if (showDebugRaycasts)
            {
                offsettedNearPlaneTM = nearPlaneTransformMatrix;
                offsettedNearPlaneTM.SetColumn(3, offsettedNearPlaneTM.GetColumn(3) + (Vector4)(forward * 0.001f));
            }

            furthestDistance = 0;
            closestDistance = float.MaxValue;
            avgDistance = 0f;
            hits = 0;
            float convergencePlaneHalfWidth;

            //Execute a grid of samples across the entire view
            if (leiaDisplay.DriverCamera.orthographic)
            {
                convergencePlaneHalfWidth = leiaDisplay.DriverCamera.orthographicSize * 2f;
                for (int i = 0; i < cameraNearPlaneRaysOrigins.Length; i++)
                {              
                    Vector3 startPoint = nearPlaneTransformMatrix.MultiplyPoint3x4(cameraNearPlaneRaysOrigins[i]);
                    SampleSpherecast(startPoint, forward, convergencePlaneHalfWidth / (samples * 2f), i);
                }
            }
            else
            {
                convergencePlaneHalfWidth = leiaDisplay.FocalDistance * Mathf.Sin(leiaDisplay.DriverCamera.fieldOfView * Mathf.Deg2Rad) / 2f;
                for (int i = 0; i < cameraNearPlaneRaysOrigins.Length; i++)
                {
                    Vector3 startPoint = nearPlaneTransformMatrix.MultiplyPoint3x4(cameraNearPlaneRaysOrigins[i]);// + new Vector3(0, 100, 0);
                    Vector3 direction = (startPoint - leiaDisplay.DriverCamera.transform.position).normalized;
                    SampleSpherecast(startPoint, direction, convergencePlaneHalfWidth / (samples * 2f), i);
                }
            }
 
            if (hits > 0)
            {
                avgDistance /= hits;

                float newTargetConvergenceDistance = avgDistance + leiaDisplay.DriverCamera.nearClipPlane;

                this.SetTargetConvergence(newTargetConvergenceDistance);

                float nearPlaneBestBaseline = GetRecommendedBaselineBasedOnNearPlane(
                    closestDistance, 
                    targetConvergence
                );

                float farPlaneBestBaseline = GetRecommendedBaselineBasedOnFarPlane(
                    furthestDistance, 
                    targetConvergence
                );

                float targetBaseline = Mathf.Min(nearPlaneBestBaseline, farPlaneBestBaseline);
                this.SetTargetBaseline(targetBaseline);
            }
        }

        float GetRecommendedBaselineBasedOnNearPlane(float nearPlaneDistance, float convergenceDistance)
        {
            float recommendedBaseline;

            if (leiaDisplay.DriverCamera.orthographic)
            {
                recommendedBaseline = -1f / (nearPlaneDistance - convergenceDistance);
            }
            else //if its a perspective camera
            {
                recommendedBaseline = nearPlaneDistance / Mathf.Max(convergenceDistance - nearPlaneDistance, .01f);
            }

            return recommendedBaseline;
        }

        float GetRecommendedBaselineBasedOnFarPlane(float farPlaneDistance, float convergenceDistance)
        {
            float recommendedBaseline;

            if (leiaDisplay.DriverCamera.orthographic)
            {
                recommendedBaseline = 1f / (farPlaneDistance - convergenceDistance);
            }
            else //if its a perspective camera
            {
                recommendedBaseline = farPlaneDistance / Mathf.Max(convergenceDistance - farPlaneDistance, .01f);
            }

            return recommendedBaseline;
        }

        /// <summary>
        /// Performs a spherecast sample, sets the closest and furthest distance found so far, and accumulates
        /// the sum of hit distances so that the average can be later calculated and used to set convergence distance
        /// </summary>
        /// <param name="startPoint"></param> The point to start the spherecast from
        /// <param name="direction"></param> The direction for the spherecast to travel in
        /// <param name="radius"></param> The radius of the spherecast
        void SampleSpherecast(Vector3 startPoint, Vector3 direction, float radius, int sampleIndex)
        {
 
            RaycastHit hit;
            Physics.SphereCast(
                startPoint,
                radius,
                direction,
                out hit,
                MaxRaycastDistance,
                layers
                );

            if (hit.collider != null)
            {
                if (hit.distance > furthestDistance)
                {
                    furthestDistance = hit.distance;
                }
                if (hit.distance < closestDistance)
                {
                    closestDistance = hit.distance;
                }

                //Accumulate weighted average of hit distances where weight is inversely proportional to distance from center of view
               
                float distancePowered = Mathf.Pow(hit.distance, distanceWeightPower);
                float weight = (1f / Mathf.Max(minDistancePowered, distancePowered));

                avgDistance += hit.distance * weight;
                hits += 1f * weight;

                if (showDebugRaycasts)
                {
                    Debug.DrawLine(startPoint, startPoint + direction * hit.distance, Color.green);
                    Vector2 c0 = cameraNearPlaneRaysOrigins[sampleIndex];
                    Vector2 cbottom = c0 + new Vector2(0, -crossScale);
                    Vector2 ctop = c0 + new Vector2(0, crossScale);
                    Vector2 cleft = c0 + new Vector2(-crossScale, 0);
                    Vector2 cright = c0 + new Vector2(crossScale, 0);
                    Debug.DrawLine(offsettedNearPlaneTM.MultiplyPoint3x4(ctop), nearPlaneTransformMatrix.MultiplyPoint3x4(cbottom), Color.green);
                    Debug.DrawLine(offsettedNearPlaneTM.MultiplyPoint3x4(cleft), nearPlaneTransformMatrix.MultiplyPoint3x4(cright), Color.green);
                }
 
            }
 
            else
            {
                if (showDebugRaycasts)
                {
                    Debug.DrawRay(startPoint, direction * targetConvergence * 2f, Color.red);
                    Vector2 c0 = cameraNearPlaneRaysOrigins[sampleIndex];
                    Vector2 cbottom = c0 + new Vector2(0, -crossScale);
                    Vector2 ctop = c0 + new Vector2(0, crossScale);
                    Vector2 cleft = c0 + new Vector2(-crossScale, 0);
                    Vector2 cright = c0 + new Vector2(crossScale, 0);
                    Debug.DrawLine(offsettedNearPlaneTM.MultiplyPoint3x4(ctop), nearPlaneTransformMatrix.MultiplyPoint3x4(cbottom), Color.red);
                    Debug.DrawLine(offsettedNearPlaneTM.MultiplyPoint3x4(cleft), nearPlaneTransformMatrix.MultiplyPoint3x4(cright), Color.red);
                }
                
            }
 
        }

    }
}