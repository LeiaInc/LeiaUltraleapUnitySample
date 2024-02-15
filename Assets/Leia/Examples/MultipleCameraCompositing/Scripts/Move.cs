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
    public class Move : MonoBehaviour
    {
        [SerializeField] private Vector3 speed = Vector3.zero;
        [SerializeField] private bool randomizeStartVelocity = false;
        [SerializeField] private bool relativeDirection = false;

        void Start()
        {
            if (randomizeStartVelocity)
            {
                speed = new Vector3(
                    Random.value * speed.x * 2f - speed.x,
                    Random.value * speed.y * 2f - speed.y,
                    Random.value * speed.z * 2f - speed.z
                );
            }
        }
        void Update()
        {
            if (relativeDirection)
            {
                transform.position += transform.rotation * speed * Time.deltaTime;
            }
            else
            {
                transform.position += speed * Time.deltaTime;
            }
        }

        public void SetXSpeed(float xspeed)
        {
            speed = new Vector3(xspeed, speed.y, speed.z);
        }
        public void SetYSpeed(float yspeed)
        {
            speed = new Vector3(speed.x, yspeed, speed.z);
        }
        public void SetZSpeed(float zspeed)
        {
            speed = new Vector3(speed.x, speed.y, zspeed);
        }
    }
}