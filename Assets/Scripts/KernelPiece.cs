using System.Collections;
using UnityEngine;

public class KernelPiece : MonoBehaviour
{
    [Header("Tane Sesi")]
    [Tooltip("Tane kocandan ayrildiginda calacak ses.")]
    [SerializeField] private AudioClip kernelPeelSound;

    [Tooltip("Tane sesinin ses seviyesi.")]
    [Range(0f, 1f)]
    [SerializeField] private float kernelSoundVolume = 0.8f;

    [Tooltip("Cok hizli soyulmada seslerin birbirine fazla binmesini engeller.")]
    [Range(0.01f, 0.2f)]
    [SerializeField] private float kernelSoundMinInterval = 0.035f;

    [Tooltip("Her tanede, surukleme pitch'inin ustune eklenen cok hafif rastgele ton farki.")]
    [Range(0f, 0.2f)]
    [SerializeField] private float kernelSoundPitchVariation = 0.02f;


    [Header("Mobil Titresim")]
    [Tooltip("Taneler soyulurken mobil cihazda titresim verilmesini acar/kapatir.")]
    public bool enableHaptics = true;

    [Tooltip("Android cihazdaki kisa titresimin suresi (milisaniye).")]
    [Range(5, 50)]
    public int hapticDurationMilliseconds = 15;

    [Tooltip("Hizli suruklemede titresimlerin ust uste binmesini engelleyen minimum aralik.")]
    [Range(0.02f, 0.2f)]
    public float hapticMinInterval = 0.06f;


    [Header("Pop Animasyonu")]
    [Tooltip("Soyulma aninda tanenin hafifce buyudugu 'pop' fazinin suresi (saniye).")]
    public float popDuration = 0.08f;

    [Tooltip("Pop fazinda tanenin ulasacagi olcek carpani (1 = degisim yok).")]
    public float popScale = 1.15f;

    [Tooltip("Pop fazinda tanenin disariya kayacagi kucuk mesafe.")]
    public float popForwardDistance = 0f;


    [Header("Firlama Hizi")]
    [Tooltip("Tanenin kocan merkezinden disariya dogru dokulme hizi.")]
    public float outwardVelocity = 0.15f;

    [Tooltip("Tanenin yukari dogru baslangic hizi.")]
    public float upwardVelocity = 0f;

    [Tooltip("Kocan cevresine teget yondeki rastgele yana sapma miktari.")]
    public float sidewaysRandomness = 0.05f;

    [Tooltip("Dususte tanenin donecegi maksimum acisal hiz.")]
    public float torqueAmount = 0.5f;


    [Header("Fizik Omru")]
    [Tooltip("Tanenin fizik simulasyonunda kalacagi yaklasik sure.")]
    public float physicsLifetime = 1.5f;

    [Tooltip("Tanenin kuculup pasif hale gelme suresi.")]
    public float shrinkDuration = 0.2f;


    [Header("Rigidbody Ayarlari")]
    [Tooltip("Soyulan taneye eklenecek Rigidbody kutlesi.")]
    public float rigidbodyMass = 0.05f;

    [Tooltip("Rigidbody dogrusal surtunmesi.")]
    public float rigidbodyDrag = 0.5f;

    [Tooltip("Rigidbody acisal surtunmesi.")]
    public float rigidbodyAngularDrag = 0.5f;


    private bool isPeeled = false;

    private AudioSource kernelAudioSource;

    private static float lastHapticTime = -100f;
    private static float lastKernelSoundTime = -100f;


    private void Awake()
    {
        kernelAudioSource = GetComponent<AudioSource>();

        if (kernelAudioSource == null)
        {
            kernelAudioSource = gameObject.AddComponent<AudioSource>();
        }

        kernelAudioSource.playOnAwake = false;
        kernelAudioSource.loop = false;
        kernelAudioSource.spatialBlend = 0f;
    }


