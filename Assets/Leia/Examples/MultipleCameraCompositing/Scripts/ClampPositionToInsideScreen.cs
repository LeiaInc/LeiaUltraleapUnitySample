/*!
* Copyright (C) 2023  Leia, Inc.
*
* This software has been provided under the Leia license agreement.
* You can find the agreement at https://www.leiainc.com/legal/license-agreement
*
* This source code is considered Creator Materials under the definitions of the Leia license agreement.
*/
using UnityEngine;

namespace LeiaUnity.Examples.Asteroids
{
    [DefaultExecutionOrder(100)]
    public class ClampPositionToInsideScreen : MonoBehaviour
    {
        [SerializeField] private Transform screenLeft = null;
        [SerializeField] private Transform screenRight = null;

        void LateUpdate()
        {
            ClampPositionToScreen();
        }

        void ClampPositionToScreen()
        {
            transform.position =
                new Vector3(
                Mathf.Clamp(transform.position.x, screenLeft.position.x, screenRight.position.x),
                transform.position.y,
                transform.position.z
                );
        }
    }
}