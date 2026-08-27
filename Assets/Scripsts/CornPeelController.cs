using UnityEngine;

public class CornPeelController : MonoBehaviour
{
    private bool canPeel = false;
    private bool isCompleted = false;

    private int remainingKernels;
    private int totalKernelsAtStart;

    // Sahnede tek olmasi beklenir; Inspector'dan elle atamaya gerek kalmasin diye Start()'ta
    // otomatik bulunur (bkz. FindProgressBar).
    private PeelProgressBar progressBar;

    private Vector2 lastPointerPosition;

    // Tanelerin aras�ndaki k���k bo�luklar� tolere eder.
    // Inspector'dan de�i�tirebilirsin.
    public float peelDetectionRadius = 45f;

    // Bir tanenin onunde, kocan/govde collider'i (CapsuleCollider yaklasik bir sekildir, gercek
    // mesh yuzeyini birebir takip etmez) bu mesafe kadar yakinsa yine de o tane secilebilir sayilir.
    // Boylece govdeye yakin/kismen arkasinda kalan ust taneler raycast'te atlanmaz, ama gercekten
    // govdenin cok arkasinda kalan (buyuk mesafe farkli) taneler yine de secilemez.
    // Yukseltildi: yan acidan gorunen taneler, kabaca yaklasan kapsul collider yuzunden
    // "arkada" sayilip atlanmasin diye (kullanici sadece onden degil, yandan gorunen
    // taneleri de suruklerken dokebilmeli).
    public float kernelOcclusionTolerance = 0.9f;

    // Tanenin "disariya donuk" yuzeyinin kameraya ne kadar donuk olmasi gerektigi (dot carpimi, -1..1).
    // 1 = tam kameraya bakiyor (klasik on yuz), 0 = tam yandan (siluet kenari), negatif = arkaya donuk.
    // Kocan uste dogru incelirken (dar ust satirlar) yaklasik kapsul collider ile gercek yuzey arasindaki
    // fark buyudugunden, sadece mesafe toleransina (kernelOcclusionTolerance) guvenmek ust satirlardaki
    // yandan gorunen taneleri "arkada" sayip elerdi. Bu esik, tanenin GERCEK yonelimine bakarak (kocanin
    // kaba govde seklinden bagimsiz) yandan/siluet kenarindaki taneleri de secilebilir kilar; gercekten
    // kocanin arka tarafina donuk taneler yine de reddedilir.
    [Range(-1f, 1f)]
    public float kernelFacingDotThreshold = -0.35f;

    // =====================================================
    // TANE SESI PITCH ILERLEMESI
    // =====================================================
    // Sayac tek bir yerde (burada) tutulur; her KernelPiece kendi basina ayri bir
    // ilerleme tutmaz. Boylece ayni kesintisiz surukleme sirasinda soyulan taneler
    // (hangi KernelPiece olursa olsun) ortak, tutarli bir pitch dizisi paylasir.

    [Header("Tane Sesi Pitch Ilerlemesi")]
    [Tooltip("Kesintisiz suruklemenin ilk tanesinde kullanilan pitch (tok/normal).")]
    public float kernelSoundBasePitch = 1f;

    [Tooltip("Kesintisiz surukleme boyunca ulasilabilecek en tiz pitch. Cok tizlesmesin diye dusuk tutulur.")]
    public float kernelSoundMaxPitch = 1.35f;

    [Tooltip("Her GERCEKTEN soyulan tanede pitch'in ne kadar artacagi. Kucuk deger = daha uzun/yumusak yukselis.")]
    public float kernelSoundPitchStep = 0.015f;

    // Mevcut kesintisiz surukleme icinde bir sonraki soyulacak tanede kullanilacak pitch.
    // Parmak/mouse birakildiginda veya yeni bir surukleme basladiginda kernelSoundBasePitch'e sifirlanir.
    private float nextKernelSoundPitch;

    // Physics.RaycastNonAlloc icin sinif seviyesinde bir kez olusturulan, tekrar kullanilan tampon;
    // her dokunma/orneklemede yeni dizi/liste/LINQ allocation'i onler.
    private readonly RaycastHit[] raycastBuffer = new RaycastHit[16];

    private void Start()
    {
        nextKernelSoundPitch = kernelSoundBasePitch;
        progressBar = FindObjectOfType<PeelProgressBar>();
    }


