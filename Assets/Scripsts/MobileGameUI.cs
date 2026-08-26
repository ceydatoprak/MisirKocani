using UnityEngine;
using UnityEngine.SceneManagement;

public class MobileGameUI : MonoBehaviour
{
    public void RestartGame()
    {
        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }

    public void ExitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        Debug.Log("Exit butonu çalýþtý. Application.Quit sadece APK/build içinde oyunu kapatýr.");
#endif
    }
}