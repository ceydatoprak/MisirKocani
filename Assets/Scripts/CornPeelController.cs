using System.Collections;
using UnityEngine;

public class CornPeelController : MonoBehaviour
{
    private bool canPeel = false;
    private bool isCompleted = false;

    private int remainingKernels;
    private int totalKernelsAtStart;

    [Header("Otomatik Bitirme (Son Taneler)")]
    [Tooltip("Soyulan oran bu esige ulasinca kalan taneler kendiliginden firlayip dusme animasyonuyla soyulur. Boylece oyuncu son birkac zor-erisilen taneyi tek tek aramak zorunda kalmaz.")]
    [Range(0f, 1f)]
    public float autoFinishThreshold = 0.9f;

    [Tooltip("Otomatik bitirmede ardisik tanelerin firlamasi arasindaki minimum gecikme (saniye).")]
    public float autoFinishStaggerMin = 0.015f;

    [Tooltip("Otomatik bitirmede ardisik tanelerin firlamasi arasindaki maksimum gecikme (saniye).")]
    public float autoFinishStaggerMax = 0.05f;

    private bool autoFinishTriggered = false;

    private PeelProgressBar progressBar;
    private Vector2 lastPointerPosition;

    public float peelDetectionRadius = 45f;

    // Gövde collider'ı ile tane arasındaki görünürlük toleransı.
    public float kernelOcclusionTolerance = 0.9f;

    // Tanenin kameraya dönüklüğünü kontrol eden eşik değeri.
    [Range(-1f, 1f)]
    public float kernelFacingDotThreshold = -0.35f;

    [Header("Tane Sesi Pitch Ilerlemesi")]
    [Tooltip("Kesintisiz suruklemenin ilk tanesinde kullanilan pitch (tok/normal).")]
    public float kernelSoundBasePitch = 1f;

    [Tooltip("Kesintisiz surukleme boyunca ulasilabilecek en tiz pitch. Cok tizlesmesin diye dusuk tutulur.")]
    public float kernelSoundMaxPitch = 1.35f;

    [Tooltip("Her GERCEKTEN soyulan tanede pitch'in ne kadar artacagi. Kucuk deger = daha uzun/yumusak yukselis.")]
    public float kernelSoundPitchStep = 0.015f;

    private float nextKernelSoundPitch;

    // Raycast işlemlerinde tekrar kullanılan tampon.
    private readonly RaycastHit[] raycastBuffer = new RaycastHit[16];

    private void Start()
    {
        nextKernelSoundPitch = kernelSoundBasePitch;
        progressBar = FindObjectOfType<PeelProgressBar>();
    }

    private void ResetKernelSoundPitchProgression()
    {
        nextKernelSoundPitch = kernelSoundBasePitch;
    }

    // Yapraklar açıldıktan sonra soyma işlemini başlatır.
    public void StartPeeling()
    {
        canPeel = true;

        remainingKernels =
            GetComponentsInChildren<KernelPiece>(includeInactive: false).Length;

        totalKernelsAtStart = remainingKernels;

        Debug.Log("Mısır artık soyulabilir!");
        Debug.Log("Toplam tane sayısı: " + remainingKernels);

        if (progressBar != null)
        {
            progressBar.SetProgress(0f);
        }

        CornRotateController rotateController =
            GetComponent<CornRotateController>();

        if (rotateController != null)
        {
            rotateController.StartRotating();
        }
    }

    private void Update()
    {
        if (!canPeel || isCompleted)
            return;

        HandleMouse();

        if (Input.touchCount > 0)
        {
            HandleTouch();
        }
    }

    // =====================================================
    // MOUSE
    // =====================================================

    private void HandleMouse()
    {
        if (Input.GetMouseButtonDown(0))
        {
            lastPointerPosition = Input.mousePosition;

            ResetKernelSoundPitchProgression();

            PeelNearPoint(Input.mousePosition);
        }

        if (Input.GetMouseButton(0))
        {
            Vector2 currentPosition = Input.mousePosition;

            PeelBetweenPoints(
                lastPointerPosition,
                currentPosition
            );

            lastPointerPosition = currentPosition;
        }

        if (Input.GetMouseButtonUp(0))
        {
            ResetKernelSoundPitchProgression();
        }
    }

    // =====================================================
    // MOBİL TOUCH
    // =====================================================

    private void HandleTouch()
    {
        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Began)
        {
            lastPointerPosition = touch.position;

            ResetKernelSoundPitchProgression();

            PeelNearPoint(touch.position);
        }

        if (touch.phase == TouchPhase.Moved ||
            touch.phase == TouchPhase.Stationary)
        {
            Vector2 currentPosition = touch.position;

            PeelBetweenPoints(
                lastPointerPosition,
                currentPosition
            );

            lastPointerPosition = currentPosition;
        }

