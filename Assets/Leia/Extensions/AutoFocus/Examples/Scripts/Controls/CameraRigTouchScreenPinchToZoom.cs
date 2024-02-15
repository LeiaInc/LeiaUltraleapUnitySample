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
    public class CameraRigTouchScreenPinchToZoom : MonoBehaviour
    {
        [SerializeField] MinMaxPair perspZoomRange = new MinMaxPair(10, 0, "MinZoom", 50, float.MaxValue, "Max zoom");
        [SerializeField] MinMaxPair orthoZoomRange = new MinMaxPair(1, 0, "Min ortho zoom", 10, float.MaxValue, "Max ortho zoom");
        [SerializeField, Tooltip("Zoom sensitivity when using a perspective camera")] private float perspectiveSensitivity = .01f;
        [SerializeField, Tooltip("Zoom sensitivity when using an orthographic camera")] private float orthographicSensitivity = .004f;
        private float startTouchDistance = 0;
        private float startCameraDistance = 0;
        private float startOrthographicSize = 0;
        private Camera childCamera = null;

        void Start()
        {
            childCamera = GetComponentInChildren<Camera>();
        }

        void LateUpdate()
        {
            if (Input.touchCount > 1)
            {
                float currentTouchDistance = Vector3.Distance(Input.GetTouch(0).position, Input.GetTouch(1).position);

                if (Input.GetTouch(1).phase == TouchPhase.Began)
                {
                    startTouchDistance = currentTouchDistance;
                    startCameraDistance = -childCamera.transform.localPosition.z;
                    startOrthographicSize = childCamera.orthographicSize;
                }
                else
                {
                    float newZoom;

                    if (childCamera.orthographic)
                    {
                        newZoom = startOrthographicSize - (currentTouchDistance - startTouchDistance) * orthographicSensitivity;
                        newZoom = Mathf.Clamp(newZoom, orthoZoomRange.min, orthoZoomRange.max);
                        childCamera.orthographicSize = newZoom;
                    }
                    else
                    {
                        newZoom = startCameraDistance - (currentTouchDistance - startTouchDistance) * perspectiveSensitivity;
                        newZoom = Mathf.Clamp(newZoom, perspZoomRange.min, perspZoomRange.max);
                        childCamera.transform.localPosition = new Vector3(
                            0,
                            0,
                            -newZoom
                        );
                    }
                }
            }
        }
    }
}