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

    [Tooltip("En genis satirin ulasabilecegi maksimum sutun sayisi.")]
    public int columns = 12;


    [Header("Dinamik Sutun Sayisi")]
    [Tooltip("Acikken her satirin tane sayisi, o satirin yuzey yaricapina gore hesaplanir.")]
    public bool useDynamicColumnsPerRow = true;

    [Tooltip("Dinamik sutun sayisinin inebilecegi en dusuk deger.")]
    public int minimumColumnsPerRow = 6;


    [Header("Yaricap")]
    [Range(0f, 1.5f)]
    [Tooltip("En alt satirin, kocanin olculen en genis yaricapina gore orani.")]
    public float bottomRadiusFactor = 0.95f;

    [Range(0f, 1.5f)]
    [Tooltip("En ust satirin, kocanin olculen en genis yaricapina gore orani.")]
    public float endRadiusFactor = 0.62f;

    [Tooltip("Tanenin kocan yuzeyinden disariya dogru birakilacagi ek bosluk.")]
    public float surfaceOffset = 0.015f;


    [Header("Yuzey Tabanli Yaricap")]
    [Tooltip("Acikken yaricap, kocan mesh vertexlerinden hesaplanir.")]
    public bool useMeshSurfaceRadii = true;


    [Header("Dikey Yerlesim")]
    [Tooltip("Kocanin en alt noktasindan ilk satira kadar birakilacak bosluk.")]
    public float bottomMargin = 0.05f;

    [Tooltip("Kocanin en ust noktasindan son satira kadar birakilacak bosluk.")]
    public float topMargin = 0.08f;

    [Tooltip("Komsu satirlari yarim sutun acisi kadar kaydirir.")]
    public bool staggerAlternateRows = false;


    [Header("Manuel Dikey Sinir")]
    [Tooltip("Acikken satirlar manualBottom ve manualTop arasinda olusturulur.")]
    public bool useManualVerticalBounds = true;

    public float manualBottom = -1f;
    public float manualTop = 1f;


    [Header("Tane Olcek / Donus")]
    [Tooltip("Klonlanan tanenin nihai local olcegi.")]
    public Vector3 kernelScale =
        new Vector3(0.17f, 0.17f, 0.17f);

    [Tooltip("Tanenin disariya donuk hizalamasina eklenecek Euler donus duzeltmesi.")]
    public Vector3 kernelRotationOffset =
        Vector3.zero;


    [Header("Uclarda Tane Kucultme")]
    [Tooltip("Dar satirlarda tanelerin boyutunu yaricap oranina gore kucultur.")]
    public bool scaleKernelsByRowRadius = true;

    [Range(0.05f, 1f)]
    public float minimumRowScaleFactor = 0.85f;


    [Header("Yedek Degerler")]
    public float fallbackRadius = 0.55f;
    public float fallbackBottom = -1f;
    public float fallbackTop = 1f;


    [Header("Katman Kirici - Spiral Burgu")]
    [Tooltip("Her satira eklenen ek donus acisi.")]
    public float rowTwistDegrees = 0f;


    [Header("Katman Kirici - Bosluk Kapatma")]
    [Tooltip("Tane olcegine eksen bazli ek carpan uygular.")]
    public Vector3 kernelScaleMultiplier =
        Vector3.one;

    [Tooltip("Taneyi yuzeyden iceri dogru gomer.")]
    public float extraSinkIntoCob = 0f;


    [Header("Katman Kirici - Dogal Dagilim")]
    [Tooltip("Rastgeleligin tohumu.")]
    public int randomSeed = 1337;

    [Range(0f, 0.5f)]
    public float positionJitterY = 0f;

    [Range(0f, 0.5f)]
    public float positionJitterAngle = 0f;

    [Range(0f, 25f)]
    public float rotationJitter = 0f;

    [Range(0f, 0.4f)]
    public float scaleJitter = 0f;


    [Header("Yuzey Egimine Hizalama")]
    [Tooltip("Taneleri kocanin yuzey egimine gore hizalar.")]
    public bool alignToSurfaceSlope = false;


    [Header("Yukseklige Gore Katmanli Boyut")]
    [Min(1)]
    public int sizeLayerCount = 1;

    public float[] sizeLayerScales =
        new float[] { 1f };

    public float[] sizeLayerColumnCounts =
        new float[] { 14f };


    [Header("Satir Bazli Yukseklik")]
    public float[] rowHeightMultipliers =
        new float[0];


    [Header("Satir Bazli Genislik")]
    public float[] rowWidthMultipliers =
        new float[0];


    [Header("Editor")]
    [Tooltip("Acikken Inspector'da deger degisince sahne otomatik yeniden uretilir.")]
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

    [UnityEditor.MenuItem(
        "Tools/Misir Kocani/Secili KernelSpawner Tanelerini Uret"
    )]
    private static void SpawnKernelsForSelection()
    {
        GameObject go =
            UnityEditor.Selection.activeGameObject;

        if (go == null)
            return;

        KernelSpawner spawner =
            go.GetComponent<KernelSpawner>();

        if (spawner == null)
            return;

        spawner.SpawnKernels();
    }


    private void OnValidate()
    {
        if (!autoRebuildOnValidate ||
            Application.isPlaying)
        {
            return;
        }

        UnityEditor.EditorApplication.delayCall -=
            DelayedRebuild;

        UnityEditor.EditorApplication.delayCall +=
            DelayedRebuild;
    }


    private void DelayedRebuild()
    {
        UnityEditor.EditorApplication.delayCall -=
            DelayedRebuild;

        if (this == null ||
            kernelPrefab == null ||
            Application.isPlaying)
        {
            return;
        }

        SpawnKernels();
    }

