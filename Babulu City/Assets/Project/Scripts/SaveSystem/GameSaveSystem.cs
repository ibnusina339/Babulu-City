using System;
using System.Collections;
using IntegratedApps;
using LarisID;
using ProdukLM;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BabuluCity.SaveSystem
{
    [Serializable]
    public sealed class BabuluGameSaveData
    {
        public int version = 1;
        public int gameDayOffset;
        public float elapsedRealSeconds;
        public int completedStudySessions;
        public int studyScore;
        public int studiedDateMask;
        public int dailyGenerationCount;
        public int aiTier;
        public float playerX;
        public float playerY;
        public LarisMarketplaceSaveData marketplace;
    }

    /// <summary>
    /// Save lokal ringan untuk testing. UI tidak disimpan; hanya data mekanik.
    /// </summary>
    public sealed class GameSaveManager : MonoBehaviour
    {
        const string SaveKey = "BabuluCity.Save.V1";
        const string ContinueRequestKey = "BabuluCity.ContinueRequested";
        const string MainScene = "Main";
        const string StartScreenScene = "StartScreen";

        [SerializeField, Min(30f)] float autosaveIntervalSeconds = 150f;

        static GameSaveManager instance;
        float nextAutosaveTime;
        GameObject backPopup;
        Button confirmExitButton;
        Button cancelExitButton;
        bool restoring;

        public static bool HasSave => !string.IsNullOrWhiteSpace(PlayerPrefs.GetString(SaveKey, string.Empty));
        public static bool IsRestoring => instance != null && instance.restoring;

        void Awake()
        {
            instance = this;
            ResolveBackPopup();
            nextAutosaveTime = Time.unscaledTime + autosaveIntervalSeconds;
        }

        IEnumerator Start()
        {
            yield return null;
            if (PlayerPrefs.GetInt(ContinueRequestKey, 0) == 1)
            {
                PlayerPrefs.DeleteKey(ContinueRequestKey);
                PlayerPrefs.Save();
                RestoreNow();
            }
        }

        void Update()
        {
            if (!restoring && Time.unscaledTime >= nextAutosaveTime)
            {
                SaveNow();
                nextAutosaveTime = Time.unscaledTime + autosaveIntervalSeconds;
            }

            if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
                return;
            if (IsEditingText() || IsLaptopOpen() || AnyBlockingPopupOpen())
                return;

            ShowBackPopup();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused)
                SaveNow();
        }

        void OnApplicationQuit()
        {
            SaveNow();
        }

        public static void SaveImportant()
        {
            if (instance != null && !instance.restoring)
                instance.SaveNow();
        }

        public void SaveNow()
        {
            if (restoring || SceneManager.GetActiveScene().name != MainScene)
                return;

            GameClockUI clock = FindAnyObjectByType<GameClockUI>(FindObjectsInactive.Include);
            LarisIDManager laris = FindAnyObjectByType<LarisIDManager>(FindObjectsInactive.Include);
            VentraMeetUI ventra = FindAnyObjectByType<VentraMeetUI>(FindObjectsInactive.Include);
            PlayerMovement player = FindAnyObjectByType<PlayerMovement>(FindObjectsInactive.Include);
            ProjectFlowManager flow = ProjectFlowManager.Instance;
            laris?.EnsureInitialized();

            var data = new BabuluGameSaveData
            {
                gameDayOffset = clock != null ? clock.GameDayOffset : 0,
                elapsedRealSeconds = clock != null ? clock.ElapsedRealSeconds : 0f,
                completedStudySessions = ventra != null ? ventra.CompletedSessions : 0,
                studyScore = ventra != null ? ventra.StudyScore : 0,
                studiedDateMask = ventra != null ? ventra.StudiedDateMask : 0,
                dailyGenerationCount = DailyGenerationCounter.Count,
                aiTier = flow != null ? (int)flow.CurrentTier : 0,
                playerX = player != null ? player.transform.position.x : 0f,
                playerY = player != null ? player.transform.position.y : 0f,
                marketplace = laris?.Marketplace?.CaptureSaveData()
            };

            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        public void RestoreNow()
        {
            string json = PlayerPrefs.GetString(SaveKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json))
                return;

            BabuluGameSaveData data;
            try
            {
                data = JsonUtility.FromJson<BabuluGameSaveData>(json);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Save game tidak dapat dibaca: {exception.Message}");
                return;
            }
            if (data == null)
                return;

            restoring = true;
            GameClockUI clock = FindAnyObjectByType<GameClockUI>(FindObjectsInactive.Include);
            LarisIDManager laris = FindAnyObjectByType<LarisIDManager>(FindObjectsInactive.Include);
            VentraMeetUI ventra = FindAnyObjectByType<VentraMeetUI>(FindObjectsInactive.Include);
            PlayerMovement player = FindAnyObjectByType<PlayerMovement>(FindObjectsInactive.Include);

            clock?.RestoreState(data.gameDayOffset, data.elapsedRealSeconds);
            if (laris != null)
            {
                laris.EnsureInitialized();
                laris.Marketplace.RestoreSaveData(data.marketplace);
                laris.NotifyProductEdited();
            }
            ventra?.RestoreProgress(
                data.completedStudySessions,
                data.studyScore,
                data.studiedDateMask);
            if (player != null)
                player.transform.position = new Vector3(data.playerX, data.playerY, player.transform.position.z);
            if (ProjectFlowManager.Instance != null)
                ProjectFlowManager.Instance.SetAITier((AITier)Mathf.Clamp(data.aiTier, 0, 2));
            DailyGenerationCounter.RestoreCount(data.dailyGenerationCount);
            restoring = false;
        }

        public static void RequestContinue()
        {
            if (!HasSave)
                return;
            PlayerPrefs.SetInt(ContinueRequestKey, 1);
            PlayerPrefs.Save();
            SceneManager.LoadScene(MainScene);
        }

        public static void StartNewGame()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.DeleteKey(ContinueRequestKey);
            DailyGenerationCounter.RestoreCount(0);
            PlayerPrefs.Save();
            SceneManager.LoadScene(MainScene);
        }

        public static BabuluGameSaveData ReadSave()
        {
            string json = PlayerPrefs.GetString(SaveKey, string.Empty);
            return string.IsNullOrWhiteSpace(json)
                ? null
                : JsonUtility.FromJson<BabuluGameSaveData>(json);
        }

        void ResolveBackPopup()
        {
            Transform root = FindSceneTransform("BacktoStartScreen");
            backPopup = root?.gameObject;
            Transform panel = FindIn(root, "keluar konfirm") ?? root;
            confirmExitButton = EnsureButton(FindIn(panel, "Keluar BUtton", "Keluar Button"));
            cancelExitButton = EnsureButton(FindIn(panel, "Kembali Button"));
            if (confirmExitButton != null)
            {
                confirmExitButton.onClick.RemoveListener(ReturnToStartScreen);
                confirmExitButton.onClick.AddListener(ReturnToStartScreen);
            }
            if (cancelExitButton != null)
            {
                cancelExitButton.onClick.RemoveListener(HideBackPopup);
                cancelExitButton.onClick.AddListener(HideBackPopup);
            }
            if (backPopup != null)
                backPopup.SetActive(false);
        }

        void ShowBackPopup()
        {
            if (backPopup == null)
                ResolveBackPopup();
            backPopup?.SetActive(true);
            FindAnyObjectByType<PlayerMovement>()?.StopMovement();
        }

        void HideBackPopup()
        {
            backPopup?.SetActive(false);
            FindAnyObjectByType<PlayerMovement>()?.ResumeMovement();
        }

        void ReturnToStartScreen()
        {
            SaveNow();
            SceneManager.LoadScene(StartScreenScene);
        }

        bool AnyBlockingPopupOpen()
        {
            if (backPopup != null && backPopup.activeInHierarchy)
                return true;
            VentraMeetUI ventra = FindAnyObjectByType<VentraMeetUI>(FindObjectsInactive.Include);
            return ventra != null && ventra.HasOpenModal;
        }

        static bool IsLaptopOpen()
        {
            LaptopProximityController laptop = FindAnyObjectByType<LaptopProximityController>();
            return laptop != null && laptop.IsLaptopOpened;
        }

        static bool IsEditingText()
        {
            GameObject selected = EventSystem.current?.currentSelectedGameObject;
            TMP_InputField input = selected?.GetComponent<TMP_InputField>() ??
                                   selected?.GetComponentInParent<TMP_InputField>();
            return input != null && input.isFocused;
        }

        static Transform FindSceneTransform(string objectName)
        {
            foreach (Transform candidate in FindObjectsByType<Transform>(FindObjectsInactive.Include))
                if (candidate.gameObject.scene == SceneManager.GetActiveScene() && candidate.name == objectName)
                    return candidate;
            return null;
        }

        static Transform FindIn(Transform root, params string[] names)
        {
            if (root == null)
                return null;
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                foreach (string name in names)
                    if (child.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                        return child;
            return null;
        }

        static Button EnsureButton(Transform target)
        {
            if (target == null)
                return null;
            Button button = target.GetComponent<Button>() ?? target.gameObject.AddComponent<Button>();
            button.targetGraphic ??= target.GetComponent<Graphic>();
            return button;
        }
    }

    static class GameSaveBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (SceneManager.GetActiveScene().name != "Main" ||
                UnityEngine.Object.FindAnyObjectByType<GameSaveManager>() != null)
                return;
            new GameObject("Game Save Manager").AddComponent<GameSaveManager>();
        }
    }
}