        if (touch.phase == TouchPhase.Ended ||
            touch.phase == TouchPhase.Canceled)
        {
            ResetKernelSoundPitchProgression();
        }
    }

    // =====================================================
    // GÖRÜNEN TANEYİ BUL
    // =====================================================

    private KernelPiece GetVisibleKernelAtPoint(
        Vector2 screenPoint
    )
    {
        Ray ray =
            Camera.main.ScreenPointToRay(screenPoint);

        int hitCount =
            Physics.RaycastNonAlloc(ray, raycastBuffer);

        if (hitCount <= 0)
        {
            return null;
        }

        float closestDistance = float.MaxValue;

        KernelPiece closestKernel = null;

        float closestKernelDistance = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = raycastBuffer[i];

            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
            }

            if (hit.distance >= closestKernelDistance)
            {
                continue;
            }

            KernelPiece kernel =
                hit.collider.GetComponentInParent<KernelPiece>();

            if (kernel == null ||
                !kernel.gameObject.activeInHierarchy)
            {
                continue;
            }

            closestKernel = kernel;
            closestKernelDistance = hit.distance;
        }

        if (closestKernel == null)
        {
            return null;
        }

        bool passesDistanceTolerance =
            closestKernelDistance <=
            closestDistance + kernelOcclusionTolerance;

        bool passesFacingCheck =
            IsKernelFacingCamera(closestKernel);

        if (!passesDistanceTolerance &&
            !passesFacingCheck)
        {
            return null;
        }

        return closestKernel;
    }

    private bool IsKernelFacingCamera(
        KernelPiece kernel
    )
    {
        if (Camera.main == null)
        {
            return false;
        }

        Vector3 towardCamera =
            Camera.main.transform.position -
            kernel.transform.position;

        if (towardCamera.sqrMagnitude < 0.0001f)
        {
            return true;
        }

        float facingDot =
            Vector3.Dot(
                kernel.transform.forward,
                towardCamera.normalized
            );

        return facingDot >= kernelFacingDotThreshold;
    }

    // =====================================================
    // TANELERİ SOY
    // =====================================================

    private void PeelNearPoint(
        Vector2 screenPoint
    )
    {
        KernelPiece centerKernel =
            GetVisibleKernelAtPoint(screenPoint);

        if (centerKernel != null)
        {
            TryPeelKernelWithProgressivePitch(
                centerKernel
            );
        }

        float smallRadius =
            peelDetectionRadius * 0.30f;

        Vector2[] offsets =
        {
            new Vector2(smallRadius, 0f),
            new Vector2(-smallRadius, 0f),

            new Vector2(0f, smallRadius),
            new Vector2(0f, -smallRadius),

            new Vector2(smallRadius, smallRadius),
            new Vector2(-smallRadius, smallRadius),
            new Vector2(smallRadius, -smallRadius),
            new Vector2(-smallRadius, -smallRadius)
        };

        foreach (Vector2 offset in offsets)
        {
            KernelPiece kernel =
                GetVisibleKernelAtPoint(
                    screenPoint + offset
                );

            if (kernel != null)
            {
                TryPeelKernelWithProgressivePitch(
                    kernel
                );
            }
        }
    }

    private void TryPeelKernelWithProgressivePitch(
        KernelPiece kernel
    )
    {
        float pitchForThisKernel =
            nextKernelSoundPitch;

        bool actuallyPeeled =
            kernel.Peel(pitchForThisKernel);

        if (actuallyPeeled)
        {
            nextKernelSoundPitch =
                Mathf.Min(
                    kernelSoundMaxPitch,
                    nextKernelSoundPitch +
                    kernelSoundPitchStep
                );
        }
    }

    // =====================================================
    // SÜRÜKLEME YOLU
    // =====================================================

    private void PeelBetweenPoints(
        Vector2 start,
        Vector2 end
    )
    {
        float distance =
            Vector2.Distance(start, end);

        int steps =
            Mathf.Max(
                1,
                Mathf.CeilToInt(
                    distance / 15f
                )
            );

        for (int i = 0; i <= steps; i++)
        {
            float t =
                (float)i / steps;

            Vector2 screenPoint =
                Vector2.Lerp(
                    start,
                    end,
                    t
                );

            PeelNearPoint(screenPoint);
        }
    }

    // =====================================================
    // TANE SAYACI
    // =====================================================

    public void KernelRemoved()
    {
        remainingKernels--;

        Debug.Log(
            "Kalan tane: " +
            remainingKernels
        );

        float peeledRatio =
            totalKernelsAtStart > 0
                ? 1f -
                  (float)remainingKernels /
                  totalKernelsAtStart
                : 0f;

        if (progressBar != null)
        {
            progressBar.SetProgress(
                peeledRatio
            );
        }

        if (!autoFinishTriggered &&
            totalKernelsAtStart > 0 &&
            peeledRatio >= autoFinishThreshold)
        {
            autoFinishTriggered = true;

            StartCoroutine(
                AutoFinishRemainingKernels()
            );
        }

        if (remainingKernels <= 0)
        {
            CompletePeeling();
        }
    }

    private IEnumerator AutoFinishRemainingKernels()
    {
        KernelPiece[] remaining =
            GetComponentsInChildren<KernelPiece>(
                includeInactive: false
            );

        foreach (KernelPiece kernel in remaining)
        {
            if (kernel == null)
                continue;

            kernel.Peel(
                nextKernelSoundPitch
            );

            yield return new WaitForSeconds(
                Random.Range(
                    autoFinishStaggerMin,
                    autoFinishStaggerMax
                )
            );
        }
    }

    private void CompletePeeling()
    {
        isCompleted = true;

        if (progressBar != null)
        {
            progressBar.SetProgress(1f);
        }

        canPeel = false;

        Debug.Log(
            "Tüm mısır taneleri soyuldu!"
        );
    }
}