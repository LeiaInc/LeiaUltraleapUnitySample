/*!
* Copyright (C) 2023  Leia, Inc.
*
* This software has been provided under the Leia license agreement.
* You can find the agreement at https://www.leiainc.com/legal/license-agreement
*
* This source code is considered Creator Materials under the definitions of the Leia license agreement.
*/
using UnityEngine;

namespace LeiaUnity.Examples
{
    public class CameraRigMouseVerticalPan : MonoBehaviour
    {
        [SerializeField, Range(.01f, 1f)] private float sensitivity = .1f;
        [SerializeField, Range(2f, 90f)] private float minAngle = 2f, maxAngle = 90f;
        private Transform cameraPivot = null;
        private Transform camera3d = null;
        private Vector3 startMousePosition = Vector3.zero;
        private float startRotation = 0;

        void Start()
        {
            cameraPivot = transform.Find("RotatePivot");
            camera3d = GameObject.Find("Scene 3D Camera").transform;
        }

        void LateUpdate()
        {
            if (Input.GetMouseButtonDown(1))
            {
                startMousePosition = Input.mousePosition;
                startRotation = cameraPivot.rotation.eulerAngles.x;
            }

            if (Input.GetMouseButton(1))
            {
                float deltaMousePositionY = Input.mousePosition.y - startMousePosition.y;
                float unclampedRotation = startRotation - deltaMousePositionY * sensitivity;
                float clampedRotation = Mathf.Clamp(unclampedRotation, minAngle, maxAngle);
                cameraPivot.localRotation = Quaternion.Euler(
                    clampedRotation,
                    0,
                    0);
            }

            camera3d.localRotation = Quaternion.identity;
        }
    }
}