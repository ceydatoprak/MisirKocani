using UnityEngine;

// Tane Onizleme tool'undaki boyut/yaricap/katman ayarlarini bir asset (.asset)
// dosyasi olarak saklar; sahneden bagimsizdir, farkli sahneler/oturumlar arasinda
// tekrar yuklenip uygulanabilir.
[CreateAssetMenu(fileName = "KernelSizePreset", menuName = "Misir Kocani/Tane Boyut Preseti")]
public class KernelSizePreset : ScriptableObject
{
    public Vector3 kernelScale = new Vector3(0.17f, 0.17f, 0.17f);
    public Vector3 kernelScaleMultiplier = Vector3.one;
    public float kernelRadius = 1f;
    public int rows = 12;

    public int sizeLayerCount = 1;
    public float[] sizeLayerScales = { 1f };
    public float[] sizeLayerColumnCounts = { 14f };
    public float[] rowHeightMultipliers = new float[0];
    public float[] rowWidthMultipliers = new float[0];
}
