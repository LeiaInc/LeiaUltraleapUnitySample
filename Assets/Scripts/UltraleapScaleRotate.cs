using UnityEngine;
using Leap.Unity;

/// <summary>
/// Controls scaling and rotating of a target object using pinch gestures with the Ultraleap hand tracking.
/// </summary>
public class UltraleapScaleRotate : MonoBehaviour
{
    [SerializeField] private GameObject targetObject;

    [SerializeField] private Vector2 minMaxScale = new Vector2(1f, 2f);
    [SerializeField] private float minPinchStrength = 0.6f;
    [SerializeField] private float scaleRate = 0.05f;
    [SerializeField] private float rotSensitivity = 2.0f;
    [SerializeField] private float maxDistance = 10f;

    private float lastDistance = -1;
    private bool isPinching = false;
    private Quaternion lastRotation;

    private void Update()
    {
        HandlePinchScale();
        HandlePinchRotate();
    }

    /// <summary>
    /// Handles the rotation of the object based on pinch gesture.
    /// </summary>
    private void HandlePinchRotate()
    {
        if (IsPinching())
        {
            Quaternion newRotation = CalculatePinchRotation();
            if (!isPinching)
            {
                lastRotation = newRotation;
                isPinching = true;
            }
            else
            {
                ApplyRotation(newRotation);
            }
        }
        else
        {
            isPinching = false;
        }
    }

    /// <summary>
    /// Checks if both hands are pinching above the minimum pinch strength.
    /// </summary>
    /// <returns>True if both hands are pinching; otherwise, false.</returns>
    private bool IsPinching()
    {
        return Hands.Left != null && Hands.Left.PinchStrength > minPinchStrength &&
               Hands.Right != null && Hands.Right.PinchStrength > minPinchStrength;
    }

    /// <summary>
    /// Calculates the new rotation based on the pinch gesture.
    /// </summary>
    /// <returns>The new rotation as a Quaternion.</returns>
    private Quaternion CalculatePinchRotation()
    {
        var newRotation = Quaternion.LookRotation(Hands.Left.PalmPosition - Hands.Right.PalmPosition);
        return Quaternion.Euler(newRotation.eulerAngles.x * rotSensitivity,
                                newRotation.eulerAngles.y * rotSensitivity,
                                newRotation.eulerAngles.z * rotSensitivity);
    }

    /// <summary>
    /// Applies rotation to the target object based on the difference from the last rotation.
    /// </summary>
    /// <param name="newRotation">The new rotation as a Quaternion.</param>
    private void ApplyRotation(Quaternion newRotation)
    {
        var difference = lastRotation * Quaternion.Inverse(newRotation);
        transform.Rotate(-difference.eulerAngles.z, 0f, 0f, Space.World);
        transform.Rotate(0f, -difference.eulerAngles.y, 0f, Space.World);
        transform.Rotate(0f, 0f, difference.eulerAngles.x, Space.World);
        lastRotation = newRotation;
    }

    /// <summary>
    /// Handles the scaling of the object based on pinch distance.
    /// </summary>
    private void HandlePinchScale()
    {
        if (IsPinching())
        {
            float distance = CalculatePinchDistance();
            if (distance <= maxDistance)
            {
                ApplyScale(distance);
            }
        }
        else
        {
            lastDistance = -1f;
        }
    }

    /// <summary>
    /// Calculates the distance between the index tips of both pinching hands.
    /// </summary>
    /// <returns>The distance as a float.</returns>
    private float CalculatePinchDistance()
    {
        Vector3 leftPos = Hands.Left.GetIndex().TipPosition;
        Vector3 rightPos = Hands.Right.GetIndex().TipPosition;
        return Vector3.Distance(leftPos, rightPos);
    }

    /// <summary>
    /// Applies scaling to the target object based on the change in pinch distance.
    /// </summary>
    /// <param name="currentDistance">The current pinch distance.</param>
    private void ApplyScale(float currentDistance)
    {
        if (lastDistance != -1)
        {
            float distanceDiff = lastDistance - currentDistance;
            float scale = targetObject.transform.localScale.x - distanceDiff * scaleRate;
            scale = Mathf.Clamp(scale, minMaxScale.x, minMaxScale.y);
            targetObject.transform.localScale = new Vector3(scale, scale, scale);
        }
        lastDistance = currentDistance;
    }
}
