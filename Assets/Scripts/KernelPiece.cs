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

    // Kucuk tutuldu: ana pitch yukselisini artik CornPeelController'daki surukleme
    // ilerlemesi (Peel'e verilen pitchOverride) belirliyor. Bu deger sadece o pitch'in
    // ustune cok hafif dogal bir titresim ekler.
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

    // Sifir tutulur: disariya/kameraya dogru pozisyon kaymasi "ekrana dogru geliyor" hissi
    // yaratiyordu. "Yerinden cikma" hissi zaten yukaridaki olcek (popScale) buyumesiyle
    // veriliyor; pozisyon kaymasina gerek yok. Inspector'dan istenirse tekrar acilabilir.
    [Tooltip("Pop fazinda tanenin disariya kayacagi kucuk mesafe (world birim). Varsayilan 0: sadece olcek buyur, pozisyon kaymaz (ekrana dogru gelmesin diye).")]
    public float popForwardDistance = 0f;


    // Eskiden yuksekti (1.2 / 0.8) ve taneler firlama hissi verecek kadar uzaga/yukari
    // savruluyordu ("zipliyor" gibi goruniyordu). Artik taneler sicramadan, dokulur gibi
    // hafif bir disariya kayisla dogrudan asagi dusuyor.
    [Header("Firlama Hizi")]
    [Tooltip("Tanenin kocan merkezinden disariya dogru dokulme hizi (dusuk tutulur, zipmasin).")]
    public float outwardVelocity = 0.15f;

    [Tooltip("Tanenin yukari dogru baslangic hizi (0 = yukari zipmadan dogrudan dusme).")]
    public float upwardVelocity = 0f;

    [Tooltip("Disari hizina eklenen, kocan cevresine teget yondeki rastgele yana sapma miktari.")]
    public float sidewaysRandomness = 0.05f;

    // Bu deger artik bir "tork/impulse" degil, DOGRUDAN acisal hiz (radyan/saniye).
    // Kutleden/ataletten bagimsizdir; kucuk degerler gercekten yavas/nazik donus verir.
    [Tooltip("Dususte tanenin donecegi maksimum acisal hiz (radyan/saniye, eksen basina). Kucuk tutulur, cilginca donmesin.")]
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


    // Tum taneler arasinda ortak tutulur.
    // Boylece ayni karede birden fazla tane soyulsa bile titresimler ust uste binmez.
    private static float lastHapticTime = -100f;

    // Ayni mantik ses icin de kullanilir.
    // Hizli suruklemede onlarca ses ayni anda baslamasin.
    private static float lastKernelSoundTime = -100f;


    private void Awake()
    {
        kernelAudioSource = GetComponent<AudioSource>();

        // Prefabda AudioSource yoksa otomatik olarak ekle.
        if (kernelAudioSource == null)
        {
            kernelAudioSource =
                gameObject.AddComponent<AudioSource>();
        }

        kernelAudioSource.playOnAwake = false;
        kernelAudioSource.loop = false;

        // Mobil oyun efekti olarak 2D calar.
        // Kameradan uzaklasinca ses kisilmaz.
        kernelAudioSource.spatialBlend = 0f;
    }


    // pitchOverride >= 0 ise ses bu pitch etrafinda (+/- kernelSoundPitchVariation) calinir;
    // bu, CornPeelController'in surukleme boyunca kademeli yukselttigi pitch degeridir.
    // pitchOverride < 0 (varsayilan) verilirse eski rastgele-pitch davranisina dusulur.
    // Donus degeri: bu cagrinin taneyi GERCEKTEN soyup soymadigi (zaten soyulmus bir taneye
    // tekrar Peel() cagrilirsa false doner). CornPeelController, pitch ilerlemesini SADECE
    // gercekten soyulan taneler icin bir adim ilerletmek amaciyla bu degeri kullanir.
    public bool Peel(float pitchOverride = -1f)
    {
        if (isPeeled)
            return false;

        // Kilit hemen kapanir.
        // Ayni tane ikinci kez islenemez.
        isPeeled = true;


        // Tane ayrildigi anda ses.
        TryPlayKernelSound(pitchOverride);


        // Yalnizca gercekten soyulan tane icin bir kez calisir.
        TryTriggerHaptic();


        Debug.Log(
            gameObject.name +
            " tanesi soyuldu!"
        );


        CornPeelController controller =
            GetComponentInParent<CornPeelController>();


        if (controller != null)
        {
            controller.KernelRemoved();
        }


        Vector3 outwardDirection =
            ComputeOutwardDirection(controller);


        // Kocan govdesinin (CornBody) CapsuleCollider'i: tane hala buna
        // temas/gomulu haldeyken Rigidbody eklenirse, PhysX ikisini ayirmak icin
        // ani bir itme (depenetration) uygular. Bu itme, tanenin "zipliyor" ve
        // kameraya dogru firliyor gibi gorunmesinin asil sebebidir. PopAndFall
        // icinde Physics.IgnoreCollision ile bu temas tamamen devre disi birakilir.
        Collider cobCollider =
            controller != null
                ? controller.GetComponent<CapsuleCollider>()
                : null;


        int ignoreRaycastLayer =
            LayerMask.NameToLayer(
                "Ignore Raycast"
            );


        if (ignoreRaycastLayer >= 0)
        {
            SetLayerRecursively(
                transform,
                ignoreRaycastLayer
            );
        }


        StartCoroutine(
            PopAndFall(outwardDirection, cobCollider)
        );

        return true;
    }


    // pitchOverride >= 0 ise surukleme ilerlemesinden gelen pitch merkez alinir (+/- kucuk
    // dogal titresim). pitchOverride < 0 ise (Peel() parametresiz/eski gibi cagrilirsa) 1
    // etrafinda eski rastgele-pitch davranisi kullanilir.
    private void TryPlayKernelSound(float pitchOverride)
    {
        if (kernelPeelSound == null)
            return;


        if (kernelAudioSource == null)
            return;


        // Hizli suruklemede ayni anda cok fazla ses baslamasin.
        if (
            Time.unscaledTime -
            lastKernelSoundTime <
            kernelSoundMinInterval
        )
        {
            return;
        }


        lastKernelSoundTime =
            Time.unscaledTime;


        float centerPitch =
            pitchOverride >= 0f
                ? pitchOverride
                : 1f;


        // Merkez pitch'in ustune cok hafif ton farki.
        // Ayni sesin surekli tekrar ettigi hissini azaltir.
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


        if (
            Time.unscaledTime -
            lastHapticTime <
            hapticMinInterval
        )
        {
            return;
        }


        lastHapticTime =
            Time.unscaledTime;


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
                                    vibrationEffect
                                        .GetStatic<int>(
                                            "DEFAULT_AMPLITUDE"
                                        );


                                using (
                                    AndroidJavaObject effect =
                                        vibrationEffect
                                            .CallStatic<AndroidJavaObject>(
                                                "createOneShot",
                                                (long)
                                                hapticDurationMilliseconds,
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
                                (long)
                                hapticDurationMilliseconds
                            );
                        }
                    }
                }
            }
        }
        catch
        {
            // Native kisa titresim desteklenmezse
            // Unity'nin standart titresimi kullanilir.
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
            transform.position -
            center;


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
        root.gameObject.layer =
            layer;


        for (
            int i = 0;
            i < root.childCount;
            i++
        )
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
            startScale *
            popScale;


        // popForwardDistance varsayilan olarak 0'dir; pop fazinda sadece olcek
        // buyur, pozisyon kaymaz. Asil dusme (yer cekimi) bu fazdan SONRA,
        // asagidaki Rigidbody devreye girince baslar.
        Vector3 startPosition =
            transform.position;


        Vector3 poppedPosition =
            startPosition +
            outwardDirection *
            popForwardDistance;


        float elapsed =
            0f;


        while (elapsed < popDuration)
        {
            elapsed +=
                Time.deltaTime;


            float t =
                popDuration > 0f
                    ? Mathf.Clamp01(
                        elapsed /
                        popDuration
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


        transform.localScale =
            poppedScale;


        transform.position =
            poppedPosition;


        transform.SetParent(
            null,
            true
        );


        Rigidbody rb =
            gameObject
                .AddComponent<Rigidbody>();


        rb.mass =
            rigidbodyMass;


        rb.drag =
            rigidbodyDrag;


        rb.angularDrag =
            rigidbodyAngularDrag;


        // Tane hala kocan govdesine (CornBody) VEYA komsu, henuz soyulmamis
        // baska tanelere temas/gomulu haldeyken Rigidbody eklenmis olabilir.
        // Ignore-collision tek basina yeterli degil: kocanin uzerinde onlarca
        // komsu tane var, hepsiyle tek tek ugrasmak yerine, dusen tanenin
        // kendi collider'ini TRIGGER yapiyoruz. Boylece PhysX hicbir seyle
        // (kocan, komsu taneler, baska dusen taneler) fiziksel cakisma/itme
        // cozumlemesi yapmaz; Rigidbody yine de yer cekimi + verdigimiz
        // hiz/tork ile normal sekilde hareket eder, sadece "sicratan" itmeler
        // devre disi kalir. Tane zaten kisa sure sonra shrink olup pasif
        // hale geliyor, bu yuzden gercek fiziksel carpisma/durma gerekmiyor.
        Collider kernelCollider =
            GetComponent<Collider>();


        if (kernelCollider != null)
        {
            kernelCollider.isTrigger =
                true;
        }


        if (kernelCollider != null && cobCollider != null)
        {
            Physics.IgnoreCollision(
                kernelCollider,
                cobCollider,
                true
            );
        }


        rb.useGravity =
            true;


        rb.isKinematic =
            false;


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


        rb.velocity =
            launchVelocity;


        // ONEMLI: AddTorque(..., ForceMode.Impulse) KULLANILMIYOR. Impulse'un actual
        // acisal hiza etkisi kutlenin atalet momentiyle (mass/boyuta bagli, cok kucuk
        // bir SphereCollider icin I neredeyse sifira yakin) ters orantili; bu tanecikler
        // kadar kucuk/hafif bir Rigidbody'de (rigidbodyMass ~0.05) ayni impulse degeri
        // saniyede binlerce radyanlik bir donme hizina karsilik gelebiliyordu - taneler
        // dususte cilgin gibi firil firil donerek "zipliyor/sicriyor" gibi goruntu
        // veriyordu. Acisal hizi DOGRUDAN atamak, kutle/atalet momentinden tamamen
        // bagimsiz, ongorulebilir (radyan/saniye) bir sonuc verir.
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
                    physicsLifetime -
                    0.2f
                ),

                physicsLifetime +
                0.3f
            );


        yield return new WaitForSeconds(
            lifetime
        );


        Vector3 shrinkStartScale =
            transform.localScale;


        elapsed =
            0f;


        while (elapsed < shrinkDuration)
        {
            elapsed +=
                Time.deltaTime;


            float t =
                shrinkDuration > 0f
                    ? Mathf.Clamp01(
                        elapsed /
                        shrinkDuration
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