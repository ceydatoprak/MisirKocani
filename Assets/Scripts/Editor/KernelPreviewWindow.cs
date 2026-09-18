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
    private int rows = 12;

    private int sizeLayerCount = 1;
    private float[] sizeLayerScales = { 1f };
    private float[] sizeLayerColumnCounts = { 14f };
    private float[] rowHeightMultipliers = new float[0];
    private float[] rowWidthMultipliers = new float[0];

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

    // Elle satir satir ayarlanan degerlerdeki (sizeLayerScales, sizeLayerColumnCounts,
    // rowHeightMultipliers, rowWidthMultipliers) ani/tek-satirlik sicramalari komsu satirlarla
    // harmanlayarak azaltir. Onden/yandan bakildiginda kenarlarin "yamuk" gorunmesinin ana sebebi
    // budur: sizeLayerCount == rows oldugunda katmanlar arasi smoothstep gecisi devre disi kalir
    // (her satir kendi kontrol noktasi olur) ve elle girilen degerler arasinda hic yumusatma
    // uygulanmaz. Genel egilimi/sekli BOZMAZ (agirlikli ortalama), sadece komsu satirlar arasi
    // sert farkin bir kismini alir. RegenerateActiveSceneKernels gibi Selection'a bagimli degildir.
    [MenuItem("Tools/Misir Kocani/Aktif Sahnedeki Satirlari Puruzsuzlestir")]
    private static void SmoothActiveSceneRows()
    {
        KernelSpawner activeSpawner = FindObjectOfType<KernelSpawner>();
        if (activeSpawner == null)
        {
            Debug.LogWarning("Aktif sahnede KernelSpawner bulunamadi.");
            return;
        }
        Undo.RegisterFullObjectHierarchyUndo(activeSpawner.transform.root.gameObject, "Satirlari Puruzsuzlestir");
        Undo.RecordObject(activeSpawner, "Satirlari Puruzsuzlestir");

        activeSpawner.sizeLayerScales = SmoothArray(activeSpawner.sizeLayerScales);
        activeSpawner.sizeLayerColumnCounts = SmoothArray(activeSpawner.sizeLayerColumnCounts);
        activeSpawner.rowHeightMultipliers = SmoothArray(activeSpawner.rowHeightMultipliers);
        activeSpawner.rowWidthMultipliers = SmoothArray(activeSpawner.rowWidthMultipliers);

        activeSpawner.SpawnKernels();
        EditorUtility.SetDirty(activeSpawner);
        Debug.Log("Satir bazli degerler puruzsuzlestirildi ve taneler yeniden uretildi.");
    }

    // 3 noktali agirlikli hareketli ortalama (0.25 / 0.5 / 0.25), kenarlar clamp edilir (dizi
    // disina tasan komsu, en uctaki degerin kendisiyle doldurulur). TEK GECIS: genel egri/sekli
    // korur, sadece komsu satirlar arasindaki ani (tek satirlik) sicramalarin bir kismini alir.
    // Ust uste birkac kez cagirmak etkisini guclendirir (daha fazla puruzsuzlestirme).
    private static float[] SmoothArray(float[] values)
    {
        if (values == null || values.Length <= 2)
            return values;

        float[] result = new float[values.Length];
        for (int i = 0; i < values.Length; i++)
        {
            float prev = values[Mathf.Max(0, i - 1)];
            float curr = values[i];
            float next = values[Mathf.Min(values.Length - 1, i + 1)];
            result[i] = prev * 0.25f + curr * 0.5f + next * 0.25f;
        }
        return result;
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
            rows = Mathf.Max(1, spawner.rows);
            sizeLayerCount = Mathf.Max(1, spawner.sizeLayerCount);
            sizeLayerScales = SyncLayerArray(spawner.sizeLayerScales, sizeLayerCount, 1f);
            sizeLayerColumnCounts = SyncLayerArray(spawner.sizeLayerColumnCounts, sizeLayerCount, spawner.columns);
            rowHeightMultipliers = SyncLayerArray(spawner.rowHeightMultipliers, rows, 1f);
            rowWidthMultipliers = SyncLayerArray(spawner.rowWidthMultipliers, rows, 1f);
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
        rows = Mathf.Max(1, EditorGUILayout.IntField("Dikey Satir Sayisi", rows));
        EditorGUILayout.HelpBox(
            "Kocanin dikeyde kac satir taneden olusacagini belirler (alttan usta). " +
            "Taneyi kucultunce satirlar arasi bosluk artar; bunu artirarak bosluklari " +
            "daha fazla satirla doldurabilirsin.",
            MessageType.None);
        rowHeightMultipliers = SyncLayerArray(rowHeightMultipliers, rows, 1f);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Satir Bazli Elle Yukseklik (Y) Carpani (Alttan Usta)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "SADECE tanenin yuksekligini (Y ekseni) etkiler; genislik/derinlik (X/Z) degismez. " +
            "Yukaridaki 'Katman' sistemi satirlar arasinda yumusak gecis yapar; burada ise HER " +
            "satirin yuksekligini birbirinden bagimsiz, ayri ayri belirleyebilirsin (aralarinda " +
            "yumusatma yoktur). 1 = degisiklik yok. Diger boyut carpanlarinin UZERINE eklenir.",
            MessageType.None);

        EditorGUI.indentLevel++;
        for (int i = 0; i < rowHeightMultipliers.Length; i++)
        {
            string positionSuffix;
            if (i == 0)
                positionSuffix = " - En Alt";
            else if (i == rowHeightMultipliers.Length - 1)
                positionSuffix = " - En Ust";
            else
                positionSuffix = "";

            rowHeightMultipliers[i] = EditorGUILayout.Slider(
                "Satir " + i.ToString("D2") + positionSuffix,
                rowHeightMultipliers[i],
                0.05f,
                2f);
        }
        EditorGUI.indentLevel--;

        rowWidthMultipliers = SyncLayerArray(rowWidthMultipliers, rows, 1f);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Satir Bazli Elle Genislik (X) Carpani (Alttan Usta)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "SADECE tanenin genisligini (X ekseni) etkiler; yukseklik/derinlik (Y/Z) degismez. " +
            "Yukaridaki yukseklik ayariyla ayni mantik: satirlar arasinda yumusatma yoktur, her " +
            "satirin genisligini birbirinden bagimsiz, ayri ayri belirleyebilirsin. 1 = degisiklik " +
            "yok. Diger boyut carpanlarinin UZERINE eklenir.",
            MessageType.None);

        EditorGUI.indentLevel++;
        for (int i = 0; i < rowWidthMultipliers.Length; i++)
        {
            string positionSuffix;
            if (i == 0)
                positionSuffix = " - En Alt";
            else if (i == rowWidthMultipliers.Length - 1)
                positionSuffix = " - En Ust";
            else
                positionSuffix = "";

            rowWidthMultipliers[i] = EditorGUILayout.Slider(
                "Satir " + i.ToString("D2") + positionSuffix,
                rowWidthMultipliers[i],
                0.05f,
                2f);
        }
        EditorGUI.indentLevel--;

        EditorGUILayout.Space();
        if (GUILayout.Button("Satirlari Puruzsuzlestir (Kenar Yamukluklarini Azalt)", GUILayout.Height(28)))
        {
            sizeLayerScales = SmoothArray(sizeLayerScales);
            sizeLayerColumnCounts = SmoothArray(sizeLayerColumnCounts);
            rowHeightMultipliers = SmoothArray(rowHeightMultipliers);
            rowWidthMultipliers = SmoothArray(rowWidthMultipliers);
            ApplyToSourceOfTruth();
            RegenerateAndShow();
        }
        EditorGUILayout.HelpBox(
            "Elle satir satir girdigin degerlerdeki ani (tek satirlik) sicramalari komsu " +
            "satirlarla hafifce harmanlayarak azaltir; genel sekli/egilimi BOZMAZ, sadece " +
            "onden/yandan bakildiginda kenarlarin yamuk gorunmesine yol acan puruzu alir. " +
            "Ust uste birkac kez basarsan etki katlanarak artar (daha guclu puruzsuzlestirme).",
            MessageType.None);

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
        spawner.rows = Mathf.Max(1, rows);
        spawner.sizeLayerCount = sizeLayerCount;
        spawner.sizeLayerScales = (float[])sizeLayerScales.Clone();
        spawner.sizeLayerColumnCounts = (float[])sizeLayerColumnCounts.Clone();
        spawner.rowHeightMultipliers = (float[])rowHeightMultipliers.Clone();
        spawner.rowWidthMultipliers = (float[])rowWidthMultipliers.Clone();

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
        preset.rows = rows;
        preset.sizeLayerCount = sizeLayerCount;
        preset.sizeLayerScales = (float[])sizeLayerScales.Clone();
        preset.sizeLayerColumnCounts = (float[])sizeLayerColumnCounts.Clone();
        preset.rowHeightMultipliers = (float[])rowHeightMultipliers.Clone();
        preset.rowWidthMultipliers = (float[])rowWidthMultipliers.Clone();

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
        rows = Mathf.Max(1, preset.rows);
        sizeLayerCount = Mathf.Max(1, preset.sizeLayerCount);
        sizeLayerScales = SyncLayerArray(preset.sizeLayerScales, sizeLayerCount, 1f);
        sizeLayerColumnCounts = SyncLayerArray(preset.sizeLayerColumnCounts, sizeLayerCount, spawner.columns);
        rowHeightMultipliers = SyncLayerArray(preset.rowHeightMultipliers, rows, 1f);
        rowWidthMultipliers = SyncLayerArray(preset.rowWidthMultipliers, rows, 1f);
        Repaint();
    }
}
