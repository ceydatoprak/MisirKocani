using UnityEngine;
public class KernelSpawner : MonoBehaviour
{
    public GameObject kernelPrefab;
    [Header("Kocan Referansi")]
    [Tooltip("Delik/yaricap hesaplarinin dayanacagi soyulmus kocan mesh'i (kocan.002). Bos birakilirsa CornBody altinda ayni isimle otomatik bulunur.")]
    public Renderer cornCobRenderer;
    [Header("Satir / Sutun")]
    [Tooltip("Kocanin uzunlugu boyunca dikey delik satiri sayisi.")]
    public int rows = 12;
    [Tooltip("useDynamicColumnsPerRow acikken: en genis (orta) satirin ulasabilecegi MAKSIMUM sutun sayisi. Kapaliyken: her satirda sabit uretilen sutun sayisi (eski davranis).")]
    public int columns = 12;
    [Header("Dinamik Sutun Sayisi (Kocan Cevresine Gore)")]
    [Tooltip("Acikken her satirin sutun (tane) sayisi, o satirin gercek olculen yuzey yaricapinin en genis satira oranina gore hesaplanir: rowColumnCount = Round(columns * rowRadius / widestRowRadius). Boylece dar ust/alt satirlarda daha az, genis orta satirlarda columns kadar tane uretilir. Kapaliyken her satirda sabit 'columns' adet tane uretilir (eski davranis, fallback).")]
    public bool useDynamicColumnsPerRow = true;
    [Tooltip("useDynamicColumnsPerRow acikken hesaplanan sutun sayisinin inebilecegi en dusuk deger. Cok dar uc satirlarda bile taneler tamamen kaybolmasin/asiri seyrelmesin diye alt sinir olarak kullanilir.")]
    public int minimumColumnsPerRow = 6;
    [Header("Yaricap (Kocan Mesh Sinirina Gore)")]
    [Range(0f, 1.5f)]
    [Tooltip("En alt satirin, kocanin olculen en genis yaricapina gore orani.")]
    public float bottomRadiusFactor = 0.95f;
    [Range(0f, 1.5f)]
    [Tooltip("En ust satirin, kocanin olculen en genis yaricapina gore orani. Kocan yukari dogru daraldigi icin genelde 1'den kucuk olmali.")]
    public float endRadiusFactor = 0.62f;
    [Tooltip("Tanenin kocan yuzeyinden disariya dogru birakilacagi ek bosluk (batma/gomulme onlenir).")]
    public float surfaceOffset = 0.015f;
    [Header("Yuzey Tabanli Yaricap (Mesh Vertexlerinden)")]
    [Tooltip("Acikken her tanenin yaricapi, kocan.002 mesh vertexlerinden o satir yuksekligi ve sutun acisina en yakin noktalar orneklenerek hesaplanir (bombeli/duzensiz yuzeye oturur). Kapaliyken veya mesh okunamazsa bottomRadiusFactor/endRadiusFactor tabanli analitik sisteme dusulur.")]
    public bool useMeshSurfaceRadii = true;
    [Header("Dikey Yerlesim")]
    [Tooltip("Kocanin olculen en alt noktasindan ilk satira kadar birakilacak bosluk.")]
    public float bottomMargin = 0.05f;
    [Tooltip("Kocanin olculen en ust noktasindan son satira kadar birakilacak bosluk.")]
    public float topMargin = 0.08f;
    [Tooltip("Komsu satirlari yarim sutun acisi kadar kaydirarak sasirtmali (capraz) dizilim uygular. Kaydirma her satirin KENDI aci adimina gore yapilir, bu yuzden dinamik sutun sayisi acikken de guvenlidir. Yatay bant (katman) gorunumunu kiran ana ayardir.")]
    public bool staggerAlternateRows = false;
    [Header("Manuel Dikey Sinir (Opsiyonel)")]
    [Tooltip("Acikken satirlarin Y konumlari MeshRenderer.bounds yerine manualBottom/manualTop arasinda olusturulur. Kapaliyken otomatik mesh-bounds sistemi calisir.")]
    public bool useManualVerticalBounds = true;
    [Tooltip("useManualVerticalBounds acikken kullanilan alt Y siniri (KernelContainer/CornBody local uzayinda).")]
    public float manualBottom = -1f;
    [Tooltip("useManualVerticalBounds acikken kullanilan ust Y siniri (KernelContainer/CornBody local uzayinda).")]
    public float manualTop = 1f;
    [Header("Tane Olcek / Donus")]
    [Tooltip("Klonlanan tanenin nihai local olcegi. Delik boyutuna gore elle ayarlanabilir.")]
    public Vector3 kernelScale = new Vector3(0.17f, 0.17f, 0.17f);
    [Tooltip("Her tanenin disariya donuk hizalamasina ek olarak uygulanacak Euler donus duzeltmesi (dolgun yuz disari, ic taraf kocana baksin).")]
    public Vector3 kernelRotationOffset = Vector3.zero;
    [Header("Uclarda Tane Kucultme (Satir Yaricapina Gore)")]
    [Tooltip("Acikken her satirdaki tanelerin boyutu, o satirin gercek yaricapinin kocanin en genis satirina oranina gore uniform kucultulur. Boylece dar ust/alt uclarda taneler birbirinin ustune binmez.")]
    public bool scaleKernelsByRowRadius = true;
    [Tooltip("Satir olcek carpaninin inebilecegi en dusuk deger (0-1). Dinamik sutun sayisi zaten dar cevreyi telafi ettigi icin bu artik hafif bir guvenlik agi olarak yuksek tutulmalidir; taneler tamamen kaybolmasin diye alt sinir olarak kullanilir.")]
    [Range(0.05f, 1f)]
    public float minimumRowScaleFactor = 0.85f;
    [Header("Yedek Degerler (kocan mesh'i bulunamazsa)")]
    public float fallbackRadius = 0.55f;
    public float fallbackBottom = -1f;
    public float fallbackTop = 1f;

