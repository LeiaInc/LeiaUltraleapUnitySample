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
    public class TrafficSpawner : MonoBehaviour
    {
#pragma warning disable 649
        [SerializeField] private Transform carPrefab;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private float spawnInterval = 3f;
#pragma warning restore 649

        void OnEnable()
        {
            SpawnCarTimer();
        }

        public void SpawnCarTimer()
        {
            if (this.enabled)
            {
                int chosenSpawnPoint = (int)(Random.value * spawnPoints.Length);
                Instantiate(carPrefab, spawnPoints[chosenSpawnPoint].position, spawnPoints[chosenSpawnPoint].rotation);
                Invoke("SpawnCarTimer", spawnInterval);
            }
        }
    }
}