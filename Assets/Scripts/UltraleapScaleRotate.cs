using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;
using LeiaUnity;


public class UltraleapScaleRotate : MonoBehaviour
{
    public Vector2 MinMaxScale = new Vector2(1f, 2f);
    public float MinPinchStrength = .6f;
    public float ScaleRate = 0.05f;
    public float RotSensitivity = 2.0f;
    private float lastDistance = -1;
    public float minDifference = .01f;
    public float MaxDistance = 10f;
    public GameObject targetObject;
    private bool _isPinching = false;
    private Quaternion _lastRotation;


    private void Update()
    {
        PinchScale();
        PinchRotate();

    }

    private void PinchRotate()
    {
        if ((Hands.Left != null && Hands.Left.PinchStrength > MinPinchStrength) &&
        (Hands.Right != null && Hands.Right.PinchStrength > MinPinchStrength))
        {
            var newRotation = Quaternion.LookRotation(Hands.Left.PalmPosition - Hands.Right.PalmPosition);
            var sensitiveNewRot = Quaternion.Euler(newRotation.eulerAngles.x * RotSensitivity,
                                                   newRotation.eulerAngles.y * RotSensitivity,
                                                   newRotation.eulerAngles.z * RotSensitivity);
            if (!_isPinching)
            {
                _lastRotation = sensitiveNewRot;
                _isPinching = true;
            }
            else
            {
                var difference = _lastRotation * Quaternion.Inverse(sensitiveNewRot);
                transform.Rotate(-difference.eulerAngles.z, 0f, 0f, Space.World);
                transform.Rotate(0f, -difference.eulerAngles.y, 0f, Space.World);
                transform.Rotate(0f, 0f, difference.eulerAngles.x, Space.World);
                _lastRotation = sensitiveNewRot;

            }
        }
        else
        {
            _isPinching = false;
        }
    }


    private void PinchScale()
    {
        if ((Hands.Left != null && Hands.Left.PinchStrength > MinPinchStrength) &&
            (Hands.Right != null && Hands.Right.PinchStrength > MinPinchStrength))
        {
            Vector3 leftPos = Hands.Left.GetIndex().TipPosition;
            Vector3 rightPos = Hands.Right.GetIndex().TipPosition;
            float distance = Vector3.Distance(leftPos, rightPos);

            if (distance > MaxDistance)
                return;
            if (lastDistance != -1)
            {
                var distanceDiff = lastDistance - distance;
                float scale = targetObject.transform.localScale.x - distanceDiff * 1.25f;
                scale = Mathf.Clamp(scale, MinMaxScale.x, MinMaxScale.y);
                targetObject.transform.localScale = new Vector3(scale, scale, scale);
            }

            lastDistance = distance;
        }
        else
        {
            lastDistance = -1f;
        }
    }

}