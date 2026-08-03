using UnityEngine;
using TMPro;

public class EndingManager : MonoBehaviour
{
    public TextMeshProUGUI endingText;
    public GameObject endingPanel;

    public void ShowEnding(int endingNumber)
    {
        endingPanel.SetActive(true);
        endingText.text = "Ending " + endingNumber;
    }

    public void GoToCredits()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("CreditScene");
    }
}