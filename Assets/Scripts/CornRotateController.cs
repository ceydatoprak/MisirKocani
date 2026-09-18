using UnityEngine;

public class CornRotateController : MonoBehaviour
{
    [Tooltip("Misirin kendi kendine donme hizi (derece/saniye). Cok hizli/cok yavas olmayacak sekilde ayarlanmalidir.")]
    public float autoRotateSpeed = 20f;

    // Iki yaprak da acilip mısır soyulabilir hale gelene kadar donmeye baslamaz
    
    private bool isRotating = false;

    public void StartRotating()
    {
        isRotating = true;
    }

    private void Update()
    {
        if (!isRotating)
            return;

        transform.Rotate(
            0f,
            autoRotateSpeed * Time.deltaTime,
            0f,
            Space.World
        );
    }
}
