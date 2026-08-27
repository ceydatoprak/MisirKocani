using UnityEngine;
using UnityEngine.UI;

public class PeelProgressBar : MonoBehaviour
{
    [SerializeField] private Slider fillSlider;
    [SerializeField] private Image fillImage;

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
            fillImage.fillOrigin = 0; // soldan baþlasýn
            fillImage.fillAmount = 0f;
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
    }
}