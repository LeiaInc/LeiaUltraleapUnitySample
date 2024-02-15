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
    public class UpdateSliderLabel : MonoBehaviour
    {
        [SerializeField] private Text label = null;
        [SerializeField] private Slider slider = null;
        [SerializeField] private string valueName = "";

        // Start is called before the first frame update
        void Start()
        {
            slider.onValueChanged.AddListener(UpdateLabel);
            UpdateLabel(slider.value);
        }

        public void UpdateLabel(float value)
        {
            label.text = string.Format(
                "{0}: {1}",
                valueName,
                slider.value.ToString("F1")
                );
        }
    }
}
