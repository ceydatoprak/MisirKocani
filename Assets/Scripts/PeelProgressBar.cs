using UnityEngine;
using UnityEngine.UI;

public class PeelProgressBar : MonoBehaviour
{
    [SerializeField] private Slider fillSlider;
    [SerializeField] private Image fillImage;

    [Header("Misir Ikonu")]
    [Tooltip("Fill oranina gore soldan saga hareket edecek misir ikonu.")]
    [SerializeField] private RectTransform cornIcon;

    [Tooltip("Ikonun hareket edecegi alan. Bos birakilirsa Fill Area otomatik bulunur.")]
    [SerializeField] private RectTransform iconTrack;


    private void Awake()
    {
        if (fillSlider == null)
        {
            fillSlider = GetComponent<Slider>();
        }

        if (fillImage == null)
        {
            Transform fill =
                transform.Find("Fill Area/Fill");

            if (fill != null)
            {
                fillImage =
                    fill.GetComponent<Image>();
            }
        }

        if (fillImage != null)
        {
            fillImage.type =
                Image.Type.Filled;

            fillImage.fillMethod =
                Image.FillMethod.Horizontal;

            fillImage.fillOrigin = 0;
            fillImage.fillAmount = 0f;
        }

        if (iconTrack == null)
        {
            Transform fillArea =
                transform.Find("Fill Area");

            if (fillArea != null)
            {
                iconTrack =
                    fillArea as RectTransform;
            }
        }

        SetProgress(0f);
    }


    public void SetProgress(float normalizedValue)
    {
        float progress =
            Mathf.Clamp01(normalizedValue);

        if (fillSlider != null)
        {
            fillSlider.value = progress;
        }

        if (fillImage != null)
        {
            fillImage.fillAmount = progress;
        }

        UpdateCornIconPosition(progress);
    }


    private void UpdateCornIconPosition(float progress)
    {
        if (cornIcon == null ||
            iconTrack == null)
        {
            return;
        }

        Rect trackRect =
            iconTrack.rect;

        Rect iconRect =
            cornIcon.rect;

        float halfIconWidth =
            iconRect.width * 0.5f;

        float insetMin =
            trackRect.xMin +
            halfIconWidth;

        float insetMax =
            trackRect.xMax -
            halfIconWidth;

        float targetCenterX =
            Mathf.Lerp(
                insetMin,
                insetMax,
                progress
            );

        float pivotOffsetFromCenter =
            (cornIcon.pivot.x - 0.5f) *
            iconRect.width;

        float targetPivotX =
            targetCenterX +
            pivotOffsetFromCenter;

        float anchorReferenceX =
            trackRect.xMin +
            cornIcon.anchorMin.x *
            trackRect.width;

        Vector2 anchoredPosition =
            cornIcon.anchoredPosition;

        anchoredPosition.x =
            targetPivotX -
            anchorReferenceX;

        cornIcon.anchoredPosition =
            anchoredPosition;
    }
}