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
    [DefaultExecutionOrder(1000)]
    public class CameraRigBoundary : MonoBehaviour
    {
#pragma warning disable 649
        [SerializeField] private Vector3 dimensions;
        [SerializeField] private bool showBounds;
#pragma warning restore 649

        void LateUpdate()
        {
            transform.position = new Vector3(
                Mathf.Clamp(transform.position.x, -dimensions.x / 2f, dimensions.x / 2f),
                Mathf.Clamp(transform.position.y, -dimensions.y / 2f, dimensions.y / 2f),
                Mathf.Clamp(transform.position.z, -dimensions.z / 2f, dimensions.z / 2f)
                );
        }

        void OnDrawGizmos()
        {
            if (showBounds)
            {
                Gizmos.DrawWireCube(Vector3.zero, dimensions);
            }
        }
    }
}