    public bool Peel(float pitchOverride = -1f)
    {
        if (isPeeled)
            return false;

        isPeeled = true;

        TryPlayKernelSound(pitchOverride);
        TryTriggerHaptic();

        Debug.Log(gameObject.name + " tanesi soyuldu!");

        CornPeelController controller =
            GetComponentInParent<CornPeelController>();

        if (controller != null)
        {
            controller.KernelRemoved();
        }

        Vector3 outwardDirection =
            ComputeOutwardDirection(controller);

        Collider cobCollider =
            controller != null
                ? controller.GetComponent<CapsuleCollider>()
                : null;

        int ignoreRaycastLayer =
            LayerMask.NameToLayer("Ignore Raycast");

        if (ignoreRaycastLayer >= 0)
        {
            SetLayerRecursively(
                transform,
                ignoreRaycastLayer
            );
        }

        StartCoroutine(
            PopAndFall(
                outwardDirection,
                cobCollider
            )
        );

        return true;
    }


    private void TryPlayKernelSound(float pitchOverride)
    {
        if (kernelPeelSound == null ||
            kernelAudioSource == null)
        {
            return;
        }

        if (Time.unscaledTime - lastKernelSoundTime <
            kernelSoundMinInterval)
        {
            return;
        }

        lastKernelSoundTime = Time.unscaledTime;

        float centerPitch =
            pitchOverride >= 0f
                ? pitchOverride
                : 1f;

        kernelAudioSource.pitch =
            centerPitch +
            Random.Range(
                -kernelSoundPitchVariation,
                kernelSoundPitchVariation
            );

        kernelAudioSource.PlayOneShot(
            kernelPeelSound,
            kernelSoundVolume
        );
    }


    private void TryTriggerHaptic()
    {
        if (!enableHaptics)
            return;

        if (Time.unscaledTime - lastHapticTime <
            hapticMinInterval)
        {
            return;
        }

        lastHapticTime = Time.unscaledTime;

#if UNITY_ANDROID && !UNITY_EDITOR

        TriggerAndroidHaptic();

#elif UNITY_IOS && !UNITY_EDITOR

        Handheld.Vibrate();

#endif
    }


#if UNITY_ANDROID && !UNITY_EDITOR

    private void TriggerAndroidHaptic()
    {
        try
        {
            using (
                AndroidJavaClass unityPlayer =
                    new AndroidJavaClass(
                        "com.unity3d.player.UnityPlayer"
                    )
            )
            {
                AndroidJavaObject activity =
                    unityPlayer.GetStatic<AndroidJavaObject>(
                        "currentActivity"
                    );

                using (
                    AndroidJavaObject vibrator =
                        activity.Call<AndroidJavaObject>(
                            "getSystemService",
                            "vibrator"
                        )
                )
                {
                    if (vibrator == null)
                        return;

                    bool hasVibrator =
                        vibrator.Call<bool>(
                            "hasVibrator"
                        );

                    if (!hasVibrator)
                        return;

                    using (
                        AndroidJavaClass version =
                            new AndroidJavaClass(
                                "android.os.Build$VERSION"
                            )
                    )
                    {
                        int sdkVersion =
                            version.GetStatic<int>(
                                "SDK_INT"
                            );

                        if (sdkVersion >= 26)
                        {
                            using (
                                AndroidJavaClass vibrationEffect =
                                    new AndroidJavaClass(
                                        "android.os.VibrationEffect"
                                    )
                            )
                            {
                                int defaultAmplitude =
                                    vibrationEffect.GetStatic<int>(
                                        "DEFAULT_AMPLITUDE"
                                    );

                                using (
                                    AndroidJavaObject effect =
                                        vibrationEffect
                                            .CallStatic<AndroidJavaObject>(
                                                "createOneShot",
                                                (long)hapticDurationMilliseconds,
                                                defaultAmplitude
                                            )
                                )
                                {
                                    vibrator.Call(
                                        "vibrate",
                                        effect
                                    );
                                }
                            }
                        }
                        else
                        {
                            vibrator.Call(
                                "vibrate",
                                (long)hapticDurationMilliseconds
                            );
                        }
                    }
                }
            }
        }
        catch
        {
            Handheld.Vibrate();
        }
    }

#endif


