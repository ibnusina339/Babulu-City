using TMPro;
using BabuluCity.SaveSystem;
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
        GameObject newGamePopup;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void InstallOnStartScreen()
        {
            if (SceneManager.GetActiveScene().name != StartScreenScene)
                return;

            StartScreenController controller = FindAnyObjectByType<StartScreenController>();
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
            ResolveNewGamePopup();
            Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include);

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
                        button.onClick.RemoveAllListeners();
                        button.onClick.AddListener(RequestNewGame);
                        break;

                    case "lanjutkan":
                        button.interactable = GameSaveManager.HasSave;
                        button.onClick.RemoveAllListeners();
                        button.onClick.AddListener(GameSaveManager.RequestContinue);
                        break;

                    case "pengaturan":
                        // Tetap tampil, tetapi belum memiliki aksi pada versi ini.
                        button.interactable = true;
                        break;
                }
            }
        }

        void RequestNewGame()
        {
            if (!Application.CanStreamedLevelBeLoaded(MainScene))
            {
                Debug.LogError($"Scene '{MainScene}' belum terdaftar di Build Settings.");
                return;
            }

            if (GameSaveManager.HasSave && newGamePopup != null)
                newGamePopup.SetActive(true);
            else
                GameSaveManager.StartNewGame();
        }

        void ResolveNewGamePopup()
        {
            newGamePopup = FindTransform("NewGameConfirmPopup")?.gameObject;
            if (newGamePopup == null)
                return;
            Button confirm = FindTransform("Confirm New Game", newGamePopup.transform)?.GetComponent<Button>();
            Button cancel = FindTransform("Cancel New Game", newGamePopup.transform)?.GetComponent<Button>();
            if (confirm != null)
            {
                confirm.onClick.RemoveAllListeners();
                confirm.onClick.AddListener(GameSaveManager.StartNewGame);
            }
            if (cancel != null)
            {
                cancel.onClick.RemoveAllListeners();
                cancel.onClick.AddListener(() => newGamePopup.SetActive(false));
            }
            newGamePopup.SetActive(false);
        }

        static Transform FindTransform(string objectName, Transform root = null)
        {
            Transform[] transforms = root != null
                ? root.GetComponentsInChildren<Transform>(true)
                : FindObjectsByType<Transform>(FindObjectsInactive.Include);
            foreach (Transform candidate in transforms)
                if (candidate.name.Equals(objectName, System.StringComparison.OrdinalIgnoreCase))
                    return candidate;
            return null;
        }
    }
}
