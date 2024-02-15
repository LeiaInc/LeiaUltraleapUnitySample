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
    public class Shoot : MonoBehaviour
    {
        [SerializeField] private Transform projectilePrefab = null;
        [SerializeField] private Transform[] spawnPoint = null;
        int currentSpawnPoint = 0;
        [SerializeField] private float interval = .3f;
        float timer = 0;

        public void Fire()
        {
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                Instantiate(projectilePrefab, spawnPoint[currentSpawnPoint].position, Quaternion.Euler(spawnPoint[currentSpawnPoint].forward));
                timer = interval;
                currentSpawnPoint++;
                if (currentSpawnPoint >= spawnPoint.Length)
                {
                    currentSpawnPoint = 0;
                }
            }
        }
    }
}