using UnityEngine;
using TMPro;

public class EndingManager : MonoBehaviour
{
    [Header("Referensi")]
    public EndingDatabase database;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI subtitleText;
    public TextMeshProUGUI descriptionText;
    public GameObject endingPanel;

    [Header("Scene Tujuan")]
    public string creditSceneName = "CreditScene";

    public void ShowEnding(StatLevel penjualan, StatLevel prestasi)
    {
        EndingData result = database.GetEnding(penjualan, prestasi);

        if (result == null)
        {
            Debug.LogWarning($"Ending tidak ditemukan untuk kombinasi Penjualan={penjualan}, Prestasi={prestasi}");
            return;
        }

        endingPanel.SetActive(true);
        titleText.text = result.title;
        subtitleText.text = result.subtitle;
        descriptionText.text = result.description;
    }

    // Panggil ini dari tombol "Lanjut ke Credits"
    public void GoToCredits()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(creditSceneName);
    }
}
