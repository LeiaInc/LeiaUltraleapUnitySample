/*!
* Copyright (C) 2023  Leia, Inc.
*
* This software has been provided under the Leia license agreement.
* You can find the agreement at https://www.leiainc.com/legal/license-agreement
*
* This source code is considered Creator Materials under the definitions of the Leia license agreement.
*/
using System.Collections;
using UnityEngine;

namespace LeiaUnity.Examples.Asteroids
{
    public class AsteroidSpawner : MonoBehaviour
    {
        [SerializeField] private Transform asteroidPrefab = null;
        [SerializeField] private Vector3 size = Vector3.zero;
        [SerializeField] private float spawnInterval = 1f;

        void Start()
        {
            StartSpawnTimer(spawnInterval);
        }

        void StartSpawnTimer(float waitTime)
        {
            IEnumerator timer = SpawnTimer(waitTime);
            StartCoroutine(timer);
        }
        IEnumerator SpawnTimer(float waitTime)
        {
            yield return new WaitForSeconds(waitTime);
            //Spawn Asteroid
            Instantiate(asteroidPrefab, transform.position + new Vector3(
                Random.value * size.x - size.x / 2f,
                Random.value * size.y - size.y / 2f,
                Random.value * size.z - size.z / 2f),
                Quaternion.identity
            );
            StartSpawnTimer(spawnInterval);
        }

        void OnDrawGizmos()
        {
            Gizmos.DrawWireCube(transform.position, size);
        }
    }
}