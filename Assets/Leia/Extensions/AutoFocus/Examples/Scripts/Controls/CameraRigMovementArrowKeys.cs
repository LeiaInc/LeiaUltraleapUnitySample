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
    public class CameraRigMovementArrowKeys : MonoBehaviour
    {
        [SerializeField] private float speed = 5;
        [SerializeField] private float drag = 5;
        private Rigidbody rb = null;
        private Transform childCamera = null;

        void Start()
        {
            rb = transform.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                rb.useGravity = false;
                rb.drag = drag;
            }

            childCamera = GetComponentInChildren<Camera>().transform;
        }

        void LateUpdate()
        {
            MoveCamera(
                -Input.GetAxis("Horizontal"),
                -Input.GetAxis("Vertical")
                );
        }

        public void MoveCamera(float horizontal, float vertical)
        {
            Vector3 controlsMoveVector;
            Quaternion forwardsDirection;

            controlsMoveVector = new Vector3(
                    horizontal * speed * (childCamera.localPosition.z + 10),
                    0,
                    vertical * speed * (childCamera.localPosition.z + 10)
                    );

            forwardsDirection = Quaternion.AngleAxis(transform.eulerAngles.y, Vector3.up);

            Vector3 moveVector = forwardsDirection * controlsMoveVector;

            rb.AddForce(moveVector);
        }
    }
}