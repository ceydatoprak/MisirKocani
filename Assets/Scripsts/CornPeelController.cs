using UnityEngine;

public class CornPeelController : MonoBehaviour
{
    private bool canPeel = false;
    private bool isCompleted = false;

    private int remainingKernels;

    private Vector2 lastPointerPosition;

    private CornRotateController rotateController;

    // Tanelerin aras�ndaki k���k bo�luklar� tolere eder.
    // Inspector'dan de�i�tirebilirsin.
    public float peelDetectionRadius = 35f;

    // Bir tanenin onunde, kocan/govde collider'i (CapsuleCollider yaklasik bir sekildir, gercek
    // mesh yuzeyini birebir takip etmez) bu mesafe kadar yakinsa yine de o tane secilebilir sayilir.
    // Boylece govdeye yakin/kismen arkasinda kalan ust taneler raycast'te atlanmaz, ama gercekten
    // govdenin cok arkasinda kalan (buyuk mesafe farkli) taneler yine de secilemez.
    public float kernelOcclusionTolerance = 0.35f;

    // Physics.RaycastNonAlloc icin sinif seviyesinde bir kez olusturulan, tekrar kullanilan tampon;
    // her dokunma/orneklemede yeni dizi/liste/LINQ allocation'i onler.
    private readonly RaycastHit[] raycastBuffer = new RaycastHit[16];

    private enum GestureMode
    {
        None,
        Peeling,
        Rotating
    }

    private GestureMode currentMode = GestureMode.None;


    private void Start()
    {
        rotateController = GetComponent<CornRotateController>();
    }


    // B�t�n yapraklar a��ld�ktan sonra �a�r�l�r.
    public void StartPeeling()
    {
        canPeel = true;

        // includeInactive: false -> pasif KernelTemplate sayilmaz, sadece aktif taneler sayilir.
        remainingKernels =
            GetComponentsInChildren<KernelPiece>(includeInactive: false).Length;

        Debug.Log("M�s�r art�k soyulabilir!");
        Debug.Log("Toplam tane say�s�: " + remainingKernels);
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
        // �lk t�klama
        if (Input.GetMouseButtonDown(0))
        {
            lastPointerPosition = Input.mousePosition;

            // Tam alt�nda veya yak�n�nda tane varsa:
            // Bu hareket SOYMA hareketidir.
            if (IsNearVisibleKernel(Input.mousePosition))
            {
                currentMode = GestureMode.Peeling;

                PeelNearPoint(Input.mousePosition);
            }
            else
            {
                // Ger�ekten bo� bir b�lgedeysek:
                // Bu hareket D�ND�RME hareketidir.
                currentMode = GestureMode.Rotating;
            }
        }


        // Mouse bas�l� tutuluyor
        if (Input.GetMouseButton(0))
        {
            Vector2 currentPosition = Input.mousePosition;

            if (currentMode == GestureMode.Peeling)
            {
                PeelBetweenPoints(
                    lastPointerPosition,
                    currentPosition
                );
            }
            else if (currentMode == GestureMode.Rotating)
            {
                float difference =
                    currentPosition.x - lastPointerPosition.x;

                if (rotateController != null)
                {
                    rotateController.Rotate(difference);
                }
            }

            lastPointerPosition = currentPosition;
        }


        // Mouse b�rak�ld�
        if (Input.GetMouseButtonUp(0))
        {
            currentMode = GestureMode.None;
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

            if (IsNearVisibleKernel(touch.position))
            {
                currentMode = GestureMode.Peeling;

                PeelNearPoint(touch.position);
            }
            else
            {
                currentMode = GestureMode.Rotating;
            }
        }


        if (touch.phase == TouchPhase.Moved)
        {
            Vector2 currentPosition = touch.position;

            if (currentMode == GestureMode.Peeling)
            {
                PeelBetweenPoints(
                    lastPointerPosition,
                    currentPosition
                );
            }
            else if (currentMode == GestureMode.Rotating)
            {
                float difference =
                    currentPosition.x - lastPointerPosition.x;

                if (rotateController != null)
                {
                    rotateController.Rotate(difference);
                }
            }

            lastPointerPosition = currentPosition;
        }


        if (touch.phase == TouchPhase.Ended ||
            touch.phase == TouchPhase.Canceled)
        {
            currentMode = GestureMode.None;
        }
    }


    // =====================================================
    // BU NOKTANIN YAKININDA G�R�NEN TANE VAR MI?
    // =====================================================

    private bool IsNearVisibleKernel(Vector2 screenPoint)
    {
        // �nce tam bast���m�z noktaya bak.
        if (GetVisibleKernelAtPoint(screenPoint) != null)
        {
            return true;
        }

        // Sonra �evresine bak.
        Vector2[] offsets =
        {
            new Vector2(peelDetectionRadius, 0f),
            new Vector2(-peelDetectionRadius, 0f),

            new Vector2(0f, peelDetectionRadius),
            new Vector2(0f, -peelDetectionRadius),

            new Vector2(
                peelDetectionRadius,
                peelDetectionRadius
            ),

            new Vector2(
                -peelDetectionRadius,
                peelDetectionRadius
            ),

            new Vector2(
                peelDetectionRadius,
                -peelDetectionRadius
            ),

            new Vector2(
                -peelDetectionRadius,
                -peelDetectionRadius
            )
        };


        foreach (Vector2 offset in offsets)
        {
            if (GetVisibleKernelAtPoint(
                    screenPoint + offset
                ) != null)
            {
                return true;
            }
        }

        return false;
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

        // Tanenin onunde, kocan/govde collider'inin yaklasik sekli yuzunden
        // toleransi asan gercek bir engel varsa (tane gercekten cok arkada/uzakta kaliyorsa)
        // secilebilir sayma; boylece cok uzaktaki veya govdenin arka yuzundeki taneler
        // yanlislikla secilmez, kocani dondurme hareketi de bozulmaz.
        if (closestKernelDistance > closestDistance + kernelOcclusionTolerance)
        {
            return null;
        }

        return closestKernel;
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
            centerKernel.Peel();
        }


        // K���k bir �evreyi de kontrol ediyoruz.
        float smallRadius =
            peelDetectionRadius * 0.5f;

        Vector2[] offsets =
        {
            new Vector2(smallRadius, 0f),
            new Vector2(-smallRadius, 0f),

            new Vector2(0f, smallRadius),
            new Vector2(0f, -smallRadius)
        };


        foreach (Vector2 offset in offsets)
        {
            KernelPiece kernel =
                GetVisibleKernelAtPoint(
                    screenPoint + offset
                );

            if (kernel != null)
            {
                kernel.Peel();
            }
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
            Mathf.CeilToInt(distance / 8f)
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

        if (remainingKernels <= 0)
        {
            CompletePeeling();
        }
    }


    private void CompletePeeling()
    {
        isCompleted = true;
        canPeel = false;
        currentMode = GestureMode.None;

        Debug.Log("Tüm mısır taneleri soyuldu!");
    }
}