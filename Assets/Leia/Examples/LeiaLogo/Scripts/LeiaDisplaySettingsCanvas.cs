/*********************************************************************************************************
*
* Copyright (C) 2024  Leia, Inc.
*
* This software has been provided under the Leia license agreement.
* You can find the agreement at https://www.leiainc.com/legal/license-agreement
*
* This source code is considered Creator Materials under the definitions of the Leia license agreement.
*
*********************************************************************************************************
*/

using UnityEngine;
using UnityEngine.UI;

namespace LeiaUnity.Examples
{
    public class LeiaDisplaySettingsCanvas : MonoBehaviour
    {
#pragma warning disable 649 // Suppress warning that var is never assigned to and will always be null
        [SerializeField] private LeiaDisplay leiaDisplay;
#pragma warning restore 649

        //both
        public Slider DepthFactorSlider;
        public Slider LookAroundSlider;

        //camera centric only
        public Slider FocalDistanceSlider;
        public Slider FOVSlider;

        //display centric only
        public Slider FOVFactorSlider;
        public Slider DisplayPositionZSlider;
        public Slider CameraPositionZSlider;
        public Slider VirtualHeightSlider;

        public bool isCameraDisplaySwitchScene;
        void Start()
        {
            if (leiaDisplay == null)
            {
                leiaDisplay = FindObjectOfType<LeiaDisplay>();
            }

            DepthFactorSlider.value = leiaDisplay.DepthFactor;
            FOVFactorSlider.value = leiaDisplay.FOVFactor;
            FOVSlider.value = leiaDisplay.HeadCamera.fieldOfView;
            DepthFactorSlider.onValueChanged.AddListener(delegate { SetDepth(); });
            LookAroundSlider.onValueChanged.AddListener(delegate { SetLookAround(); });

            FocalDistanceSlider.value = leiaDisplay.FocalDistance;
            bool IsCameraDriven = (leiaDisplay.mode == LeiaDisplay.ControlMode.CameraDriven);

            if (IsCameraDriven)
            {
                FocalDistanceSlider.value = leiaDisplay.FocalDistance;
                FocalDistanceSlider.onValueChanged.AddListener(delegate { SetDisplayDistance(); });

                FOVSlider.value = leiaDisplay.HeadCamera.fieldOfView;
                FOVSlider.onValueChanged.AddListener(delegate { SetFOV(); });
            }
            else
            {
                FOVFactorSlider.value = leiaDisplay.FOVFactor;
                FOVFactorSlider.onValueChanged.AddListener(delegate { SetFOVFactor(); });
            }

            if (isCameraDisplaySwitchScene && IsCameraDriven)
            {
                CameraPositionZSlider.value = leiaDisplay.DriverCamera.transform.position.z;
                CameraPositionZSlider.onValueChanged.AddListener(delegate { SetCameraPositionZ(); });
            }
            else if (isCameraDisplaySwitchScene && !IsCameraDriven)
            {
                DisplayPositionZSlider.value = leiaDisplay.transform.position.z;
                DisplayPositionZSlider.onValueChanged.AddListener(delegate { SetDisplayPositionZ(); });

                VirtualHeightSlider.value = leiaDisplay.VirtualHeight;
                VirtualHeightSlider.onValueChanged.AddListener(delegate { SetVirtualHeight(); });
            }

            FocalDistanceSlider.gameObject.SetActive(IsCameraDriven);
            FOVSlider.gameObject.SetActive(IsCameraDriven);
            FOVFactorSlider.gameObject.SetActive(!IsCameraDriven);
            CameraPositionZSlider.gameObject.SetActive(IsCameraDriven && isCameraDisplaySwitchScene);
            DisplayPositionZSlider.gameObject.SetActive(!IsCameraDriven && isCameraDisplaySwitchScene);
            VirtualHeightSlider.gameObject.SetActive(!IsCameraDriven && isCameraDisplaySwitchScene);
        }

        public void UpdateUI()
        {
            DisplayPositionZSlider.value = leiaDisplay.transform.position.z;

            if (leiaDisplay.DriverCamera != null)
            {
                CameraPositionZSlider.value = leiaDisplay.DriverCamera.transform.position.z;
            }
        }

        void SetDepth()
        {
            leiaDisplay.DepthFactor = DepthFactorSlider.value;
        }
        void SetDisplayDistance()
        {
            leiaDisplay.FocalDistance = FocalDistanceSlider.value;
        }
        void SetFOV()
        {
            leiaDisplay.DriverCamera.fieldOfView = FOVSlider.value;
        }
        void SetFOVFactor()
        {
            leiaDisplay.FOVFactor = FOVFactorSlider.value;
        }
        void SetLookAround()
        {
            leiaDisplay.LookAroundFactor = LookAroundSlider.value;
        }

        void SetCameraPositionZ()
        {
            if(leiaDisplay.DriverCamera != null)
            {
                leiaDisplay.DriverCamera.transform.position = new Vector3(leiaDisplay.DriverCamera.transform.position.x,
                                                                          leiaDisplay.DriverCamera.transform.position.y, 
                                                                          CameraPositionZSlider.value);
            }
        }
        void SetDisplayPositionZ()
        {
            leiaDisplay.transform.position = new Vector3(leiaDisplay.transform.position.x, leiaDisplay.transform.position.y, DisplayPositionZSlider.value);
        }

        void SetVirtualHeight()
        {
            leiaDisplay.VirtualHeight = VirtualHeightSlider.value;
        }
    }
}
