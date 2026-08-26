using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Play moduna girmeden, KernelSpawner/KernelTemplate uzerinden tane
// olcegini/yaricapini/katmanli boyutunu ayarlayip Misir Kocani uzerinde onizlemeyi saglar.
// "Onayla" ile ayni degerler Play modda da (Start() yeniden uretiminde) aynen kullanilir.
// Ayarlar ayrica bir KernelSizePreset ScriptableObject asset'ine kaydedilip/yuklenebilir.
public class KernelPreviewWindow : EditorWindow
{
    private KernelSpawner spawner;
    private Transform kernelTemplate;
    private SphereCollider templateCollider;
    private MeshRenderer templateRenderer;

    private Vector3 kernelScale;
    private Vector3 kernelScaleMultiplier;
    private float kernelRadius = 1f;

    private int sizeLayerCount = 1;
    private float[] sizeLayerScales = { 1f };
    private float[] sizeLayerColumnCounts = { 14f };

    private KernelSizePreset preset;
    private Vector2 scrollPosition;
    private bool livePreviewEnabled;

    [MenuItem("Tools/Misir Kocani/Tane Onizleme")]
    private static void Open()
    {
        KernelPreviewWindow window = GetWindow<KernelPreviewWindow>("Tane Onizleme");
        window.minSize = new Vector2(340, 300);
        window.Refresh();
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
            sizeLayerCount = Mathf.Max(1, spawner.sizeLayerCount);
            sizeLayerScales = SyncLayerArray(spawner.sizeLayerScales, sizeLayerCount, 1f);
            sizeLayerColumnCounts = SyncLayerArray(spawner.sizeLayerColumnCounts, sizeLayerCount, spawner.columns);
        }
        if (templateCollider != null)
        {
            kernelRadius = templateCollider.radius;
        }
        Repaint();
    }

    private static float[] SyncLayerArray(float[] source, int count, float fallback)
    {
        float[] result = new float[count];
        for (int i = 0; i < count; i++)
        {
            result[i] = (source != null && i < source.Length) ? source[i] : fallback;
        }
        return result;
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

        // Katman sayisi arttikca icerik pencere boyunu asabilir; ScrollView
        // olmadan alttaki butonlar gorunmez alana kayardi.
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.LabelField("Misir Tanesi Onizleme", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Degerleri ayarla ve 'Onizle' ile Play'e girmeden Kocan uzerinde gor. " +
            "'Onayla' ayni degerlerin Play modda da aynen uretilmesini saglar.",
            MessageType.None);

        EditorGUILayout.Space();
        Color liveButtonColor = GUI.backgroundColor;
        GUI.backgroundColor = livePreviewEnabled ? new Color(1f, 0.6f, 0.25f) : liveButtonColor;
        string liveButtonLabel = livePreviewEnabled
            ? "Canli Onizleme: ACIK (kapatmak icin tikla)"
            : "Canli Onizleme: KAPALI (acmak icin tikla)";
        if (GUILayout.Button(liveButtonLabel, GUILayout.Height(30)))
        {
            livePreviewEnabled = !livePreviewEnabled;
            if (livePreviewEnabled)
            {
                ApplyToSourceOfTruth();
                RegenerateAndShow();
            }
        }
        GUI.backgroundColor = liveButtonColor;
        if (livePreviewEnabled)
        {
            EditorGUILayout.HelpBox(
                "Canli onizleme acik: asagidaki degerlerden herhangi birini degistirdiginde " +
                "Kocan uzerinde Play'e girmeden aninda guncellenir.",
                MessageType.Info);
        }

        // Bu blok icindeki herhangi bir alan degisirse (slider surukleme dahil) ve canli
        // onizleme aciksa, asagida TEK bir apply+regenerate cagrisi tetiklenir.
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.Space();
        kernelScale = EditorGUILayout.Vector3Field("Tane Olcegi", kernelScale);
        kernelScaleMultiplier = EditorGUILayout.Vector3Field("Olcek Carpani", kernelScaleMultiplier);
        kernelRadius = EditorGUILayout.Slider("Tane Yaricapi", kernelRadius, 0.01f, 2f);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Yukseklige Gore Katmanli Boyut ve Tane Sayisi (Alttan Usta)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Her katman, yukseklik boyunca bir kontrol noktasidir (Katman 1 = en alt, son katman = en ust). " +
            "Aralarinda yumusak (smoothstep) gecis uygulanir, yani katman sinirlari goze BATMAZ; " +
            "sonuc tek parca, surekli bir egri gibi gorunur. Kocanin gercek olculerinden bagimsizdir, " +
            "tamamen elle kontrol edilir. Bir katmanda boyutu buyutuyorsan, ayni katmanin Tane Sayisini " +
            "azaltarak tanelerin birbirine girmesini (ustuste binmesini) onleyebilirsin.",
            MessageType.None);

        int newLayerCount = Mathf.Max(1, EditorGUILayout.IntField("Katman Sayisi", sizeLayerCount));
        if (newLayerCount != sizeLayerCount)
        {
            sizeLayerCount = newLayerCount;
            sizeLayerScales = SyncLayerArray(sizeLayerScales, sizeLayerCount, 1f);
            sizeLayerColumnCounts = SyncLayerArray(sizeLayerColumnCounts, sizeLayerCount, spawner.columns);
        }

        EditorGUI.indentLevel++;
        for (int i = 0; i < sizeLayerScales.Length; i++)
        {
            string positionSuffix;
            if (i == 0)
                positionSuffix = " - En Alt";
            else if (i == sizeLayerScales.Length - 1)
                positionSuffix = " - En Ust";
            else
                positionSuffix = "";

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Katman " + (i + 1) + positionSuffix, EditorStyles.boldLabel);
                sizeLayerScales[i] = EditorGUILayout.Slider("Boyut Carpani", sizeLayerScales[i], 0.1f, 2f);
                float columnCount = EditorGUILayout.Slider("Tane Sayisi", sizeLayerColumnCounts[i], 3f, 40f);
                sizeLayerColumnCounts[i] = Mathf.Round(columnCount);
            }
        }
        EditorGUI.indentLevel--;

        if (EditorGUI.EndChangeCheck() && livePreviewEnabled)
        {
            ApplyToSourceOfTruth();
            RegenerateAndShow();
        }

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
        }
        GUI.backgroundColor = previousColor;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Boyut Preseti (ScriptableObject)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Yukaridaki degerleri sahneden bagimsiz bir .asset dosyasina kaydeder; " +
            "farkli sahnelerde/oturumlarda tekrar yukleyip uygulayabilirsin.",
            MessageType.None);
        preset = (KernelSizePreset)EditorGUILayout.ObjectField(
            "Preset", preset, typeof(KernelSizePreset), false);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Preset'e Kaydet"))
            {
                SaveToPreset();
            }
            GUI.enabled = preset != null;
            if (GUILayout.Button("Preset'ten Yukle"))
            {
                LoadFromPreset();
            }
            GUI.enabled = true;
        }

        EditorGUILayout.Space();
        GUI.backgroundColor = new Color(0.6f, 0.75f, 1f);
        if (GUILayout.Button("Sahneyi Kaydet", GUILayout.Height(28)))
        {
            EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);
            EditorSceneManager.SaveScene(spawner.gameObject.scene);
            Debug.Log("Sahne kaydedildi.");
        }
        GUI.backgroundColor = previousColor;

        EditorGUILayout.Space();
        if (GUILayout.Button("Sahneden Yenile"))
        {
            Refresh();
        }

        EditorGUILayout.Space();
        EditorGUILayout.EndScrollView();
    }

    private void ApplyToSourceOfTruth()
    {
        Undo.RecordObject(spawner, "Tane Olcegi/Yaricapi/Katman Ayarla");
        spawner.kernelScale = kernelScale;
        spawner.kernelScaleMultiplier = kernelScaleMultiplier;
        spawner.sizeLayerCount = sizeLayerCount;
        spawner.sizeLayerScales = (float[])sizeLayerScales.Clone();
        spawner.sizeLayerColumnCounts = (float[])sizeLayerColumnCounts.Clone();

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

    private void SaveToPreset()
    {
        if (preset == null)
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Boyut Preseti Kaydet",
                "KernelSizePreset",
                "asset",
                "Yeni preset dosyasinin adini ve konumunu sec.");
            if (string.IsNullOrEmpty(path))
                return;

            preset = ScriptableObject.CreateInstance<KernelSizePreset>();
            AssetDatabase.CreateAsset(preset, path);
        }

        Undo.RecordObject(preset, "Boyut Preseti Kaydet");
        preset.kernelScale = kernelScale;
        preset.kernelScaleMultiplier = kernelScaleMultiplier;
        preset.kernelRadius = kernelRadius;
        preset.sizeLayerCount = sizeLayerCount;
        preset.sizeLayerScales = (float[])sizeLayerScales.Clone();
        preset.sizeLayerColumnCounts = (float[])sizeLayerColumnCounts.Clone();

        EditorUtility.SetDirty(preset);
        AssetDatabase.SaveAssets();
        Debug.Log("Boyut preseti kaydedildi: " + AssetDatabase.GetAssetPath(preset));
    }

    private void LoadFromPreset()
    {
        if (preset == null)
        {
            Debug.LogWarning("Once bir preset sec veya 'Preset'e Kaydet' ile olustur.");
            return;
        }

        kernelScale = preset.kernelScale;
        kernelScaleMultiplier = preset.kernelScaleMultiplier;
        kernelRadius = preset.kernelRadius;
        sizeLayerCount = Mathf.Max(1, preset.sizeLayerCount);
        sizeLayerScales = SyncLayerArray(preset.sizeLayerScales, sizeLayerCount, 1f);
        sizeLayerColumnCounts = SyncLayerArray(preset.sizeLayerColumnCounts, sizeLayerCount, spawner.columns);
        Repaint();
    }
}