#endif


    public void SpawnKernels()
    {
        if (kernelPrefab == null)
        {
            Debug.LogError(
                "KernelSpawner: kernelPrefab atanmamış, taneler oluşturulamıyor."
            );

            return;
        }

        if (rows <= 0 ||
            columns <= 0)
        {
            Debug.LogError(
                "KernelSpawner: rows ve columns 0'dan buyuk olmalidir."
            );

            return;
        }

        ClearPreviousRows();

        GetCobLocalBounds(
            out float localBottom,
            out float localTop,
            out float baseRadiusX,
            out float baseRadiusZ
        );

        if (useManualVerticalBounds)
        {
            localBottom = manualBottom;
            localTop = manualTop;
        }

        float usableBottom =
            localBottom + bottomMargin;

        float usableTop =
            localTop - topMargin;

        float baseAngleStep =
            360f / columns;


        bool meshSurfaceAvailable = false;
        Vector3[] localVerts = null;

        if (useMeshSurfaceRadii)
        {
            localVerts =
                TryBuildLocalVertexCache(
                    out meshSurfaceAvailable
                );
        }


        float approxRadiusForWeighting =
            (baseRadiusX + baseRadiusZ) * 0.5f;

        if (approxRadiusForWeighting <= 0.0001f)
        {
            approxRadiusForWeighting = 0.5f;
        }


        float heightWindowStep =
            rows > 1
                ? Mathf.Abs(
                    usableTop - usableBottom
                ) / (rows - 1)
                : Mathf.Abs(
                    localTop - localBottom
                ) * 0.5f;

        if (heightWindowStep <= 0.0001f)
        {
            heightWindowStep = 0.05f;
        }

        float angleWindowStep =
            baseAngleStep;

        float rowSpacing =
            heightWindowStep;


        float[] rowAverageRadius =
            new float[rows];

        float widestRowRadius = 0f;


        if (scaleKernelsByRowRadius ||
            useDynamicColumnsPerRow)
        {
            for (int row = 0; row < rows; row++)
            {
                float rt =
                    rows > 1
                        ? (float)row / (rows - 1)
                        : 0.5f;

                float rtEased =
                    rt * rt * (3f - 2f * rt);

                float rRadiusFactor =
                    Mathf.Lerp(
                        bottomRadiusFactor,
                        endRadiusFactor,
                        rtEased
                    );

                float rRowRadiusX =
                    baseRadiusX *
                    rRadiusFactor +
                    surfaceOffset;

                float rRowRadiusZ =
                    baseRadiusZ *
                    rRadiusFactor +
                    surfaceOffset;

                float rRowFallbackRadius =
                    (baseRadiusX + baseRadiusZ) *
                    0.5f *
                    rRadiusFactor;

                float rY =
                    Mathf.Lerp(
                        usableBottom,
                        usableTop,
                        rt
                    );

                float rRowStagger =
                    staggerAlternateRows &&
                    row % 2 == 1
                        ? baseAngleStep / 2f
                        : 0f;

                rRowStagger +=
                    row * rowTwistDegrees;

                float radiusSum = 0f;


                for (
                    int column = 0;
                    column < columns;
                    column++
                )
                {
                    float rAngle =
                        baseAngleStep *
                        column +
                        rRowStagger;

                    float columnRadius;

                    if (useMeshSurfaceRadii &&
                        meshSurfaceAvailable)
                    {
                        float sampled =
                            SampleSurfaceRadius(
                                localVerts,
                                rY,
                                rAngle,
                                heightWindowStep,
                                angleWindowStep,
                                approxRadiusForWeighting
                            );

                        columnRadius =
                            !float.IsNaN(sampled) &&
                            sampled > 0f
                                ? sampled
                                : rRowFallbackRadius;
                    }
                    else
                    {
                        columnRadius =
                            (rRowRadiusX +
                             rRowRadiusZ) *
                            0.5f;
                    }

                    radiusSum +=
                        columnRadius;
                }

                rowAverageRadius[row] =
                    radiusSum / columns;

                if (rowAverageRadius[row] >
                    widestRowRadius)
                {
                    widestRowRadius =
                        rowAverageRadius[row];
                }
            }
        }


        int[] rowColumnCount =
            new int[rows];

        for (int row = 0; row < rows; row++)
        {
            if (useDynamicColumnsPerRow &&
                widestRowRadius > 0.0001f)
            {
                int dynamicCount =
                    Mathf.RoundToInt(
                        columns *
                        (
                            rowAverageRadius[row] /
                            widestRowRadius
                        )
                    );

                rowColumnCount[row] =
                    Mathf.Clamp(
                        dynamicCount,
                        Mathf.Min(
                            minimumColumnsPerRow,
                            columns
                        ),
                        columns
                    );
            }
            else
            {
                rowColumnCount[row] =
                    columns;
            }

            float rowT =
                rows > 1
                    ? (float)row / (rows - 1)
                    : 0.5f;

            rowColumnCount[row] =
                GetSizeLayerColumnCount(
                    rowT
                );
        }


        Random.InitState(
            randomSeed
        );

        int totalKernelsGenerated = 0;

        System.Text.StringBuilder rowSummary =
            new System.Text.StringBuilder();


        for (int row = 0; row < rows; row++)
        {
            GameObject rowParent =
                new GameObject(
                    RowPrefix +
                    row.ToString("D2")
                );

            rowParent.transform.SetParent(
                transform,
                false
            );


            float t =
                rows > 1
                    ? (float)row / (rows - 1)
                    : 0.5f;

            float tEased =
                t * t * (3f - 2f * t);

            float radiusFactor =
                Mathf.Lerp(
                    bottomRadiusFactor,
                    endRadiusFactor,
                    tEased
                );

            float rowRadiusX =
                baseRadiusX *
                radiusFactor +
                surfaceOffset;

            float rowRadiusZ =
                baseRadiusZ *
                radiusFactor +
                surfaceOffset;

            float rowFallbackRadius =
                (baseRadiusX + baseRadiusZ) *
                0.5f *
                radiusFactor;

            float y =
                Mathf.Lerp(
                    usableBottom,
                    usableTop,
                    t
                );


            int rowColumns =
                rowColumnCount[row];

            float rowAngleStep =
                rowColumns > 0
                    ? 360f / rowColumns
                    : baseAngleStep;

            float rowStagger =
                staggerAlternateRows &&
                row % 2 == 1
                    ? rowAngleStep / 2f
                    : 0f;

            rowStagger +=
                row * rowTwistDegrees;


            float rowScaleMultiplier =
                1f;

            if (scaleKernelsByRowRadius &&
                widestRowRadius > 0.0001f)
            {
                float radiusRatio =
                    rowAverageRadius[row] /
                    widestRowRadius;

                rowScaleMultiplier =
                    Mathf.Clamp(
                        Mathf.Sqrt(radiusRatio),
                        minimumRowScaleFactor,
                        1f
                    );
            }

            rowScaleMultiplier *=
                GetSizeLayerMultiplier(t);


            float rowHeightMultiplier =
                GetRowHeightMultiplier(row);

            float rowWidthMultiplier =
                GetRowWidthMultiplier(row);


            for (
                int column = 0;
                column < rowColumns;
                column++
            )
            {
                float yJitter =
                    Random.Range(-1f, 1f) *
                    rowSpacing *
                    positionJitterY;

                float angleJitter =
                    Random.Range(-1f, 1f) *
                    rowAngleStep *
                    positionJitterAngle;

                float rotJitterX =
                    Random.Range(-1f, 1f) *
                    rotationJitter;

                float rotJitterY =
                    Random.Range(-1f, 1f) *
                    rotationJitter;

                float rotJitterZ =
                    Random.Range(-1f, 1f) *
                    rotationJitter;

                float scaleJitterFactor =
                    1f +
                    Random.Range(-1f, 1f) *
                    scaleJitter;


                float angle =
                    rowAngleStep *
                    column +
                    rowStagger +
                    angleJitter;

                float angleRad =
                    angle *
                    Mathf.Deg2Rad;

                float kernelY =
                    y + yJitter;

                float x;
                float z;

                float slope = 0f;


                if (useMeshSurfaceRadii &&
                    meshSurfaceAvailable)
                {
                    float sampledRadius =
                        SampleSurfaceRadius(
                            localVerts,
                            kernelY,
                            angle,
                            heightWindowStep,
                            angleWindowStep,
                            approxRadiusForWeighting
                        );

                    float surfaceRadius =
                        !float.IsNaN(sampledRadius) &&
                        sampledRadius > 0f
                            ? sampledRadius
                            : rowFallbackRadius;

                    float finalRadius =
                        surfaceRadius +
                        surfaceOffset -
                        extraSinkIntoCob;

                    x =
                        Mathf.Sin(angleRad) *
                        finalRadius;

                    z =
                        Mathf.Cos(angleRad) *
                        finalRadius;


                    if (alignToSurfaceSlope)
                    {
                        float h =
                            Mathf.Max(
                                rowSpacing * 0.5f,
                                0.0001f
                            );

                        float rUp =
                            SampleSurfaceRadius(
                                localVerts,
                                kernelY + h,
                                angle,
                                heightWindowStep,
                                angleWindowStep,
                                approxRadiusForWeighting
                            );

                        float rDown =
                            SampleSurfaceRadius(
                                localVerts,
                                kernelY - h,
                                angle,
                                heightWindowStep,
                                angleWindowStep,
                                approxRadiusForWeighting
                            );

                        if (!float.IsNaN(rUp) &&
                            !float.IsNaN(rDown))
                        {
                            slope =
                                (rUp - rDown) /
                                (2f * h);
                        }
                    }
                }
                else
                {
                    x =
                        Mathf.Sin(angleRad) *
                        (
                            rowRadiusX -
                            extraSinkIntoCob
                        );

                    z =
                        Mathf.Cos(angleRad) *
                        (
                            rowRadiusZ -
                            extraSinkIntoCob
                        );
                }


                Vector3 localPosition =
                    new Vector3(
                        x,
                        kernelY,
                        z
                    );


                GameObject newKernel =
                    Instantiate(
                        kernelPrefab,
                        rowParent.transform
                    );

                newKernel.transform.localPosition =
                    localPosition;


                Quaternion baseRotation;

                if (alignToSurfaceSlope &&
                    !Mathf.Approximately(
                        slope,
                        0f
                    ))
                {
                    Vector3 outward =
                        new Vector3(
                            Mathf.Sin(angleRad),
                            0f,
                            Mathf.Cos(angleRad)
                        );

                    Vector2 n2 =
                        new Vector2(
                            1f,
                            -slope
                        ).normalized;

                    Vector2 u2 =
                        new Vector2(
                            slope,
                            1f
                        ).normalized;

                    Vector3 forwardDir =
                        (
                            outward * n2.x +
                            Vector3.up * n2.y
                        ).normalized;

                    Vector3 upDir =
                        (
                            outward * u2.x +
                            Vector3.up * u2.y
                        ).normalized;

                    baseRotation =
                        Quaternion.LookRotation(
                            forwardDir,
                            upDir
                        );
                }
                else
                {
                    baseRotation =
                        Quaternion.Euler(
                            0f,
                            angle,
                            0f
                        );
                }


                newKernel.transform.localRotation =
                    baseRotation
                    *
                    Quaternion.Euler(
                        rotJitterX,
                        rotJitterY,
                        rotJitterZ
                    )
                    *
                    Quaternion.Euler(
                        kernelRotationOffset
                    );


                Vector3 finalScale =
                    Vector3.Scale(
                        kernelScale,
                        kernelScaleMultiplier
                    )
                    *
                    rowScaleMultiplier
                    *
                    scaleJitterFactor;

                finalScale.y *=
                    rowHeightMultiplier;

                finalScale.x *=
                    rowWidthMultiplier;

                newKernel.transform.localScale =
                    finalScale;

                newKernel.SetActive(true);

                newKernel.name =
                    "Kernel_R" +
                    row.ToString("D2") +
                    "_C" +
                    column.ToString("D2");
            }


            totalKernelsGenerated +=
                rowColumns;

            if (row > 0)
            {
                rowSummary.Append(", ");
            }

            rowSummary.Append(
                RowPrefix +
                row.ToString("D2") +
                "=" +
                rowColumns
            );
        }


        Debug.Log(
            "KernelSpawner satir basina tane sayisi: " +
            rowSummary
        );

        Debug.Log(
            "Toplam tane sayısı: " +
            totalKernelsGenerated
        );
    }


    private void GetCobLocalBounds(
        out float localBottom,
        out float localTop,
        out float radiusX,
        out float radiusZ
    )
    {
        Renderer cob =
            ResolveCornCobRenderer();

        if (cob == null)
        {
            Debug.LogWarning(
                "KernelSpawner: '" +
                CornCobObjectName +
                "' mesh'i bulunamadi, yedek olculerle uretiliyor."
            );

            localBottom =
                fallbackBottom;

            localTop =
                fallbackTop;

            radiusX =
                fallbackRadius;

            radiusZ =
                fallbackRadius;

            return;
        }


        Bounds worldBounds =
            cob.bounds;

        Vector3 c =
            worldBounds.center;

        Vector3 e =
            worldBounds.extents;

        Vector3 min =
            new Vector3(
                float.MaxValue,
                float.MaxValue,
                float.MaxValue
            );

        Vector3 max =
            new Vector3(
                float.MinValue,
                float.MinValue,
                float.MinValue
            );


        for (int i = 0; i < 8; i++)
        {
            Vector3 corner =
                new Vector3(
                    c.x +
                    (
                        (i & 1) == 0
                            ? -e.x
                            : e.x
                    ),
                    c.y +
                    (
                        (i & 2) == 0
                            ? -e.y
                            : e.y
                    ),
                    c.z +
                    (
                        (i & 4) == 0
                            ? -e.z
                            : e.z
                    )
                );

            Vector3 local =
                transform.InverseTransformPoint(
                    corner
                );

            min =
                Vector3.Min(
                    min,
                    local
                );

            max =
                Vector3.Max(
                    max,
                    local
                );
        }


        localBottom =
            min.y;

        localTop =
            max.y;

        radiusX =
            (max.x - min.x) *
            0.5f;

        radiusZ =
            (max.z - min.z) *
            0.5f;
    }


    private Vector3[] TryBuildLocalVertexCache(
        out bool success
    )
    {
        success = false;

        Renderer cob =
            ResolveCornCobRenderer();

        if (cob == null)
            return null;


        MeshFilter meshFilter =
            cob.GetComponent<MeshFilter>();

        if (meshFilter == null ||
            meshFilter.sharedMesh == null)
        {
            return null;
        }


        Mesh mesh =
            meshFilter.sharedMesh;

        if (!mesh.isReadable)
        {
            Debug.LogWarning(
                "KernelSpawner: '" +
                CornCobObjectName +
                "' mesh'i okunabilir degil. Analitik sisteme geciliyor."
            );

            return null;
        }


        Vector3[] meshVertices =
            mesh.vertices;

        if (meshVertices == null ||
            meshVertices.Length == 0)
        {
            return null;
        }


        Transform meshTransform =
            meshFilter.transform;

        Vector3[] localVerts =
            new Vector3[
                meshVertices.Length
            ];


        for (
            int i = 0;
            i < meshVertices.Length;
            i++
        )
        {
            Vector3 world =
                meshTransform.TransformPoint(
                    meshVertices[i]
                );

            localVerts[i] =
                transform.InverseTransformPoint(
                    world
                );
        }


        success = true;

        return localVerts;
    }


    private static float SampleSurfaceRadius(
        Vector3[] localVerts,
        float targetY,
        float targetAngleDeg,
        float heightWindowStep,
        float angleWindowStep,
        float approxRadiusForWeighting
    )
    {
        if (localVerts == null ||
            localVerts.Length == 0)
        {
            return float.NaN;
        }


        float heightWindow =
            Mathf.Max(
                heightWindowStep,
                0.0001f
            );

        float angleWindow =
            Mathf.Max(
                angleWindowStep,
                0.5f
            );


        for (int expand = 0; expand < 6; expand++)
        {
            double weightedRadiusSum =
                0.0;

            double weightSum =
                0.0;

            int count = 0;


            for (
                int i = 0;
                i < localVerts.Length;
                i++
            )
            {
                Vector3 v =
                    localVerts[i];

                float vertRadius =
                    Mathf.Sqrt(
                        v.x * v.x +
                        v.z * v.z
                    );

                if (vertRadius < 0.0001f)
                {
                    continue;
                }


                float heightDist =
                    v.y - targetY;

                if (Mathf.Abs(heightDist) >
                    heightWindow)
                {
                    continue;
                }


                float vertAngle =
                    Mathf.Atan2(
                        v.x,
                        v.z
                    ) *
                    Mathf.Rad2Deg;

                float angleDelta =
                    Mathf.Abs(
                        Mathf.DeltaAngle(
                            vertAngle,
                            targetAngleDeg
                        )
                    );

                if (angleDelta >
                    angleWindow)
                {
                    continue;
                }


                float arcDist =
                    angleDelta *
                    Mathf.Deg2Rad *
                    approxRadiusForWeighting;

                float distSq =
                    heightDist *
                    heightDist +
                    arcDist *
                    arcDist;

                double weight =
                    1.0 /
                    (
                        distSq +
                        0.0001
                    );

                weightedRadiusSum +=
                    weight *
                    vertRadius;

                weightSum +=
                    weight;

                count++;
            }


            bool lastAttempt =
                expand == 5;

            if (
                (count >= 3 ||
                 lastAttempt)
                &&
                weightSum > 0.0
            )
            {
                return (float)(
                    weightedRadiusSum /
                    weightSum
                );
            }


            heightWindow *= 1.8f;
            angleWindow *= 1.8f;
        }


        return float.NaN;
    }


    private Renderer ResolveCornCobRenderer()
    {
        if (cornCobRenderer != null)
        {
            return cornCobRenderer;
        }


        Transform found =
            FindChildRecursive(
                transform.root,
                CornCobObjectName
            );

        if (found != null)
        {
            cornCobRenderer =
                found.GetComponent<Renderer>();
        }


        return cornCobRenderer;
    }


    private static Transform FindChildRecursive(
        Transform parent,
        string targetName
    )
    {
        for (
            int i = 0;
            i < parent.childCount;
            i++
        )
        {
            Transform child =
                parent.GetChild(i);

            if (child.name ==
                targetName)
            {
                return child;
            }


            Transform result =
                FindChildRecursive(
                    child,
                    targetName
                );

            if (result != null)
            {
                return result;
            }
        }


        return null;
    }


    private static float GetInterpolatedLayerValue(
        float[] values,
        float t,
        float fallback
    )
    {
        if (values == null ||
            values.Length == 0)
        {
            return fallback;
        }

        if (values.Length == 1)
        {
            return values[0];
        }


        float scaledT =
            Mathf.Clamp01(t) *
            (values.Length - 1);

        int lowerIndex =
            Mathf.Clamp(
                Mathf.FloorToInt(
                    scaledT
                ),
                0,
                values.Length - 2
            );

        int upperIndex =
            lowerIndex + 1;

        float localT =
            scaledT -
            lowerIndex;

        float smoothLocalT =
            localT *
            localT *
            (3f - 2f * localT);


        return Mathf.Lerp(
            values[lowerIndex],
            values[upperIndex],
            smoothLocalT
        );
    }


    private float GetSizeLayerMultiplier(
        float t
    )
    {
        return GetInterpolatedLayerValue(
            sizeLayerScales,
            t,
            1f
        );
    }


    private float GetRowHeightMultiplier(
        int row
    )
    {
        if (rowHeightMultipliers == null ||
            row < 0 ||
            row >= rowHeightMultipliers.Length)
        {
            return 1f;
        }


        float value =
            rowHeightMultipliers[row];

        return value > 0f
            ? value
            : 1f;
    }


    private float GetRowWidthMultiplier(
        int row
    )
    {
        if (rowWidthMultipliers == null ||
            row < 0 ||
            row >= rowWidthMultipliers.Length)
        {
            return 1f;
        }


        float value =
            rowWidthMultipliers[row];

        return value > 0f
            ? value
            : 1f;
    }


    private int GetSizeLayerColumnCount(
        float t
    )
    {
        float interpolated =
            GetInterpolatedLayerValue(
                sizeLayerColumnCounts,
                t,
                columns
            );

        return Mathf.Clamp(
            Mathf.RoundToInt(
                interpolated
            ),
            3,
            40
        );
    }


    private void ClearPreviousRows()
    {
        for (
            int i =
                transform.childCount - 1;
            i >= 0;
            i--
        )
        {
            Transform child =
                transform.GetChild(i);

            if (!child.name.StartsWith(
                RowPrefix
            ))
            {
                continue;
            }


            if (Application.isPlaying)
            {
                Destroy(
                    child.gameObject
                );
            }
            else
            {
                DestroyImmediate(
                    child.gameObject
                );
            }
        }
    }
}