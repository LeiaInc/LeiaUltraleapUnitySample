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
    public class CameraRigMouseRotation : MonoBehaviour
    {
#pragma warning disable 414
        [SerializeField, Range(0.01f, 1f)] private float sensitivity = 0.1f;
#pragma warning restore 414
#if UNITY_EDITOR || UNITY_STANDALONE
    private Vector3 startMousePosition = Vector3.zero;
    private Quaternion startRotation = Quaternion.identity;

    void LateUpdate()
    {
        if (Input.GetMouseButtonDown(1))
        {
            startMousePosition = Input.mousePosition;
            startRotation = transform.rotation;
        }

        if (Input.GetMouseButton(1))
        {
            float deltaMousePositionX = Input.mousePosition.x - startMousePosition.x;
            float deltaMousePositionY = Input.mousePosition.y - startMousePosition.y;

            transform.rotation = Quaternion.Euler(
                startRotation.eulerAngles.x - deltaMousePositionY * sensitivity,
                startRotation.eulerAngles.y + deltaMousePositionX * sensitivity,
                0);
        }
    }
#endif
    }
}
