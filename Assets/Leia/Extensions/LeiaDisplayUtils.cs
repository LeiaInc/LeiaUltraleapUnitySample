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
    public static class LeiaDisplayUtils
    {
        /// <summary>
        /// Performs a raycast from the given LeiaCamera
        /// </summary>
        /// <param name="leiaCam">A LeiaCamera with a Camera component and Transform</param>
        /// <param name="position">A screenPosition</param>
        /// <returns>A ray from the camera's world position, that passes through the screenPosition</returns>
        public static Ray ScreenPointToRay(this LeiaDisplay leiaDisplay, Vector3 screenPosition)
        {
            Camera cam = leiaDisplay.HeadCamera;
            bool prev_state = cam.enabled;
            cam.enabled = true;
            Ray r = cam.ScreenPointToRay(screenPosition);
            cam.enabled = prev_state;
            return (r);
        }

        /// <summary>
        /// Transforms a point from screen space to world space
        /// </summary>
        /// <param name="leiaCam">A LeiaCamera with a Camera component and Transform</param>
        /// <param name="position">A screenPosition</param>
        /// <returns>A Vector3 representing screenPosition in world space coordinates</returns>
        public static Vector3 ScreenToWorldPoint(this LeiaDisplay leiaDisplay, Vector3 screenPosition)
        {
            Camera cam = leiaDisplay.HeadCamera;
            bool prev_state = cam.enabled;
            cam.enabled = true;
            Vector3 r = cam.ScreenToWorldPoint(screenPosition);
            cam.enabled = prev_state;
            return (r);
        }

        /// <summary>
        /// Returns a baseline scaling value for a LeiaCamera based on a desired 
        /// convergence distance and leia frustum near plane distance.
        /// Useful for automatic baseline calculation scripts.
        /// </summary>
        /// <param name="leiaCam">A LeiaCamera with a Camera component and Transform</param>
        /// <param name="farPlaneDistance">The distance of the desired far plane of the Leia frustum. 
        /// Ideally should be set to the distance from the camera to the furthest currently visible point in the scene.</param>
        /// <returns>A float representing a baseline scaling value that satisfies the specified 
        /// convergence distance and Leia frustum far plane distance.</returns>
        public static float GetRecommendedBaselineBasedOnFarPlane(LeiaDisplay leiaDisplay, float farPlaneDistance, float convergenceDistance)
        {
            float recommendedBaseline;

            if (leiaDisplay.HeadCamera.orthographic)
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
        /// Returns a baseline scaling value for a LeiaCamera based on a desired 
        /// convergence distance and leia frustum near plane distance.
        /// Useful for automatic baseline calculation scripts.
        /// </summary>
        /// <param name="leiaCam">A LeiaCamera with a Camera component and Transform</param>
        /// <param name="nearPlaneDistance">The distance of the desired near plane of the Leia frustum. 
        /// Ideally should be set to the distance from the camera to the closest currently visible point in the scene.</param>
        /// <returns>A float representing a baseline scaling value that satisfies the specified 
        /// convergence distance and Leia frustum near plane distance.</returns>
        public static float GetRecommendedBaselineBasedOnNearPlane(LeiaDisplay leiaDisplay, float nearPlaneDistance, float convergenceDistance)
        {
            float recommendedBaseline;

            if (leiaDisplay.HeadCamera.orthographic)
            {
                recommendedBaseline = -1f / (nearPlaneDistance - convergenceDistance);
            }
            else //if its a perspective camera
            {
                recommendedBaseline = nearPlaneDistance / Mathf.Max(convergenceDistance - nearPlaneDistance, .01f);
            }

            return recommendedBaseline;
        }
    }
}
