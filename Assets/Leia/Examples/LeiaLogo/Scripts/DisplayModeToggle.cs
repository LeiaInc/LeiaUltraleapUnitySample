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
    public class DisplayModeToggle : MonoBehaviour
    {
        [SerializeField] Sprite toggleOnSprite;
        [SerializeField] Sprite toggleOffSprite;
        [SerializeField] Image ToggleImage;
        LeiaDisplay leiaDisplay;

        public void Toggle2D3D()
        {
            if (leiaDisplay == null)
            {
                leiaDisplay = FindObjectOfType<LeiaDisplay>();
                if (leiaDisplay == null)
                {
                    Debug.LogError("DisplayModeToggle:Toggle2D3D() LeiaDisplayDoes not exist in scene.");
                    return;
                }
            }
            if (RenderTrackingDevice.Instance.DesiredLightfieldMode == RenderTrackingDevice.LightfieldMode.Off)
            {
                Debug.Log("3D On");
                ToggleImage.sprite = toggleOnSprite;
                RenderTrackingDevice.Instance.DesiredLightfieldMode = RenderTrackingDevice.LightfieldMode.On;
            }
            else
            {
                Debug.Log("3D Off");
                ToggleImage.sprite = toggleOffSprite;
                RenderTrackingDevice.Instance.DesiredLightfieldMode = RenderTrackingDevice.LightfieldMode.Off;
            }
        }
    }
}
