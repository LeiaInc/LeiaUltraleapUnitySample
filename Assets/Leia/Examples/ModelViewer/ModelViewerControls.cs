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
    public class ModelViewerControls : MonoBehaviour
    {
        // Sensitivity for rotation and zooming with mouse
        [SerializeField] private float mouseRotationSensitivity = 1.0f;
        [SerializeField] private float mouseZoomSensitivity = 1.0f;

        // Touch zoom sensitivity
        [SerializeField] private float touchZoomSensitivity = 0.025f;

        [SerializeField] private float minModelScale = .1f;
        [SerializeField] private float maxModelScale = 5;

        // Variables for touch input
        private float prevTouchDistance;
        private Vector3 prevTouchPosition;

        bool usedMultiTouch;

        void Update()
        {
            // Handle pinch to zoom for touchscreen
            if (Input.touchCount == 2)
            {
                usedMultiTouch = true;
                Touch touch1 = Input.GetTouch(0);
                Touch touch2 = Input.GetTouch(1);

                if (touch1.phase == TouchPhase.Began || touch2.phase == TouchPhase.Began)
                {
                    prevTouchDistance = Vector2.Distance(touch1.position, touch2.position);
                }
                else if (touch1.phase == TouchPhase.Moved || touch2.phase == TouchPhase.Moved)
                {
                    float touchDistance = Vector2.Distance(touch1.position, touch2.position);
                    float pinchAmount = touchDistance - prevTouchDistance;
                    ZoomModel(pinchAmount * Time.deltaTime * touchZoomSensitivity);
                    prevTouchDistance = touchDistance;
                }
            }
            else
            {
                if (!usedMultiTouch)
                {
                    // Handle rotation with one finger touch/mouse click
                    if (Input.GetMouseButtonDown(0) && !IsOverUI())
                    {
                        prevTouchPosition = Input.mousePosition;
                    }

                    if (Input.GetMouseButton(0) && !IsOverUI())
                    {
                        Vector3 delta = Input.mousePosition - prevTouchPosition;
                        RotateModel(delta, mouseRotationSensitivity);
                        prevTouchPosition = Input.mousePosition;
                    }
                }
                else
                {
                    if (!Input.GetMouseButton(0))
                    {
                        usedMultiTouch = false;
                    }
                }
            }

            // Handle zooming with the mouse wheel
            float scrollInput = Input.GetAxis("Mouse ScrollWheel");
            ZoomModel(scrollInput * Time.deltaTime * mouseZoomSensitivity);
        }

        void RotateModel(Vector3 delta, float sensitivity)
        {
            // Rotate the model around its local up axis
            float rotationX = delta.y * sensitivity;
            float rotationY = -delta.x * sensitivity;

            transform.Rotate(Vector3.up, rotationY, Space.World);
            transform.Rotate(Vector3.right, rotationX, Space.World);
        }

        void ZoomModel(float amount)
        {
            // Zoom the model by scaling it uniformly
            Vector3 newScale = transform.localScale + Vector3.one * amount;
            if (newScale.x < minModelScale)
            {
                newScale = Vector3.one * minModelScale;
            }
            if (newScale.x > maxModelScale)
            {
                newScale = Vector3.one * maxModelScale;
            }
            transform.localScale = newScale;
        }

        bool IsOverUI()
        {
            // Helper function to check if the mouse is over a UI element
            return UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
        }
    }
}