using UnityEngine;
using System.Collections;

public class LeafController : MonoBehaviour
{
    [Header("Animasyon Ayarlari")]
    [Tooltip("Dokunma sonrasi acilma animasyonunun suresi.")]
    [SerializeField] private float snapAnimationDuration = 0.9f;


    [Header("Acik Pozisyon / Rotasyon")]
    [Tooltip("Pivot'un acik haldeki local pozisyon farki.")]
    [SerializeField] private Vector3 openLocalPositionOffset = Vector3.zero;

    [Tooltip("Pivot'un acik haldeki local rotasyon farki.")]
    [SerializeField]
    private Vector3 openLocalEulerAnglesOffset =
        new Vector3(0f, 0f, 80f);


    [Header("Yeni Gorsel Takibi")]
    [Tooltip("AnimatedLeaf_Right veya AnimatedLeaf_Left kok nesnesi.")]
    [SerializeField] private Transform replacementVisual;

    [Tooltip("Animasyon klibi atanmazsa kullanilan acilma acisi.")]
    [SerializeField] private float replacementOpenAngle = 75f;

    [Tooltip("Fallback pivot sisteminin mentese collider'i.")]
    [SerializeField] private Collider replacementHingeCollider;

    [Tooltip("Yaprak acilma animasyonu.")]
    [SerializeField] private AnimationClip replacementPeelClip;

    [Tooltip("Animasyon klibinin uygulanacagi iskelet kok nesnesi.")]
    [SerializeField] private Transform replacementSkeletonTarget;


    [Header("Ses Ayarlari")]
    [Tooltip("Yaprak acilma sesini calacak AudioSource.")]
    [SerializeField] private AudioSource leafAudioSource;


    [Header("Yaprak Dusme Ayarlari")]
    [Tooltip("Yapragin dusmeye basladiktan sonra sahnede kalacagi sure.")]
    [SerializeField] private float leafPhysicsLifetime = 2.4f;

    [Tooltip("Yapragin kaybolmadan once kuculme suresi.")]
    [SerializeField] private float leafShrinkDuration = 0.4f;

    [Tooltip("Yapragin asagi dogru baslangic hizi.")]
    [SerializeField] private float leafDownwardVelocity = 0.12f;

    [Tooltip("Yapragin kocandan disari dogru hareket hizi.")]
    [SerializeField] private float leafOutwardVelocity = 0.15f;

    [Tooltip("Duserken yapragin maksimum acisal hizi.")]
    [SerializeField] private float leafTorqueAmount = 1.2f;

    [Tooltip("Yapraga runtime sirasinda eklenecek Rigidbody kutlesi.")]
    [SerializeField] private float leafRigidbodyMass = 0.25f;

    [Tooltip("Yapragin hava direnci.")]
    [SerializeField] private float leafRigidbodyDrag = 2.6f;

    [Header("Suzulme")]
    [Tooltip("Yapragin sag-sola salinma kuvveti.")]
    [SerializeField] private float leafSwayForce = 0.35f;

    [Tooltip("Yanal salinimin frekansi.")]
    [SerializeField] private float leafSwayFrequency = 1.4f;

    [Tooltip("Yapragin donus direnci.")]
    [SerializeField] private float leafRigidbodyAngularDrag = 1.2f;


    private Transform pivot;
    private Collider leafCollider;

    private Vector3 closedLocalPosition;
    private Quaternion closedLocalRotation;

    private Vector3 openLocalPosition;
    private Quaternion openLocalRotation;

    private bool hasReplacementVisual;
    private bool isReplacementFalling = false;

    private Vector3 replacementVisualInitialWorldPosition;
    private Quaternion replacementVisualInitialWorldRotation;

    private Vector3 replacementVisualInitialLocalPosition;
    private Quaternion replacementVisualInitialLocalRotation;
    private Vector3 replacementVisualInitialLocalScale;

