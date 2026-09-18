using UnityEngine;
using UnityEngine.UI;

public class PeelProgressBar : MonoBehaviour
{
    [SerializeField] private Slider fillSlider;
    [SerializeField] private Image fillImage;

    [Header("Misir Ikonu (Fill'in ucunda hareket eder)")]
    [Tooltip("Fill oranina gore soldan saga hareket edecek misir ikonu (orn. PB_CornIcon). " +
             "'Fill Area' nesnesinin ALTINA, Left-Middle anchor (0, 0.5) ve (0.5, 0.5) pivot ile yerlestirilmelidir.")]
    [SerializeField] private RectTransform cornIcon;

    [Tooltip("Ikonun kayacagi yolun genislik referansi. Bos birakilirsa 'Fill Area' otomatik bulunur.")]
    [SerializeField] private RectTransform iconTrack;

    private void Awake()
    {
        if (fillSlider == null)
            fillSlider = GetComponent<Slider>();

        if (fillImage == null)
        {
            Transform fill = transform.Find("Fill Area/Fill");

            if (fill != null)
                fillImage = fill.GetComponent<Image>();
        }

        if (fillImage != null)
        {
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = 0; // soldan ba�las�n
            fillImage.fillAmount = 0f;
        }

        if (iconTrack == null)
        {
            Transform fillArea = transform.Find("Fill Area");

            if (fillArea != null)
                iconTrack = fillArea as RectTransform;
        }

        SetProgress(0f);
    }

    public void SetProgress(float normalizedValue)
    {
        float progress = Mathf.Clamp01(normalizedValue);

        if (fillSlider != null)
            fillSlider.value = progress;

        if (fillImage != null)
            fillImage.fillAmount = progress;

        UpdateCornIconPosition(progress);
    }

    // Ikonu, 'iconTrack'in rect'i boyunca (soldan saga) progress oranina gore konumlandirir.
    // cornIcon veya iconTrack atanmamissa hicbir sey yapmaz; mevcut bar mantigini etkilemez.
    //
    // Ikon TAMAMEN track'in icinde kalacak sekilde hareket eder: progress=0'da ikonun SOL
    // KENARI track'in sol ucuna, progress=1'de ikonun SAG KENARI track'in sag ucuna tam
    // deger (ikon yariya tasmaz). Bunun icin hedef merkez, kendi yari genisligi kadar
    // icerden (inset) alinir. Hesap, cornIcon hangi anchor/pivot ile kurulmus olursa olsun
    // dogru sonuc verir.
    private void UpdateCornIconPosition(float progress)
    {
        if (cornIcon == null || iconTrack == null)
            return;

        Rect trackRect = iconTrack.rect;
        Rect iconRect = cornIcon.rect;

        // 1) Hedef: ikonun GORSEL MERKEZinin, track'in kendi local uzayindaki
        //    (soldan saga) hedef X konumu. Ikon tasmasin diye kendi yari genisligi
        //    kadar iceriden (trackRect.xMin + yariGenislik .. trackRect.xMax - yariGenislik) alinir.
        float halfIconWidth = iconRect.width * 0.5f;
        float insetMin = trackRect.xMin + halfIconWidth;
        float insetMax = trackRect.xMax - halfIconWidth;
        float targetCenterX = Mathf.Lerp(insetMin, insetMax, progress);

        // 2) Ikonun pivot'u merkezde olmayabilir (orn. pivot.x = 0 ise pivot ikonun sol
        //    kenarindadir); merkez yerine PIVOT noktasinin nereye gitmesi gerektigini bul.
        float pivotOffsetFromCenter = (cornIcon.pivot.x - 0.5f) * iconRect.width;
        float targetPivotX = targetCenterX + pivotOffsetFromCenter;

        // 3) anchoredPosition, pivot'un KENDI anchor referans noktasina (track rect'i
        //    icinde anchorMin.x oranina karsilik gelen nokta) gore olan konumudur.
        float anchorReferenceX = trackRect.xMin + cornIcon.anchorMin.x * trackRect.width;

        Vector2 anchoredPosition = cornIcon.anchoredPosition;
        anchoredPosition.x = targetPivotX - anchorReferenceX;
        cornIcon.anchoredPosition = anchoredPosition;
    }
}