    // ============================================================================
    // [YENI] KATMAN KIRICI AYARLAR
    // Hepsinin varsayilan degeri "hicbir sey yapma"dir. Bu dosyayi yapistirdiginda
    // sahne birebir eskisi gibi uretilir. Ayarlari TEK TEK acarak ilerle.
    // ============================================================================
    [Header("[YENI] Katman Kirici - 1) Spiral Burgu")]
    [Tooltip("Her satira eklenen ek donus acisi (derece). Satirlar hafifce spiral yapar, duz dikey oluklar kirilir. 0 = kapali (eski davranis). Onerilen: 3-6.")]
    public float rowTwistDegrees = 0f;

    [Header("[YENI] Katman Kirici - 2) Bosluk Kapatma")]
    [Tooltip("kernelScale UZERINE uygulanan eksen bazli carpan. (1,1,1) = kapali (eski davranis). Y'yi buyutmek satirlar arasi yatay bantlari, X'i buyutmek sutunlar arasi dikey oluklari kapatir. Prefab'in hangi ekseninin yukari/yana baktigina gore deneyerek bul.")]
    public Vector3 kernelScaleMultiplier = Vector3.one;

    [Tooltip("Taneyi yuzeyden ICERI dogru gomer (surfaceOffset'ten bagimsiz ince ayar). 0 = kapali (eski davranis). Kucuk pozitif degerler (0.01-0.04) tane tabanlarini kocana gomup ek yerlerini gizler.")]
    public float extraSinkIntoCob = 0f;

    [Header("[YENI] Katman Kirici - 3) Dogal Dagilim (Jitter)")]
    [Tooltip("Rastgeleligin tohumu. Ayni tohum her zaman ayni dizilimi uretir (tekrarlanabilir).")]
    public int randomSeed = 1337;
    [Tooltip("Satir araliginin yuzdesi kadar dikey kaydirma. 0 = kapali. Onerilen: 0.05-0.10.")]
    [Range(0f, 0.5f)] public float positionJitterY = 0f;
    [Tooltip("Sutun aci adiminin yuzdesi kadar yatay kaydirma. 0 = kapali. Onerilen: 0.05-0.10.")]
    [Range(0f, 0.5f)] public float positionJitterAngle = 0f;
    [Tooltip("Her taneye uygulanan rastgele donus (+/- derece). 0 = kapali. Onerilen: 4-8.")]
    [Range(0f, 25f)] public float rotationJitter = 0f;
    [Tooltip("Her tanenin boyutundaki rastgele sapma (+/- oran). 0 = kapali. Onerilen: 0.05-0.10.")]
    [Range(0f, 0.4f)] public float scaleJitter = 0f;

    [Header("[YENI] Katman Kirici - 4) Yuzey Egimine Hizalama")]
    [Tooltip("Acikken taneler kocanin daralma egimini takip edecek sekilde egilir; uclarda basamakli siluet olusmaz. Kocan egimi 0 oldugu yerlerde sonuc eski davranisla BIREBIR aynidir. Kapali = eski davranis.")]
    public bool alignToSurfaceSlope = false;

    [Header("[YENI] Yukseklige Gore Katmanli Boyut (Elle Kontrol)")]
    [Tooltip("Kocan boyunca esit araliklarla yerlestirilmis kac kontrol noktasi (katman) olacagini belirler. Kocanin gercek olculerinden BAGIMSIZDIR (scaleKernelsByRowRadius'un aksine); kullanicinin elle belirledigi bir boyut egrisidir. 1 = tum taneler ayni boyut (varsayilan).")]
    [Min(1)]
    public int sizeLayerCount = 1;
    [Tooltip("Her kontrol noktasinin kernelScale uzerine ek carpani. Index 0 = en alt (t=0), son index = en ust (t=1). Aralarinda smoothstep ile yumusak gecis yapilir, katman sinirlari gorunmez. Uzunluk sizeLayerCount ile ayni tutulmalidir (Tane Onizleme tool'u bunu otomatik senkronlar).")]
    public float[] sizeLayerScales = new float[] { 1f };
    [Tooltip("Ayni kontrol noktalarinda (sizeLayerScales ile ayni t degerlerinde) hedef sutun/tane sayisi. Aralarinda ayni sekilde yumusak interpolasyon yapilip en yakin tam sayiya yuvarlanir. Boyutun buyudugu katmanlarda sayiyi azaltarak tanelerin birbirine girmesini (ustuste binmesini) onlemek icin kullanilir. Uzunluk sizeLayerCount ile ayni tutulmalidir.")]
    public float[] sizeLayerColumnCounts = new float[] { 14f };