    private Transform SkeletonSampleTarget =>
        replacementSkeletonTarget != null
            ? replacementSkeletonTarget
            : replacementVisual;

    private Vector3 skeletonTargetInitialLocalPosition;
    private Quaternion skeletonTargetInitialLocalRotation;
    private Vector3 skeletonTargetInitialLocalScale;

    private Transform replacementVisualCornBody;
    private Vector3 replacementVisualLocalHingeAxis;
    private Vector3 replacementVisualLocalHingePoint;

    private bool isAnimating = false;
    private bool isOpen = false;

    private bool hasPlayedOpenSound = false;

    private float currentProgress = 0f;

    private Coroutine snapRoutine;

    // Play moduna geciste olusabilecek ilk sahte tiklamayi engeller.
    private const float InputIgnoreDuration = 0.2f;
    private float inputReadyTime;

    // Animasyonun tek karede atlanmasini engeller.
    private const float MaxSnapStepSeconds = 0.05f;

    public bool IsOpen => isOpen;

    private static bool allLeavesOpenedMessagePrinted = false;


    private void Awake()
    {
        pivot =
            transform.parent != null
                ? transform.parent
                : transform;

        leafCollider =
            GetComponent<Collider>();

        inputReadyTime =
            Time.unscaledTime +
            InputIgnoreDuration;

        if (leafAudioSource == null)
        {
            leafAudioSource =
                GetComponent<AudioSource>();
        }

        if (leafAudioSource != null)
        {
            leafAudioSource.playOnAwake = false;
            leafAudioSource.loop = false;
            leafAudioSource.spatialBlend = 0f;
        }

        closedLocalPosition =
            pivot.localPosition;

        closedLocalRotation =
            pivot.localRotation;

        openLocalPosition =
            closedLocalPosition +
            openLocalPositionOffset;

        openLocalRotation =
            closedLocalRotation *
            Quaternion.Euler(
                openLocalEulerAnglesOffset
            );

        InitializeReplacementVisual();
    }


    private void InitializeReplacementVisual()
    {
        hasReplacementVisual =
            replacementVisual != null;

        if (!hasReplacementVisual)
            return;

        replacementVisualInitialWorldPosition =
            replacementVisual.position;

        replacementVisualInitialWorldRotation =
            replacementVisual.rotation;

        replacementVisualInitialLocalPosition =
            replacementVisual.localPosition;

        replacementVisualInitialLocalRotation =
            replacementVisual.localRotation;

        replacementVisualInitialLocalScale =
            replacementVisual.localScale;

        Transform skeletonTarget =
            SkeletonSampleTarget;

        if (skeletonTarget != null)
        {
            skeletonTargetInitialLocalPosition =
                skeletonTarget.localPosition;

            skeletonTargetInitialLocalRotation =
                skeletonTarget.localRotation;

            skeletonTargetInitialLocalScale =
                skeletonTarget.localScale;
        }

        Animator replacementAnimator =
            replacementVisual.GetComponent<Animator>();

        if (replacementAnimator != null)
        {
            replacementAnimator.enabled = false;
        }

        replacementVisualCornBody =
            replacementVisual.parent;

        Vector3 hingeAxisWorld =
            Vector3.up;

        Vector3 hingeWorldPoint =
            replacementVisualInitialWorldPosition;

        if (replacementVisualCornBody != null)
        {
            SkinnedMeshRenderer skin =
                replacementVisual
                    .GetComponentInChildren<SkinnedMeshRenderer>();

            if (skin != null)
            {
                Vector3 cornUp =
                    replacementVisualCornBody.up;

                Vector3 toBoundsCenter =
                    skin.bounds.center -
                    replacementVisualCornBody.position;

                Vector3 radialOutward =
                    Vector3.ProjectOnPlane(
                        toBoundsCenter,
                        cornUp
                    );

                if (radialOutward.sqrMagnitude > 0.0001f)
                {
                    radialOutward.Normalize();

                    hingeAxisWorld =
                        Vector3.Cross(
                            cornUp,
                            radialOutward
                        );
                }
            }

            if (replacementHingeCollider != null)
            {
                hingeWorldPoint =
                    GetColliderTopWorldPoint(
                        replacementHingeCollider,
                        replacementVisualCornBody.up
                    );
            }

            replacementVisualLocalHingeAxis =
                replacementVisualCornBody
                    .InverseTransformDirection(
                        hingeAxisWorld
                    );

            replacementVisualLocalHingePoint =
                replacementVisualCornBody
                    .InverseTransformPoint(
                        hingeWorldPoint
                    );
        }
        else
        {
            replacementVisualLocalHingeAxis =
                hingeAxisWorld;

            replacementVisualLocalHingePoint =
                hingeWorldPoint;
        }
    }


