/*!
* Copyright (C) 2023  Leia, Inc.
*
* This software has been provided under the Leia license agreement.
* You can find the agreement at https://www.leiainc.com/legal/license-agreement
*
* This source code is considered Creator Materials under the definitions of the Leia license agreement.
*/
using UnityEngine;
using UnityEngine.EventSystems;

namespace LeiaUnity.Examples
{
    public class CameraRigMovementMouse : MonoBehaviour
    {
        [SerializeField] private float sensitivity = .01f;
        private Vector3 startMousePosition = Vector3.zero;
        private Vector3 startPosition = Vector3.zero;
        private Transform childCamera = null;
        private bool multiTouching = false;
        bool startedOnUI = false;

        void Start()
        {
            childCamera = GetComponentInChildren<Camera>().transform;
        }

        void LateUpdate()
        {
            if (startedOnUI)
            {
                if (Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(2))
                {
                    startedOnUI = false;
                }
                return;
            }

            if (Input.touchCount > 1)
            {
                multiTouching = true;
                return;
            }
            else
            {
                if (multiTouching)
                {
                    if (!Input.GetMouseButton(0))
                    {
                        multiTouching = false;
                    }
                    return;
                }
            }

            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(2))
            {
                if (EventSystem.current.IsPointerOverGameObject())
                {
                    startedOnUI = true;
                    return;
                }
                startMousePosition = Input.mousePosition;
                startPosition = transform.position;
            }

            float zoomLevel = childCamera.localPosition.z;

            if (Input.GetMouseButton(0) || Input.GetMouseButton(2))
            {
                Quaternion rotateBy = Quaternion.AngleAxis(transform.rotation.eulerAngles.y, Vector3.up);

                Vector3 deltaMousePosition =
                    new Vector3(
                        Input.mousePosition.x - startMousePosition.x,
                        0,
                        Input.mousePosition.y - startMousePosition.y
                        );

                transform.position = startPosition + (rotateBy * (deltaMousePosition * zoomLevel * sensitivity));
            }
        }
    }
}