    [Header("[YENI] Satir Bazli Elle Yukseklik (Y Ekseni, Interpolasyonsuz)")]
    [Tooltip("Tanenin SADECE YUKSEKLIGINI (Y ekseni; genislik/derinlik olan X/Z'yi etkilemez) satir satir ayarlar. sizeLayerScales'in aksine, satirlar arasinda YUMUSATMA (smoothstep) yapilmaz: her satirin kendi carpani aynen uygulanir. Index 0 = en alt satir, son index = en ust satir. Uzunluk 'rows' ile ayni tutulmalidir (Tane Onizleme tool'u bunu otomatik senkronlar). Bos birakilirsa veya bir satir icin deger eksikse/0 veya altindaysa o satir icin 1 (degisiklik yok) kullanilir. Diger boyut carpanlarinin UZERINE eklenir.")]
    public float[] rowHeightMultipliers = new float[0];

    [Header("[YENI] Satir Bazli Elle Genislik (X Ekseni, Interpolasyonsuz)")]
    [Tooltip("Tanenin SADECE GENISLIGINI (X ekseni; yukseklik/derinlik olan Y/Z'yi etkilemez) satir satir ayarlar. rowHeightMultipliers ile ayni mantik: satirlar arasinda YUMUSATMA yapilmaz, her satirin kendi carpani aynen uygulanir. Index 0 = en alt satir, son index = en ust satir. Uzunluk 'rows' ile ayni tutulmalidir (Tane Onizleme tool'u bunu otomatik senkronlar). Bos birakilirsa veya bir satir icin deger eksikse/0 veya altindaysa o satir icin 1 (degisiklik yok) kullanilir. Diger boyut carpanlarinin UZERINE eklenir.")]
    public float[] rowWidthMultipliers = new float[0];

    [Header("[YENI] Editor")]
    [Tooltip("Acikken Inspector'da bir deger degistirdiginde sahne otomatik yeniden uretilir. Kapaliyken sag-tik > 'Taneleri Yeniden Uret' ile elle tetiklersin (eski davranis).")]
    public bool autoRebuildOnValidate = false;

    private const string RowPrefix = "Row_";
    private const string CornCobObjectName = "kocan.002";
    private void Start()
    {
        SpawnKernels();
    }
    [ContextMenu("Taneleri Yeniden Uret")]
    private void SpawnKernelsFromEditor()
    {
        SpawnKernels();
    }
#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/Misir Kocani/Secili KernelSpawner Tanelerini Uret")]
    private static void SpawnKernelsForSelection()
    {
        GameObject go = UnityEditor.Selection.activeGameObject;
        if (go == null)
            return;
        KernelSpawner spawner = go.GetComponent<KernelSpawner>();
        if (spawner == null)
            return;
        spawner.SpawnKernels();
    }

    // [YENI] Inspector'da deger degisince otomatik yeniden uretim (opsiyonel).
    // DestroyImmediate'i OnValidate icinde cagirmak yasak oldugu icin delayCall ile ertelenir.
    private void OnValidate()
    {
        if (!autoRebuildOnValidate || Application.isPlaying)
            return;
        UnityEditor.EditorApplication.delayCall -= DelayedRebuild;
        UnityEditor.EditorApplication.delayCall += DelayedRebuild;
    }

