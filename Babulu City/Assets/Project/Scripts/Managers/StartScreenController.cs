using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BabuluCity.UI
{
    /// <summary>
    /// Menghubungkan tombol StartScreen tanpa bergantung pada nama GameObject desain.
    /// Tombol dikenali dari teksnya agar hierarchy tetap bebas diubah desainer.
    /// </summary>
    public sealed class StartScreenController : MonoBehaviour
    {
        const string StartScreenScene = "StartScreen";
        const string MainScene = "Main";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void InstallOnStartScreen()
        {
            if (SceneManager.GetActiveScene().name != StartScreenScene)
                return;

            StartScreenController controller = FindFirstObjectByType<StartScreenController>();
            if (controller == null)
            {
                var controllerObject = new GameObject("StartScreen Controller");
                controller = controllerObject.AddComponent<StartScreenController>();
            }

            controller.ConfigureButtons();
        }

        void Awake()
        {
            ConfigureButtons();
        }

        void ConfigureButtons()
        {
            Button[] buttons = FindObjectsByType<Button>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (Button button in buttons)
            {
                if (button.gameObject.scene != gameObject.scene)
                    continue;

                TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
                if (label == null)
                    continue;

                string buttonText = label.text.Trim().ToLowerInvariant();
                switch (buttonText)
                {
                    case "mulai game":
                        button.interactable = true;
                        button.onClick.RemoveListener(StartNewGame);
                        button.onClick.AddListener(StartNewGame);
                        break;

                    case "lanjutkan":
                        // Diaktifkan nanti setelah sistem save tersedia.
                        button.interactable = false;
                        break;

                    case "pengaturan":
                        // Tetap tampil, tetapi belum memiliki aksi pada versi ini.
                        button.interactable = true;
                        break;
                }
            }
        }

        void StartNewGame()
        {
            if (!Application.CanStreamedLevelBeLoaded(MainScene))
            {
                Debug.LogError($"Scene '{MainScene}' belum terdaftar di Build Settings.");
                return;
            }

            SceneManager.LoadScene(MainScene);
        }
    }
}