    private static Vector3 GetColliderTopWorldPoint(
        Collider collider,
        Vector3 up
    )
    {
        CapsuleCollider capsule =
            collider as CapsuleCollider;

        if (capsule != null)
        {
            Transform capsuleTransform =
                capsule.transform;

            Vector3 localAxis =
                capsule.direction == 0
                    ? Vector3.right
                    : capsule.direction == 2
                        ? Vector3.forward
                        : Vector3.up;

            float halfLength =
                Mathf.Max(
                    capsule.height * 0.5f,
                    capsule.radius
                );

            Vector3 worldCenter =
                capsuleTransform.TransformPoint(
                    capsule.center
                );

            Vector3 worldEnd =
                capsuleTransform.TransformPoint(
                    capsule.center +
                    localAxis * halfLength
                );

            float verticalExtent =
                Mathf.Abs(
                    Vector3.Dot(
                        worldEnd - worldCenter,
                        up
                    )
                );

            return worldCenter +
                   up * verticalExtent;
        }

        Bounds bounds =
            collider.bounds;

        return bounds.center +
               up * bounds.extents.y;
    }


    private void UpdateReplacementVisual(float progress)
    {
        if (!hasReplacementVisual ||
            isReplacementFalling)
        {
            return;
        }

        progress =
            Mathf.Clamp01(progress);

        if (replacementPeelClip != null)
        {
            Transform skeletonTarget =
                SkeletonSampleTarget;

            float animationTime =
                progress *
                replacementPeelClip.length;

            replacementPeelClip.SampleAnimation(
                skeletonTarget.gameObject,
                animationTime
            );

            // Klip yalnizca iskelet animasyonunu uygulasin.
            skeletonTarget.localPosition =
                skeletonTargetInitialLocalPosition;

            skeletonTarget.localRotation =
                skeletonTargetInitialLocalRotation;

            skeletonTarget.localScale =
                skeletonTargetInitialLocalScale;

            return;
        }

        Vector3 hingeAxisWorld;
        Vector3 hingeWorldPoint;

        if (replacementVisualCornBody != null)
        {
            hingeAxisWorld =
                replacementVisualCornBody
                    .TransformDirection(
                        replacementVisualLocalHingeAxis
                    );

            hingeWorldPoint =
                replacementVisualCornBody
                    .TransformPoint(
                        replacementVisualLocalHingePoint
                    );
        }
        else
        {
            hingeAxisWorld =
                replacementVisualLocalHingeAxis;

            hingeWorldPoint =
                replacementVisualLocalHingePoint;
        }

        float angle =
            progress *
            replacementOpenAngle;

        Quaternion openRotation =
            Quaternion.AngleAxis(
                angle,
                hingeAxisWorld
            );

        replacementVisual.position =
            hingeWorldPoint +
            openRotation *
            (
                replacementVisualInitialWorldPosition -
                hingeWorldPoint
            );

        replacementVisual.rotation =
            openRotation *
            replacementVisualInitialWorldRotation;
    }


