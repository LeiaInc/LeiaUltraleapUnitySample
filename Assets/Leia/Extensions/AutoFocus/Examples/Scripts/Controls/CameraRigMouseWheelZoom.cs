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
    [DefaultExecutionOrder(1)]
    public class CameraRigMouseWheelZoom : MonoBehaviour
    {
        [SerializeField] MinMaxPair perspZoomRange = new MinMaxPair(10, 0, "MinZoom", 50, float.MaxValue, "Max zoom");
        [SerializeField] MinMaxPair orthoZoomRange = new MinMaxPair(1, 0, "Min ortho zoom", 10, float.MaxValue, "Max ortho zoom");

        [SerializeField] private float zoom = 20;
        [SerializeField] private float zoomSpeed = 10;
        private float zoomTarget = 20;
        private Camera childCamera = null;

        bool zooming;

        void Start()
        {
            childCamera = GetComponentInChildren<Camera>();

            if (childCamera.orthographic)
            {
                zoom = childCamera.orthographicSize;
            }
            else
            {
                zoom = -transform.GetChild(0).localPosition.z;
            }
            zoomTarget = zoom;
        }

        void LateUpdate()
        {
            if (EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            if (Mathf.Abs(Input.mouseScrollDelta.y) > 0)
            {
                zooming = true;
            }

            if (childCamera.orthographic)
            {
                zoom = childCamera.orthographicSize;
                zoomTarget -= Input.mouseScrollDelta.y * (zoom / 50f);
                zoomTarget = Mathf.Clamp(zoomTarget, orthoZoomRange.min, orthoZoomRange.max);
                
                if (zooming)
                {
                    zoom += (zoomTarget - zoom) * Mathf.Min(Time.deltaTime * zoomSpeed, 1f);
                    if (Mathf.Abs(zoomTarget - zoom) < .001f)
                    {
                        zooming = false;
                    }
                }

                childCamera.orthographicSize = zoom;
            }
            else
            {
                zoom = -transform.GetChild(0).localPosition.z;
                zoomTarget -= Input.mouseScrollDelta.y * zoomSpeed * (zoom / 50f);
                zoomTarget = Mathf.Clamp(zoomTarget, perspZoomRange.min, perspZoomRange.max);

                if (zooming)
                {
                    zoom += (zoomTarget - zoom) * Mathf.Min(Time.deltaTime * zoomSpeed, 1f);
                    if (Mathf.Abs(zoomTarget - zoom) < .001f)
                    {
                        zooming = false;
                    }
                }
                
                childCamera.transform.localPosition = new Vector3(
                    0,
                    0,
                    -zoom
                    );
            }
        }
    }
}