    private Vector3 ComputeOutwardDirection(
        CornPeelController controller
    )
    {
        Vector3 center =
            controller != null
                ? controller.transform.position
                : transform.parent != null
                    ? transform.parent.position
                    : Vector3.zero;

        Vector3 diff =
            transform.position - center;

        diff.y = 0f;

        if (diff.sqrMagnitude < 0.0001f)
        {
            return transform.forward;
        }

        return diff.normalized;
    }


    private static void SetLayerRecursively(
        Transform root,
        int layer
    )
    {
        root.gameObject.layer = layer;

        for (int i = 0; i < root.childCount; i++)
        {
            SetLayerRecursively(
                root.GetChild(i),
                layer
            );
        }
    }


    private IEnumerator PopAndFall(
        Vector3 outwardDirection,
        Collider cobCollider
    )
    {
        Vector3 startScale =
            transform.localScale;

        Vector3 poppedScale =
            startScale * popScale;

        Vector3 startPosition =
            transform.position;

        Vector3 poppedPosition =
            startPosition +
            outwardDirection * popForwardDistance;

        float elapsed = 0f;

        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;

            float t =
                popDuration > 0f
                    ? Mathf.Clamp01(
                        elapsed / popDuration
                    )
                    : 1f;

            transform.localScale =
                Vector3.Lerp(
                    startScale,
                    poppedScale,
                    t
                );

            transform.position =
                Vector3.Lerp(
                    startPosition,
                    poppedPosition,
                    t
                );

            yield return null;
        }

        transform.localScale = poppedScale;
        transform.position = poppedPosition;

        transform.SetParent(
            null,
            true
        );

        Rigidbody rb =
            gameObject.AddComponent<Rigidbody>();

        rb.mass = rigidbodyMass;
        rb.drag = rigidbodyDrag;
        rb.angularDrag = rigidbodyAngularDrag;

        Collider kernelCollider =
            GetComponent<Collider>();

        if (kernelCollider != null)
        {
            kernelCollider.isTrigger = true;
        }

        if (kernelCollider != null &&
            cobCollider != null)
        {
            Physics.IgnoreCollision(
                kernelCollider,
                cobCollider,
                true
            );
        }

        rb.useGravity = true;
        rb.isKinematic = false;
        rb.interpolation =
            RigidbodyInterpolation.Interpolate;

        Vector3 sidewaysDirection =
            Vector3.Cross(
                Vector3.up,
                outwardDirection
            );

        Vector3 launchVelocity =
            outwardDirection *
            (
                outwardVelocity +
                Random.Range(
                    -0.03f,
                    0.03f
                )
            )
            +
            Vector3.up *
            (
                upwardVelocity +
                Random.Range(
                    -0.03f,
                    0.03f
                )
            )
            +
            sidewaysDirection *
            Random.Range(
                -sidewaysRandomness,
                sidewaysRandomness
            );

        rb.velocity = launchVelocity;

        Vector3 randomAngularVelocity =
            new Vector3(
                Random.Range(
                    -torqueAmount,
                    torqueAmount
                ),
                Random.Range(
                    -torqueAmount,
                    torqueAmount
                ),
                Random.Range(
                    -torqueAmount,
                    torqueAmount
                )
            );

        rb.angularVelocity =
            randomAngularVelocity;

        float lifetime =
            Random.Range(
                Mathf.Max(
                    0.05f,
                    physicsLifetime - 0.2f
                ),
                physicsLifetime + 0.3f
            );

        yield return new WaitForSeconds(
            lifetime
        );

        Vector3 shrinkStartScale =
            transform.localScale;

        elapsed = 0f;

        while (elapsed < shrinkDuration)
        {
            elapsed += Time.deltaTime;

            float t =
                shrinkDuration > 0f
                    ? Mathf.Clamp01(
                        elapsed / shrinkDuration
                    )
                    : 1f;

            transform.localScale =
                Vector3.Lerp(
                    shrinkStartScale,
                    Vector3.zero,
                    t
                );

            yield return null;
        }

        gameObject.SetActive(false);
    }
}