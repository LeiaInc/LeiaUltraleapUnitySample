using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LeiaUnity;

namespace LeiaUnity.Examples
{
    public class SetPlaneDistanceForCanvas : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private LeiaDisplay targetDisplay;
        void Update()
        {
            if (targetDisplay.mode == LeiaDisplay.ControlMode.CameraDriven)
            {
                canvas.planeDistance = targetDisplay.FocalDistance;
            }
            else
            {
                Vector3 displayPosition = targetDisplay.transform.position;
                Vector3 cameraPosition = targetDisplay.HeadCamera.transform.position;
                canvas.planeDistance = Vector3.Distance(displayPosition, cameraPosition);
            }
        }
    }
}