    // Yeni bir kesintisiz surukleme baslarken (parmak/mouse asagi) veya
    // bir surukleme birakildiginda cagrilir; pitch dizisini bastan baslatir.
    private void ResetKernelSoundPitchProgression()
    {
        nextKernelSoundPitch = kernelSoundBasePitch;
    }


    // B�t�n yapraklar a��ld�ktan sonra �a�r�l�r.
    public void StartPeeling()
    {
        canPeel = true;

        // includeInactive: false -> pasif KernelTemplate sayilmaz, sadece aktif taneler sayilir.
        remainingKernels =
            GetComponentsInChildren<KernelPiece>(includeInactive: false).Length;
        totalKernelsAtStart = remainingKernels;

        Debug.Log("M�s�r art�k soyulabilir!");
        Debug.Log("Toplam tane say�s�: " + remainingKernels);

        if (progressBar != null)
        {
            progressBar.SetProgress(0f);
        }

        // Misir, iki yaprak da acilip soyulabilir hale gelene kadar donmez;
        // ancak bu noktada kendi kendine donmeye baslar.
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
        // �lk t�klama: nereye bas�l�rsa bas�ls�n (bo� alan dahil) suruklemeye baslar.
        // Parmak/mouse hangi noktadan gecerse gecsin, altindaki tane hemen soyulur.
        if (Input.GetMouseButtonDown(0))
        {
            lastPointerPosition = Input.mousePosition;

            // Yeni bir basis = yeni bir kesintisiz surukleme. Pitch dizisi bastan baslar.
            ResetKernelSoundPitchProgression();

            PeelNearPoint(Input.mousePosition);
        }


        // Mouse bas�l� tutuluyor
        if (Input.GetMouseButton(0))
        {
            Vector2 currentPosition = Input.mousePosition;

            PeelBetweenPoints(
                lastPointerPosition,
                currentPosition
            );

            lastPointerPosition = currentPosition;
        }


        // Mouse b�rak�ld�
        if (Input.GetMouseButtonUp(0))
        {
            // Birakildigi anda pitch ilerlemesi tamamen sifirlanir.
            ResetKernelSoundPitchProgression();
        }
    }


    // =====================================================
    // MOB�L TOUCH
    // =====================================================

    private void HandleTouch()
    {
        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Began)
        {
            lastPointerPosition = touch.position;

            // Yeni bir dokunus = yeni bir kesintisiz surukleme. Pitch dizisi bastan baslar.
            ResetKernelSoundPitchProgression();

            // Ekranin neresine dokunulursa dokunulsun (bos alan dahil) suruklemeye baslar.
            PeelNearPoint(touch.position);
        }


