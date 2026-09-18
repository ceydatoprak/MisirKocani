using System.Collections;
using UnityEngine;

public class AnimatedLeafController : MonoBehaviour
{
    [Header("Yeni Yaprak Animasyonu")]
    public Animator animator;

    [Tooltip("Animator'daki trigger adý")]
    public string peelTrigger = "Peel";

    [Tooltip("Yeni yaprak animasyonunun süresi")]
    public float animationDuration = 1.97f;

    private bool isPeeled = false;

    private void OnMouseDown()
    {
        Peel();
    }

    public void Peel()
    {
        if (isPeeled)
            return;

        isPeeled = true;

        if (animator != null)
        {
            animator.SetTrigger(peelTrigger);
        }

        StartCoroutine(FinishAnimation());
    }

    private IEnumerator FinishAnimation()
    {
        yield return new WaitForSeconds(animationDuration);

        gameObject.SetActive(false);
    }
}