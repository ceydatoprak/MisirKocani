using UnityEngine;

public class CornController : MonoBehaviour
{
    public int remainingLeaves = 2;
    public GameObject cornBody;

    private void Start()
    {
        if (cornBody != null)
        {
            remainingLeaves = cornBody.GetComponentsInChildren<LeafController>().Length;
        }
    }

    public void LeafRemoved()
    {
        remainingLeaves--;

        Debug.Log("Kalan yaprak: " + remainingLeaves);

        if (remainingLeaves <= 0)
        {
            Debug.Log("T�m yapraklar d��t�, m�s�r tamamen ortaya ��kt�!");

            CornPeelController peelController =
                cornBody.GetComponent<CornPeelController>();

            if (peelController != null)
            {
                peelController.StartPeeling();
            }
        }
    }
}