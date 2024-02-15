/*!
* Copyright (C) 2023  Leia, Inc.
*
* This software has been provided under the Leia license agreement.
* You can find the agreement at https://www.leiainc.com/legal/license-agreement
*
* This source code is considered Creator Materials under the definitions of the Leia license agreement.
*/
using UnityEngine;
using UnityEngine.Events;


namespace LeiaUnity.Examples.Asteroids
{
    public class ProjectileCollision : MonoBehaviour
    {
        [SerializeField] private UnityEvent action = null;

        void OnCollisionEnter(Collision other)
        {
            if (other.gameObject.GetComponent<Projectile>())
            {
                action.Invoke();
                Destroy(other.gameObject);
            }
        }
    }
}