    private void DelayedRebuild()
    {
        UnityEditor.EditorApplication.delayCall -= DelayedRebuild;
        if (this == null || kernelPrefab == null || Application.isPlaying)
            return;
        SpawnKernels();
    }
#endif
    public void SpawnKernels()
    {
        if (kernelPrefab == null)
        {
            Debug.LogError("KernelSpawner: kernelPrefab atanmamış, taneler oluşturulamıyor.");
            return;
        }
        if (rows <= 0 || columns <= 0)
        {
            Debug.LogError("KernelSpawner: rows ve columns 0'dan buyuk olmalidir.");
            return;
        }
        ClearPreviousRows();
        GetCobLocalBounds(out float localBottom, out float localTop, out float baseRadiusX, out float baseRadiusZ);
        if (useManualVerticalBounds)
        {
            localBottom = manualBottom;
            localTop = manualTop;
        }
        float usableBottom = localBottom + bottomMargin;
        float usableTop = localTop - topMargin;
        // Tam cevre boyunca esit aci adimi. column/columns kullanildigi icin
        // column = columns-1 sonrasi tekrar 0'a donmez; 0 ve 360 derece ust uste binmez.
        float baseAngleStep = 360f / columns;
        // Yuzey tabanli yaricap icin: kocan.002 vertexlerini bir kere KernelContainer local uzayina tasi.
        bool meshSurfaceAvailable = false;
        Vector3[] localVerts = null;
        if (useMeshSurfaceRadii)
        {
            localVerts = TryBuildLocalVertexCache(out meshSurfaceAvailable);
        }
        // Vertex-orneklemede aci farkini (derece) yaklasik bir yay uzunluguna cevirmek icin kullanilan referans yaricap.
        float approxRadiusForWeighting = (baseRadiusX + baseRadiusZ) * 0.5f;
        if (approxRadiusForWeighting <= 0.0001f)
            approxRadiusForWeighting = 0.5f;
        // Vertex aramasinin baslangic penceresi: yaklasik bir satir yuksekligi / bir sutun acisi.
        float heightWindowStep = rows > 1 ? Mathf.Abs(usableTop - usableBottom) / (rows - 1) : Mathf.Abs(localTop - localBottom) * 0.5f;
        if (heightWindowStep <= 0.0001f)
            heightWindowStep = 0.05f;
        float angleWindowStep = baseAngleStep;
        // [YENI] Jitter hesaplarinda kullanilan gercek satir araligi (pencere degerinden bagimsiz tutuldu).
        float rowSpacing = heightWindowStep;
        // On-gecis: her satirin ortalama gercek yaricapini (o satirin tum sutunlarindaki, ana
        // dongudekiyle ayni sekilde hesaplanan yuzey yaricabinin ortalamasi) ve kocandaki en genis
        // satirin yaricapini bulur. Position/rotation/yaricap formulunu degistirmez; hem satir basina
        // dinamik sutun sayisini (useDynamicColumnsPerRow) hem de opsiyonel satir olcek carpanini
        // (scaleKernelsByRowRadius) belirlemek icin kullanilir.
        float[] rowAverageRadius = new float[rows];
        float widestRowRadius = 0f;
        if (scaleKernelsByRowRadius || useDynamicColumnsPerRow)
        {
            for (int row = 0; row < rows; row++)
            {
                float rt = rows > 1 ? (float)row / (rows - 1) : 0.5f;
                float rtEased = rt * rt * (3f - 2f * rt);
                float rRadiusFactor = Mathf.Lerp(bottomRadiusFactor, endRadiusFactor, rtEased);
                float rRowRadiusX = baseRadiusX * rRadiusFactor + surfaceOffset;
                float rRowRadiusZ = baseRadiusZ * rRadiusFactor + surfaceOffset;
                float rRowFallbackRadius = (baseRadiusX + baseRadiusZ) * 0.5f * rRadiusFactor;
                float rY = Mathf.Lerp(usableBottom, usableTop, rt);
                // [YENI] Burgu acisi on-gecise de eklendi ki ortalama yaricap ana donguyle tutarli kalsin.
                float rRowStagger = (staggerAlternateRows && row % 2 == 1) ? baseAngleStep / 2f : 0f;
                rRowStagger += row * rowTwistDegrees;
                float radiusSum = 0f;
                for (int column = 0; column < columns; column++)
                {
                    float rAngle = baseAngleStep * column + rRowStagger;
                    float columnRadius;
                    if (useMeshSurfaceRadii && meshSurfaceAvailable)
                    {
                        float sampled = SampleSurfaceRadius(
                            localVerts, rY, rAngle, heightWindowStep, angleWindowStep, approxRadiusForWeighting);
                        columnRadius = (!float.IsNaN(sampled) && sampled > 0f) ? sampled : rRowFallbackRadius;
                    }
                    else
                    {
                        columnRadius = (rRowRadiusX + rRowRadiusZ) * 0.5f;
                    }
                    radiusSum += columnRadius;
                }
                rowAverageRadius[row] = radiusSum / columns;
                if (rowAverageRadius[row] > widestRowRadius)
                    widestRowRadius = rowAverageRadius[row];
            }
        }
        // Satir basina gercekte uretilecek sutun (tane) sayisi. useDynamicColumnsPerRow acikken
        // 'columns' artik HER satirda zorunlu sayi degil, en genis satirin ulasabilecegi maksimumdur:
        // dar ust/alt satirlar kendi cevresiyle orantili, daha az tane alir. Kapaliyken (veya en genis
        // yaricap hesaplanamadiysa) eski sabit-sutun davranisina (columns) geri dusulur.
        int[] rowColumnCount = new int[rows];
        for (int row = 0; row < rows; row++)
        {
            if (useDynamicColumnsPerRow && widestRowRadius > 0.0001f)
            {
                int dynamicCount = Mathf.RoundToInt(columns * (rowAverageRadius[row] / widestRowRadius));
                rowColumnCount[row] = Mathf.Clamp(dynamicCount, Mathf.Min(minimumColumnsPerRow, columns), columns);
            }
            else
            {
                rowColumnCount[row] = columns;
            }
            // [YENI] Kullanicinin katman katman belirledigi elle tane sayisi (bkz.
            // GetSizeLayerColumnCount), kocanin gercek olculerinden bagimsizdir. sizeLayerColumnCounts
            // tek elemanliyken (varsayilan) yukaridaki degeri degistirmez.
            float rowT = rows > 1 ? (float)row / (rows - 1) : 0.5f;
            rowColumnCount[row] = GetSizeLayerColumnCount(rowT);
        }
        // [YENI] Jitter'i tekrarlanabilir yap: ayni seed -> ayni dizilim.
        // (On-gecisten SONRA cagrilir ki on-gecis rastgeleligi tuketmesin.)
        Random.InitState(randomSeed);
        int totalKernelsGenerated = 0;
        System.Text.StringBuilder rowSummary = new System.Text.StringBuilder();
        for (int row = 0; row < rows; row++)
        {
            GameObject rowParent = new GameObject(RowPrefix + row.ToString("D2"));
            rowParent.transform.SetParent(transform, false);
            // t: 0 = en alt satir, 1 = en ust satir.
            float t = rows > 1 ? (float)row / (rows - 1) : 0.5f;
            float tEased = t * t * (3f - 2f * t); // smoothstep: govde duz, tepeye dogru daralma hizlanir.
            float radiusFactor = Mathf.Lerp(bottomRadiusFactor, endRadiusFactor, tEased);
            // Mevcut analitik (elips) yaricaplar - useMeshSurfaceRadii kapaliyken veya mesh bulunamadiginda fallback olarak aynen kullanilir.
            float rowRadiusX = baseRadiusX * radiusFactor + surfaceOffset;
            float rowRadiusZ = baseRadiusZ * radiusFactor + surfaceOffset;
            // Yuzey tabanli sistemde vertex bulunamazsa dusulecek satir-bazli guvenlik yaricapi (surfaceOffset haric).
            float rowFallbackRadius = (baseRadiusX + baseRadiusZ) * 0.5f * radiusFactor;
            float y = Mathf.Lerp(usableBottom, usableTop, t);
            // Bu satirin gercek sutun sayisi ve buna gore 360 dereceye esit dagitilan aci adimi.
            // Ayni aciya iki tane binmesin diye adim = 360 / sutunSayisi ve dongu 0..sutunSayisi-1.
            int rowColumns = rowColumnCount[row];
            float rowAngleStep = rowColumns > 0 ? 360f / rowColumns : baseAngleStep;
            // Komsu satirlari yarim tane acisi kadar kaydirarak sik/dogal bir dizilim olustur.
            // Kaydirma o satirin KENDI aci adimina gore yapilir.
            float rowStagger = (staggerAlternateRows && row % 2 == 1) ? rowAngleStep / 2f : 0f;
            // [YENI] Spiral burgu: her satir kendinden oncekine gore rowTwistDegrees kadar doner.
            rowStagger += row * rowTwistDegrees;
            // Bu satirin genislik oranina gore uniform kucultme carpani (X/Y/Z ayni oranda).
            // Yatay genisletme veya bosluk kapatma yapilmaz; sadece kernelScale'e gore kucultur,
            // hicbir zaman kernelScale'i asmaz (ust sinir 1) ve tamamen kaybolmaz (alt sinir minimumRowScaleFactor).
            // Dinamik sutun sayisi zaten dar cevreyi telafi ettigi icin bu artik hafif bir ek onlemdir.
            float rowScaleMultiplier = 1f;
            if (scaleKernelsByRowRadius && widestRowRadius > 0.0001f)
            {
                float radiusRatio = rowAverageRadius[row] / widestRowRadius;
                rowScaleMultiplier = Mathf.Clamp(Mathf.Sqrt(radiusRatio), minimumRowScaleFactor, 1f);
            }
            // [YENI] Kocanin gercek olculerinden bagimsiz, kullanicinin elle belirledigi
            // asagidan-yukariya katman carpani (bkz. GetSizeLayerMultiplier).
            rowScaleMultiplier *= GetSizeLayerMultiplier(t);
            // [YENI] Satir bazli elle YUKSEKLIK (Y ekseni) carpani: X/Z'yi etkilemez, sadece
            // tanenin dikey (Y) boyutunu satir satir ayarlar (bkz. asagidaki localScale atamasi).
            // Kaydirma acisi sadece Y ekseni etrafinda oldugundan (Quaternion.Euler(0, angle, 0)),
            // yerel Y ekseni her zaman dunya Y'sine (yukari/asagi) karsilik gelir.
            float rowHeightMultiplier = GetRowHeightMultiplier(row);
            // [YENI] Satir bazli elle GENISLIK (X ekseni) carpani: Y/Z'yi etkilemez.
            float rowWidthMultiplier = GetRowWidthMultiplier(row);
            for (int column = 0; column < rowColumns; column++)
            {
                // [YENI] Jitter degerleri. Tum jitter alanlari 0 iken bu ifadeler 0 uretir
                // ve asagidaki hesaplar eski davranisla birebir ayni kalir.
                float yJitter = Random.Range(-1f, 1f) * rowSpacing * positionJitterY;
                float angleJitter = Random.Range(-1f, 1f) * rowAngleStep * positionJitterAngle;
                float rotJitterX = Random.Range(-1f, 1f) * rotationJitter;
                float rotJitterY = Random.Range(-1f, 1f) * rotationJitter;
                float rotJitterZ = Random.Range(-1f, 1f) * rotationJitter;
                float scaleJitterFactor = 1f + Random.Range(-1f, 1f) * scaleJitter;

                float angle = rowAngleStep * column + rowStagger + angleJitter;
                float angleRad = angle * Mathf.Deg2Rad;
                float kernelY = y + yJitter;
                float x, z;
                // [YENI] Yuzey egimi (dr/dy). alignToSurfaceSlope kapaliyken 0 kalir -> eski rotasyon.
                float slope = 0f;
                if (useMeshSurfaceRadii && meshSurfaceAvailable)
                {
                    float sampledRadius = SampleSurfaceRadius(
                        localVerts, kernelY, angle, heightWindowStep, angleWindowStep, approxRadiusForWeighting);
                    float surfaceRadius = (!float.IsNaN(sampledRadius) && sampledRadius > 0f)
                        ? sampledRadius
                        : rowFallbackRadius;
                    float finalRadius = surfaceRadius + surfaceOffset - extraSinkIntoCob;
                    x = Mathf.Sin(angleRad) * finalRadius;
                    z = Mathf.Cos(angleRad) * finalRadius;

                    if (alignToSurfaceSlope)
                    {
                        // Ayni acida, bir satir araligi asagi/yukari orneklenen yaricaplardan egim turevi.
                        float h = Mathf.Max(rowSpacing * 0.5f, 0.0001f);
                        float rUp = SampleSurfaceRadius(
                            localVerts, kernelY + h, angle, heightWindowStep, angleWindowStep, approxRadiusForWeighting);
                        float rDown = SampleSurfaceRadius(
                            localVerts, kernelY - h, angle, heightWindowStep, angleWindowStep, approxRadiusForWeighting);
                        if (!float.IsNaN(rUp) && !float.IsNaN(rDown))
                            slope = (rUp - rDown) / (2f * h);
                    }
                }
                else
                {
                    x = Mathf.Sin(angleRad) * (rowRadiusX - extraSinkIntoCob);
                    z = Mathf.Cos(angleRad) * (rowRadiusZ - extraSinkIntoCob);
                }
                Vector3 localPosition = new Vector3(x, kernelY, z);
                GameObject newKernel = Instantiate(kernelPrefab, rowParent.transform);
                newKernel.transform.localPosition = localPosition;

                // [YENI] Taban rotasyon. slope == 0 iken LookRotation(disariDogru, up) ifadesi
                // Quaternion.Euler(0, angle, 0) ile BIREBIR aynidir -> eski davranis korunur.
                Quaternion baseRotation;
                if (alignToSurfaceSlope && !Mathf.Approximately(slope, 0f))
                {
                    Vector3 outward = new Vector3(Mathf.Sin(angleRad), 0f, Mathf.Cos(angleRad));
                    Vector2 n2 = new Vector2(1f, -slope).normalized;   // yuzey normali (radyal, dikey)
                    Vector2 u2 = new Vector2(slope, 1f).normalized;    // yuzey tegeti (yukari yon)
                    Vector3 forwardDir = (outward * n2.x + Vector3.up * n2.y).normalized;
                    Vector3 upDir = (outward * u2.x + Vector3.up * u2.y).normalized;
                    baseRotation = Quaternion.LookRotation(forwardDir, upDir);
                }
                else
                {
                    baseRotation = Quaternion.Euler(0f, angle, 0f);
                }
                newKernel.transform.localRotation =
                    baseRotation
                    * Quaternion.Euler(rotJitterX, rotJitterY, rotJitterZ)
                    * Quaternion.Euler(kernelRotationOffset);

                // [YENI] kernelScaleMultiplier (1,1,1) ve scaleJitter 0 iken sonuc eski satirla ayni:
                // kernelScale * rowScaleMultiplier
                Vector3 finalScale =
                    Vector3.Scale(kernelScale, kernelScaleMultiplier) * rowScaleMultiplier * scaleJitterFactor;

                // [YENI] Satir bazli yukseklik/genislik carpanlari SADECE kendi eksenlerine uygulanir
                // (Y <-> X birbirini etkilemez); ikisi de 1 iken sonuc yukaridaki finalScale ile birebir ayni.
                finalScale.y *= rowHeightMultiplier;
                finalScale.x *= rowWidthMultiplier;

                newKernel.transform.localScale = finalScale;

                // Sablon (kernelPrefab) pasifse klonlar da pasif dogar; aktif hale getir.
                newKernel.SetActive(true);
                newKernel.name =
                    "Kernel_R" + row.ToString("D2") + "_C" + column.ToString("D2");
            }
            totalKernelsGenerated += rowColumns;
            if (row > 0)
                rowSummary.Append(", ");
            rowSummary.Append(RowPrefix + row.ToString("D2") + "=" + rowColumns);
        }
        Debug.Log("KernelSpawner satir basina tane sayisi: " + rowSummary.ToString());
        Debug.Log("Toplam tane sayısı: " + totalKernelsGenerated);
    }
    // kocan.002 (veya atanmis cornCobRenderer) mesh'inin gorunen MeshRenderer.bounds'unu esas alir.
    // FBX'in kendi pivotuna guvenmez: dunya-uzayindaki 8 kose noktasini KernelContainer/CornBody
    // local uzayina InverseTransformPoint ile tasiyip, oradan yeni bir eksene-hizali kutu cikarir.
    private void GetCobLocalBounds(out float localBottom, out float localTop, out float radiusX, out float radiusZ)
    {
        Renderer cob = ResolveCornCobRenderer();
        if (cob == null)
        {
            Debug.LogWarning("KernelSpawner: '" + CornCobObjectName + "' mesh'i bulunamadi, yedek (fallback) olculerle uretiliyor.");
            localBottom = fallbackBottom;
            localTop = fallbackTop;
            radiusX = fallbackRadius;
            radiusZ = fallbackRadius;
            return;
        }
        Bounds worldBounds = cob.bounds;
        Vector3 c = worldBounds.center;
        Vector3 e = worldBounds.extents;
        Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = new Vector3(
                c.x + ((i & 1) == 0 ? -e.x : e.x),
                c.y + ((i & 2) == 0 ? -e.y : e.y),
                c.z + ((i & 4) == 0 ? -e.z : e.z));
            Vector3 local = transform.InverseTransformPoint(corner);
            min = Vector3.Min(min, local);
            max = Vector3.Max(max, local);
        }
        localBottom = min.y;
        localTop = max.y;
        radiusX = (max.x - min.x) * 0.5f;
        radiusZ = (max.z - min.z) * 0.5f;
    }
    // kocan.002 mesh vertexlerini bir kere okuyup KernelContainer/CornBody local uzayina tasir.
    // MeshFilter.sharedMesh.vertices -> meshTransform.TransformPoint (mesh local -> world)
    // -> this.transform.InverseTransformPoint (world -> KernelContainer local).
    // Mesh "Read/Write Enabled" degilse (isReadable == false) guvenli sekilde null doner; NaN/crash olusturmaz.
    private Vector3[] TryBuildLocalVertexCache(out bool success)
    {
        success = false;
        Renderer cob = ResolveCornCobRenderer();
        if (cob == null)
            return null;
        MeshFilter meshFilter = cob.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
            return null;
        Mesh mesh = meshFilter.sharedMesh;
        if (!mesh.isReadable)
        {
            Debug.LogWarning("KernelSpawner: '" + CornCobObjectName + "' mesh'i okunabilir (Read/Write Enabled) degil, yuzey tabanli yaricap hesaplanamiyor. Analitik (bottomRadiusFactor/endRadiusFactor) sisteme dusuluyor.");
            return null;
        }
        Vector3[] meshVertices = mesh.vertices;
        if (meshVertices == null || meshVertices.Length == 0)
            return null;
        Transform meshTransform = meshFilter.transform;
        Vector3[] localVerts = new Vector3[meshVertices.Length];
        for (int i = 0; i < meshVertices.Length; i++)
        {
            Vector3 world = meshTransform.TransformPoint(meshVertices[i]);
            localVerts[i] = transform.InverseTransformPoint(world);
        }
        success = true;
        return localVerts;
    }
    // Belirli bir satir yuksekligi (targetY) ve sutun acisina (targetAngleDeg, derece) en yakin mesh vertexlerinden
    // agirlikli ortalama ile gercek yuzey yaricapini (eksene uzaklik) hesaplar. Yakinda yeterli vertex yoksa
    // pencereyi kademeli buyuterek komsu satir/acilardan enterpolasyon yapar. Hicbir vertex bulunamazsa NaN doner
    // (cagiran taraf bunu satir bazli bir guvenlik yaricapina dusurur, boylece tane atlamaz/gomulmez).
    private static float SampleSurfaceRadius(
        Vector3[] localVerts, float targetY, float targetAngleDeg,
        float heightWindowStep, float angleWindowStep, float approxRadiusForWeighting)
    {
        if (localVerts == null || localVerts.Length == 0)
            return float.NaN;
        float heightWindow = Mathf.Max(heightWindowStep, 0.0001f);
        float angleWindow = Mathf.Max(angleWindowStep, 0.5f);
        for (int expand = 0; expand < 6; expand++)
        {
            double weightedRadiusSum = 0.0;
            double weightSum = 0.0;
            int count = 0;
            for (int i = 0; i < localVerts.Length; i++)
            {
                Vector3 v = localVerts[i];
                float vertRadius = Mathf.Sqrt(v.x * v.x + v.z * v.z);
                // Eksene cok yakin (uc/kapak) noktalar yaricap hesabini bozmasin.
                if (vertRadius < 0.0001f)
                    continue;
                float heightDist = v.y - targetY;
                if (Mathf.Abs(heightDist) > heightWindow)
                    continue;
                float vertAngle = Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg;
                float angleDelta = Mathf.Abs(Mathf.DeltaAngle(vertAngle, targetAngleDeg));
                if (angleDelta > angleWindow)
                    continue;
                float arcDist = angleDelta * Mathf.Deg2Rad * approxRadiusForWeighting;
                float distSq = heightDist * heightDist + arcDist * arcDist;
                double weight = 1.0 / (distSq + 0.0001);
                weightedRadiusSum += weight * vertRadius;
                weightSum += weight;
                count++;
            }
            bool lastAttempt = expand == 5;
            if ((count >= 3 || lastAttempt) && weightSum > 0.0)
            {
                return (float)(weightedRadiusSum / weightSum);
            }
            // Yeterince yakin vertex bulunamadi: komsu satir/acilari da kapsayacak sekilde pencereyi genislet.
            heightWindow *= 1.8f;
            angleWindow *= 1.8f;
        }
        return float.NaN;
    }
    private Renderer ResolveCornCobRenderer()
    {
        if (cornCobRenderer != null)
            return cornCobRenderer;
        Transform found = FindChildRecursive(transform.root, CornCobObjectName);
        if (found != null)
        {
            cornCobRenderer = found.GetComponent<Renderer>();
        }
        return cornCobRenderer;
    }
    private static Transform FindChildRecursive(Transform parent, string targetName)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == targetName)
                return child;
            Transform result = FindChildRecursive(child, targetName);
            if (result != null)
                return result;
        }
        return null;
    }
    // t: 0 = en alt satir, 1 = en ust satir. values, yukseklik boyunca esit araliklarla
    // yerlestirilmis kontrol noktalaridir (index 0 -> t=0, son index -> t=1). Iki komsu
    // kontrol noktasi arasinda smoothstep ile YUMUSAK gecis yapilir; boylece katman
    // sinirlari gorunmez, sonuc tek parca/surekli bir egri gibi gorunur. sizeLayerScales
    // (boyut) ve sizeLayerColumnCounts (tane sayisi) ayni mekanizmayi paylasir.
    private static float GetInterpolatedLayerValue(float[] values, float t, float fallback)
    {
        if (values == null || values.Length == 0)
            return fallback;
        if (values.Length == 1)
            return values[0];

        float scaledT = Mathf.Clamp01(t) * (values.Length - 1);
        int lowerIndex = Mathf.Clamp(Mathf.FloorToInt(scaledT), 0, values.Length - 2);
        int upperIndex = lowerIndex + 1;
        float localT = scaledT - lowerIndex;
        float smoothLocalT = localT * localT * (3f - 2f * localT);
        return Mathf.Lerp(values[lowerIndex], values[upperIndex], smoothLocalT);
    }
    private float GetSizeLayerMultiplier(float t)
    {
        return GetInterpolatedLayerValue(sizeLayerScales, t, 1f);
    }
    // Katmanlardan (GetSizeLayerMultiplier) farkli olarak interpolasyon YAPMAZ: her satirin
    // kendi elle girilen degeri aynen kullanilir. Sadece Y eksenine (yukseklik) uygulanir,
    // X/Z'ye dokunmaz. Dizi kisa/bos ya da deger 0 veya altindaysa (henuz elle ayarlanmamis
    // satirlar icin) 1 (degisiklik yok) doner.
    private float GetRowHeightMultiplier(int row)
    {
        if (rowHeightMultipliers == null || row < 0 || row >= rowHeightMultipliers.Length)
            return 1f;
        float value = rowHeightMultipliers[row];
        return value > 0f ? value : 1f;
    }
    // GetRowHeightMultiplier ile ayni mantik, ama X eksenine (genislik) uygulanir.
    private float GetRowWidthMultiplier(int row)
    {
        if (rowWidthMultipliers == null || row < 0 || row >= rowWidthMultipliers.Length)
            return 1f;
        float value = rowWidthMultipliers[row];
        return value > 0f ? value : 1f;
    }
    // Boyutun buyudugu katmanlarda sutun sayisini azaltarak tanelerin ayni cevre
    // uzerinde birbirine girmesini (ustuste binmesini) onlemek icin kullanilir.
    // 3..40 araligina sabitlenir; global 'columns' alanindan BAGIMSIZDIR, kullanici
    // isterse katman bazinda columns'tan daha fazla/az tane belirleyebilir.
    private int GetSizeLayerColumnCount(float t)
    {
        float interpolated = GetInterpolatedLayerValue(sizeLayerColumnCounts, t, columns);
        return Mathf.Clamp(Mathf.RoundToInt(interpolated), 3, 40);
    }
    private void ClearPreviousRows()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (!child.name.StartsWith(RowPrefix))
                continue;
            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }
}