using UnityEngine;

// Ekranin kosesinde anlik FPS gosteren basit bir debug overlay'i.
// UI/Canvas kurulumu gerektirmez, OnGUI ile cizilir.
public class FpsOverlay : MonoBehaviour
{
    [SerializeField] private float updateInterval = 0.5f;
    [SerializeField] private int fontSize = 42;

    private float accumulatedTime;
    private int frameCount;
    private float currentFps;

    private GUIStyle style;

    private void Update()
    {
        accumulatedTime += Time.unscaledDeltaTime;
        frameCount++;

        if (accumulatedTime >= updateInterval)
        {
            currentFps = frameCount / accumulatedTime;
            accumulatedTime = 0f;
            frameCount = 0;
        }
    }

    private void OnGUI()
    {
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                fontStyle = FontStyle.Bold
            };
            style.normal.textColor = currentFps >= 30f ? Color.green : Color.red;
        }

        style.normal.textColor = currentFps >= 30f ? Color.green : Color.red;

        GUI.Label(new Rect(20, 20, 400, fontSize + 20), "FPS: " + currentFps.ToString("0"), style);
    }
}
