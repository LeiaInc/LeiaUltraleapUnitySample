/*!
* Copyright (C) 2023  Leia, Inc.
*
* This software has been provided under the Leia license agreement.
* You can find the agreement at https://www.leiainc.com/legal/license-agreement
*
* This source code is considered Creator Materials under the definitions of the Leia license agreement.
*/
using UnityEngine;

namespace LeiaLoft.Examples
{
    public class DestroyTimer : MonoBehaviour
    {
        [SerializeField] private float time = 0;
        void Start()
        {
            Destroy(gameObject, time);
        }
    }
}
