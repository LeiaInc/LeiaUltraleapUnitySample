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

namespace LeiaUnity.Examples
{
    public class Rotation : MonoBehaviour
    {
        [SerializeField] private Vector3 rotation = Vector3.zero;

        bool rotationOn = true;

        // Update is called once per frame
        void Update()
        {
            if (rotationOn)
            {
                transform.Rotate(rotation * Time.deltaTime);
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                rotationOn = !rotationOn;
            }
        }
    }
}
