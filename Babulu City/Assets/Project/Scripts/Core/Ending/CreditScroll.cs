using UnityEngine;

public class CreditScroll : MonoBehaviour
{
    [Header("Referensi")]
    public RectTransform content;      // drag object "Content" dari Scroll View ke sini

    [Header("Pengaturan Scroll")]
    public float scrollSpeed = 40f;    // pixel per detik
    public string nextSceneName = "MainMenu";
    public bool allowSkip = true;      // tekan tombol apapun / klik untuk skip

    private float endYPosition;

    void Start()
    {
        // Hitung posisi akhir: tinggi konten + tinggi layar,
        // supaya credit dianggap selesai saat teks terakhir sudah lewat atas layar
        endYPosition = content.rect.height + Screen.height;
    }

    void Update()
    {
        content.anchoredPosition += new Vector2(0, scrollSpeed * Time.deltaTime);

        if (content.anchoredPosition.y >= endYPosition)
        {
            GoToNextScene();
        }

        if (allowSkip && (Input.GetMouseButtonDown(0) || Input.anyKeyDown))
        {
            GoToNextScene();
        }
    }

    void GoToNextScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
    }
}
