using UnityEngine;

public class CornRotateController : MonoBehaviour
{
    public float rotationSpeed = 0.25f;

    public void Rotate(float mouseDifference)
    {
        transform.Rotate(
            0f,
            -mouseDifference * rotationSpeed,
            0f,
            Space.World
        );
    }
}