        // Moved VE Stationary (parmak basiliyken kisaca durdugunda) ayni sekilde islenir.
        // Boylece parmak kaldirilmadigi surece dokulme kesintiye ugramaz.
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
            // Parmak kaldirildigi anda pitch ilerlemesi tamamen sifirlanir.
            ResetKernelSoundPitchProgression();
        }
    }


    // =====================================================
    // SADECE KAMERADAN G�R�NEN TANEY� BUL
    // =====================================================

    private KernelPiece GetVisibleKernelAtPoint(
        Vector2 screenPoint
    )
    {
        Ray ray =
            Camera.main.ScreenPointToRay(screenPoint);

        // Allocation-free: sabit tampon + NonAlloc, her ornekleme icin yeni dizi/liste olusturmaz.
        int hitCount =
            Physics.RaycastNonAlloc(ray, raycastBuffer);

        if (hitCount <= 0)
        {
            return null;
        }

        // Tum carpanlar arasinda en yakin mesafeyi (tane olsun olmasin, ornegin kocan govdesi)
        // ve en yakin uygun KernelPiece'i tek gecişte bul; sort/allocation gerekmez.
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

            // Collider'in kendisinde veya parent'inda KernelPiece ara.
            KernelPiece kernel =
                hit.collider.GetComponentInParent<KernelPiece>();

            // Sadece aktif (soyulup dususu bitmemis) taneler secilebilir.
            if (kernel == null || !kernel.gameObject.activeInHierarchy)
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

        // Iki bagimsiz kontrolden EN AZ biri gecerse tane secilebilir sayilir:
        // 1) Mesafe toleransi: kocan/govde collider'inin yaklasik sekli yuzunden
        //    toleransi asan gercek bir engel yoksa (eski davranis).
        // 2) Yon kontrolu: mesafe kontrolu, ust satirlarda kocanin daralmasi yuzunden
        //    yandan gorunen taneleri yanlislikla "arkada" sayabilir; ama tanenin kendi
        //    disariya-donuk yuzu hala kameraya yeterince donukse (siluet kenarina kadar)
        //    yine de secilebilir olmali.
        bool passesDistanceTolerance =
            closestKernelDistance <= closestDistance + kernelOcclusionTolerance;

        bool passesFacingCheck =
            IsKernelFacingCamera(closestKernel);

        if (!passesDistanceTolerance && !passesFacingCheck)
        {
            return null;
        }

        return closestKernel;
    }


    // Tanenin disariya-donuk yuzunun (KernelSpawner'da transform.forward = disari yon olacak
    // sekilde ayarlanir) kameraya ne kadar donuk oldugunu dot carpimiyla olcer. Kocanin kaba
    // govde collider'inden tamamen bagimsizdir; bu yuzden ust satirlarda (dar yaricap) mesafe
    // toleransinin kacirdigi yandan gorunen taneleri de dogru sekilde yakalar.
    private bool IsKernelFacingCamera(KernelPiece kernel)
    {
        if (Camera.main == null)
        {
            return false;
        }

        Vector3 towardCamera =
            Camera.main.transform.position - kernel.transform.position;

        if (towardCamera.sqrMagnitude < 0.0001f)
        {
            return true;
        }

        float facingDot =
            Vector3.Dot(kernel.transform.forward, towardCamera.normalized);

        return facingDot >= kernelFacingDotThreshold;
    }


    // =====================================================
    // PARMA�IN YAKININDAK� TANELER� SOY
    // =====================================================

    private void PeelNearPoint(Vector2 screenPoint)
    {
        // �nce tam alt�ndaki taneyi kontrol et.
        KernelPiece centerKernel =
            GetVisibleKernelAtPoint(screenPoint);

        if (centerKernel != null)
        {
            TryPeelKernelWithProgressivePitch(centerKernel);
        }


        // K���k bir �evreyi de kontrol ediyoruz.
        // Kosegen yonler de eklendi: yan acidan gorunen, ekran uzayinda dar/egik
        // duran taneler sadece dikey/yatay orneklemeyle atlanmasin diye.
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
                TryPeelKernelWithProgressivePitch(kernel);
            }
        }
    }


    // Taneyi, mevcut kesintisiz suruklemenin bir sonraki pitch degeriyle soymayi dener.
    // Pitch sayaci SADECE kernel.Peel(...) gercekten yeni bir taneyi soyduysa (true donduyse)
    // ilerletilir; zaten soyulmus/no-op bir tane icin bosa tuketilmez. Boylece "soyulan her
    // yeni tane" ile pitch artisi bire bir eslesir.
    private void TryPeelKernelWithProgressivePitch(KernelPiece kernel)
    {
        float pitchForThisKernel = nextKernelSoundPitch;

        bool actuallyPeeled = kernel.Peel(pitchForThisKernel);

        if (actuallyPeeled)
        {
            nextKernelSoundPitch =
                Mathf.Min(
                    kernelSoundMaxPitch,
                    nextKernelSoundPitch + kernelSoundPitchStep
                );
        }
    }


    // =====================================================
    // S�R�KLEME YOLUNU TARA
    // =====================================================

    private void PeelBetweenPoints(
        Vector2 start,
        Vector2 end
    )
    {
        float distance =
            Vector2.Distance(start, end);

        int steps = Mathf.Max(
            1,
            Mathf.CeilToInt(distance / 15f)
        );


        for (int i = 0; i <= steps; i++)
        {
            float t = (float)i / steps;

            Vector2 screenPoint =
                Vector2.Lerp(start, end, t);

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
            "Kalan tane: " + remainingKernels
        );

        if (progressBar != null && totalKernelsAtStart > 0)
        {
            float peeledRatio =
                1f - (float)remainingKernels / totalKernelsAtStart;

            progressBar.SetProgress(peeledRatio);
        }

        if (remainingKernels <= 0)
        {
            CompletePeeling();
        }
    }


    private void CompletePeeling()
    {
        isCompleted = true;

        // Yuvarlama hatalarindan bagimsiz, tamamlaninca cubuk kesinlikle tam dolu gorunsun.
        if (progressBar != null)
        {
            progressBar.SetProgress(1f);
        }
        canPeel = false;

        Debug.Log("Tüm mısır taneleri soyuldu!");
    }
}