    private void Update()
    {
        if (isOpen || isAnimating)
            return;

        if (Time.unscaledTime < inputReadyTime)
            return;

        if (Input.touchCount > 0)
        {
            HandleTouch();
        }
        else
        {
            HandleMouse();
        }
    }


    private void HandleMouse()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TryOpen(
                Input.mousePosition
            );
        }
    }


    private void HandleTouch()
    {
        Touch touch =
            Input.GetTouch(0);

        if (touch.phase == TouchPhase.Began)
        {
            TryOpen(
                touch.position
            );
        }
    }


    private void TryOpen(Vector2 screenPosition)
    {
        if (!IsPointerOnThisLeaf(screenPosition))
            return;

        PlayLeafOpenSound();

        SnapTo(
            1f,
            true
        );
    }


    private void PlayLeafOpenSound()
    {
        if (hasPlayedOpenSound)
            return;

        if (leafAudioSource == null)
        {
            leafAudioSource =
                GetComponent<AudioSource>();
        }

        if (leafAudioSource == null)
        {
            Debug.LogWarning(
                gameObject.name +
                ": Yaprak sesi calinamadi. AudioSource bulunamadi."
            );

            return;
        }

        if (leafAudioSource.clip == null)
        {
            Debug.LogWarning(
                gameObject.name +
                ": Yaprak sesi calinamadi. Audio Clip bos."
            );

            return;
        }

        hasPlayedOpenSound = true;

        leafAudioSource.spatialBlend = 0f;

        leafAudioSource.Stop();

        leafAudioSource.PlayOneShot(
            leafAudioSource.clip
        );
    }


    private bool IsPointerOnThisLeaf(
        Vector2 screenPosition
    )
    {
        if (Camera.main == null ||
            leafCollider == null)
        {
            return false;
        }

        Ray ray =
            Camera.main.ScreenPointToRay(
                screenPosition
            );

        return leafCollider.Raycast(
            ray,
            out _,
            Mathf.Infinity
        );
    }


    private void ApplyProgress(float progress)
    {
        float smoothProgress =
            Mathf.SmoothStep(
                0f,
                1f,
                progress
            );

        pivot.localPosition =
            Vector3.Lerp(
                closedLocalPosition,
                openLocalPosition,
                smoothProgress
            );

        pivot.localRotation =
            Quaternion.Slerp(
                closedLocalRotation,
                openLocalRotation,
                smoothProgress
            );

        UpdateReplacementVisual(
            smoothProgress
        );
    }


    private void SnapTo(
        float targetProgress,
        bool markOpenOnComplete
    )
    {
        if (snapRoutine != null)
        {
            StopCoroutine(
                snapRoutine
            );
        }

        snapRoutine =
            StartCoroutine(
                SnapRoutine(
                    targetProgress,
                    markOpenOnComplete
                )
            );
    }


    private IEnumerator SnapRoutine(
        float targetProgress,
        bool markOpenOnComplete
    )
    {
        isAnimating = true;

        float startProgress =
            currentProgress;

        float elapsed = 0f;

        while (elapsed < snapAnimationDuration)
        {
            elapsed +=
                Mathf.Min(
                    Time.deltaTime,
                    MaxSnapStepSeconds
                );

            float t =
                snapAnimationDuration > 0f
                    ? elapsed /
                      snapAnimationDuration
                    : 1f;

            currentProgress =
                Mathf.Lerp(
                    startProgress,
                    targetProgress,
                    t
                );

            ApplyProgress(
                currentProgress
            );

            yield return null;
        }

        currentProgress =
            targetProgress;

        ApplyProgress(
            currentProgress
        );

        isAnimating = false;

        if (markOpenOnComplete)
        {
            OnLeafFullyOpened();
        }
    }


    private void OnLeafFullyOpened()
    {
        if (isOpen)
            return;

        isOpen = true;

        Debug.Log(
            gameObject.name +
            " yapragi tamamen acildi!"
        );

        CornController cornController =
            FindObjectOfType<CornController>();

        if (cornController != null)
        {
            cornController.LeafRemoved();
        }

        CheckAllLeavesOpened();

        StartReplacementFall();
    }


    private void StartReplacementFall()
    {
        if (!hasReplacementVisual ||
            replacementVisual == null ||
            isReplacementFalling)
        {
            return;
        }

        isReplacementFalling = true;

        Transform cornBody =
            replacementVisualCornBody;

        Vector3 outwardDirection =
            replacementVisual.position -
            (
                cornBody != null
                    ? cornBody.position
                    : transform.position
            );

        Vector3 upDirection =
            cornBody != null
                ? cornBody.up
                : Vector3.up;

        outwardDirection =
            Vector3.ProjectOnPlane(
                outwardDirection,
                upDirection
            ).normalized;

        if (outwardDirection.sqrMagnitude < 0.001f)
        {
            outwardDirection =
                replacementVisual.forward;
        }

        replacementVisual.SetParent(
            null,
            true
        );

        Rigidbody leafRigidbody =
            replacementVisual
                .GetComponent<Rigidbody>();

        if (leafRigidbody == null)
        {
            leafRigidbody =
                replacementVisual
                    .gameObject
                    .AddComponent<Rigidbody>();
        }

        leafRigidbody.mass =
            leafRigidbodyMass;

        leafRigidbody.drag =
            leafRigidbodyDrag;

        leafRigidbody.angularDrag =
            leafRigidbodyAngularDrag;

        leafRigidbody.useGravity =
            true;

        leafRigidbody.isKinematic =
            false;

        leafRigidbody.interpolation =
            RigidbodyInterpolation.Interpolate;

        leafRigidbody.velocity =
            outwardDirection *
            leafOutwardVelocity +
            Vector3.down *
            leafDownwardVelocity;

        leafRigidbody.angularVelocity =
            Random.insideUnitSphere *
            leafTorqueAmount;

        Vector3 swayAxis =
            Vector3.Cross(
                upDirection,
                outwardDirection
            ).normalized;

        StartCoroutine(
            HideFallenLeaf(
                leafRigidbody,
                swayAxis
            )
        );
    }


    private IEnumerator HideFallenLeaf(
        Rigidbody leafRigidbody,
        Vector3 swayAxis
    )
    {
        float elapsed = 0f;

        while (elapsed < leafPhysicsLifetime)
        {
            elapsed +=
                Time.deltaTime;

            if (leafRigidbody != null)
            {
                float swayForce =
                    Mathf.Sin(
                        elapsed *
                        leafSwayFrequency *
                        Mathf.PI *
                        2f
                    ) *
                    leafSwayForce;

                leafRigidbody.AddForce(
                    swayAxis *
                    swayForce,
                    ForceMode.Force
                );
            }

            yield return null;
        }

        if (replacementVisual == null)
            yield break;

        Vector3 startingScale =
            replacementVisual.localScale;

        elapsed = 0f;

        while (elapsed < leafShrinkDuration)
        {
            elapsed +=
                Time.deltaTime;

            float progress =
                leafShrinkDuration > 0f
                    ? elapsed /
                      leafShrinkDuration
                    : 1f;

            replacementVisual.localScale =
                Vector3.Lerp(
                    startingScale,
                    Vector3.zero,
                    progress
                );

            yield return null;
        }

        replacementVisual.localScale =
            Vector3.zero;

        replacementVisual.gameObject
            .SetActive(false);
    }


    private void CheckAllLeavesOpened()
    {
        if (allLeavesOpenedMessagePrinted)
            return;

        LeafController[] allLeaves =
            FindObjectsOfType<LeafController>();

        foreach (LeafController leaf in allLeaves)
        {
            if (!leaf.isOpen)
                return;
        }

        allLeavesOpenedMessagePrinted =
            true;

        Debug.Log(
            "Tum mevcut yapraklar acildi!"
        );
    }
}