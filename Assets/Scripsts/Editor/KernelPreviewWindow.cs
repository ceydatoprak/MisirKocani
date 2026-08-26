using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Play moduna girmeden, KernelSpawner/KernelTemplate uzerinden tane
// olcegini/yaricapini ayarlayip Misir Kocani uzerinde onizlemeyi saglar.
// "Onayla" ile ayni degerler Play modda da (Start() yeniden uretiminde) aynen kullanilir.
public class KernelPreviewWindow : EditorWindow
{
    private KernelSpawner spawner;
    private Transform kernelTemplate;
    private SphereCollider templateCollider;
    private MeshRenderer templateRenderer;

    private Vector3 kernelScale;
    private Vector3 kernelScaleMultiplier;
    private float kernelRadius = 1f;

    [MenuItem("Tools/Misir Kocani/Tane Onizleme")]
    private static void Open()
    {
        GetWindow<KernelPreviewWindow>("Tane Onizleme").Refresh();
    }

    // Selection.activeGameObject'e bagimli olmadigi icin uzaktan/otomasyonla
    // tetiklemede (ornegin MCP koprusunden) daha guvenilir: sahnedeki tek
    // KernelSpawner'i doğrudan bulup yeniden uretir.
    [MenuItem("Tools/Misir Kocani/Aktif Sahnedeki Taneleri Yeniden Uret")]
    private static void RegenerateActiveSceneKernels()
    {
        KernelSpawner activeSpawner = FindObjectOfType<KernelSpawner>();
        if (activeSpawner == null)
        {
            Debug.LogWarning("Aktif sahnede KernelSpawner bulunamadi.");
            return;
        }
        Undo.RegisterFullObjectHierarchyUndo(activeSpawner.transform.root.gameObject, "Taneleri Yeniden Uret");
        activeSpawner.SpawnKernels();
        EditorUtility.SetDirty(activeSpawner);
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void Refresh()
    {
        spawner = FindObjectOfType<KernelSpawner>();
        kernelTemplate = spawner != null ? spawner.transform.Find("KernelTemplate") : null;
        templateCollider = kernelTemplate != null ? kernelTemplate.GetComponent<SphereCollider>() : null;
        templateRenderer = kernelTemplate != null ? kernelTemplate.GetComponent<MeshRenderer>() : null;

        if (spawner != null)
        {
            kernelScale = spawner.kernelScale;
            kernelScaleMultiplier = spawner.kernelScaleMultiplier;
        }
        if (templateCollider != null)
        {
            kernelRadius = templateCollider.radius;
        }
        Repaint();
    }

    private void OnGUI()
    {
        if (spawner == null || kernelTemplate == null || templateCollider == null || templateRenderer == null)
        {
            EditorGUILayout.HelpBox(
                "Sahnede KernelSpawner ve/veya KernelTemplate bulunamadi.",
                MessageType.Warning);
            if (GUILayout.Button("Yenile"))
                Refresh();
            return;
        }

        EditorGUILayout.LabelField("Misir Tanesi Onizleme", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Degerleri ayarla ve 'Onizle' ile Play'e girmeden Kocan uzerinde gor. " +
            "'Onayla' ayni degerlerin Play modda da aynen uretilmesini saglar.",
            MessageType.None);

        EditorGUILayout.Space();
        kernelScale = EditorGUILayout.Vector3Field("Tane Olcegi", kernelScale);
        kernelScaleMultiplier = EditorGUILayout.Vector3Field("Olcek Carpani", kernelScaleMultiplier);
        kernelRadius = EditorGUILayout.Slider("Tane Yaricapi", kernelRadius, 0.01f, 2f);

        EditorGUILayout.Space();
        if (GUILayout.Button("Onizle / Guncelle", GUILayout.Height(28)))
        {
            ApplyToSourceOfTruth();
            RegenerateAndShow();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField(
            "Asagidaki sadece editor onizlemesini gizler, onaylanmis ayarlari degistirmez:",
            EditorStyles.miniLabel);
        if (GUILayout.Button("Taneleri Gizle (Sadece Onizleme)"))
        {
            SetPreviewRenderersEnabled(false);
        }

        EditorGUILayout.Space();
        Color previousColor = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
        if (GUILayout.Button("Onayla (Play Modda Ayni Gorunsun)", GUILayout.Height(32)))
        {
            ApplyToSourceOfTruth();
            RegenerateAndShow();
            EditorUtility.SetDirty(spawner);
            EditorUtility.SetDirty(templateCollider);
            EditorUtility.SetDirty(templateRenderer);
            Debug.Log("Tane ayarlari onaylandi: Play moda girildiginde ayni sekilde uretilecek.");

            if (EditorUtility.DisplayDialog(
                    "Sahneyi Kaydet",
                    "Ayarlar onaylandi. Degisiklikleri kalici yapmak icin sahneyi simdi kaydetmek ister misin?",
                    "Kaydet",
                    "Simdi Degil"))
            {
                EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);
                EditorSceneManager.SaveScene(spawner.gameObject.scene);
            }
        }
        GUI.backgroundColor = previousColor;

        EditorGUILayout.Space();
        if (GUILayout.Button("Sahneden Yenile"))
        {
            Refresh();
        }
    }

    private void ApplyToSourceOfTruth()
    {
        Undo.RecordObject(spawner, "Tane Olcegi/Yaricapi Ayarla");
        spawner.kernelScale = kernelScale;
        spawner.kernelScaleMultiplier = kernelScaleMultiplier;

        Undo.RecordObject(templateCollider, "Tane Yaricapi Ayarla");
        templateCollider.radius = kernelRadius;

        // Sablonun MeshRenderer'i kapaliysa Instantiate ile klonlanan her tane de
        // kapali dogar; onizleme/onay her zaman gorunur baslamasini garanti eder.
        Undo.RecordObject(templateRenderer, "Tane Gorunurlugu Ac");
        templateRenderer.enabled = true;
    }

    private void RegenerateAndShow()
    {
        spawner.SpawnKernels();
        SetPreviewRenderersEnabled(true);
    }

    private void SetPreviewRenderersEnabled(bool value)
    {
        MeshRenderer[] renderers = spawner.GetComponentsInChildren<MeshRenderer>(true);
        foreach (MeshRenderer kernelRenderer in renderers)
        {
            if (kernelRenderer.transform == kernelTemplate)
                continue;
            Undo.RecordObject(kernelRenderer, "Tane Onizleme Gorunurlugu");
            kernelRenderer.enabled = value;
        }
    }
}
