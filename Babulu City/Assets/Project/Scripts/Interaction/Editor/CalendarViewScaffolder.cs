using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Interaction.Editor
{
    /// <summary>
    /// Memasang canvas Kalender (Assets/Project/Prefabs/UI/kalender.prefab) ke dalam
    /// scene gameplay utama sebagai "Kalender Screen" dan menghubungkan
    /// CalendarDayMarksUI ke objek "Tanda Silang Hari" di dalamnya.
    /// PlayerInteractHintController mencari layar ini lewat nama "Kalender Screen",
    /// sedangkan hint keluar memakai objek "Keluar Kalender" yang sudah ada di
    /// Canvas Interact Hint.
    /// </summary>
    [InitializeOnLoad]
    public static class CalendarViewScaffolder
    {
        const string ScenePath = "Assets/Project/Scenes/Main.unity";
        const string KalenderPrefabPath = "Assets/Project/Prefabs/UI/kalender.prefab";
        const string CalendarScreenName = "Kalender Screen";
        const string MarksRootName = "Tanda Silang Hari";
        // Hint keluar memakai objek yang sudah ada di Canvas Interact Hint,
        // jadi hint buatan versi awal scaffolder ini dibersihkan bila tersisa.
        const string ObsoleteExitHintName = "Tutup Kalender Hint";
        const int CalendarSortingOrder = 400;

        static CalendarViewScaffolder()
        {
            EditorApplication.delayCall += BuildAutomatically;
        }

        [MenuItem("Tools/Kalender/Build Calendar View")]
        public static void BuildFromMenu()
        {
            BuildCalendarView(true);
        }

        static void BuildAutomatically()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                BuildCalendarView(false);
        }

        static void BuildCalendarView(bool showLog)
        {
            GameObject kalenderPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(KalenderPrefabPath);
            if (kalenderPrefab == null)
            {
                if (showLog)
                    Debug.LogError($"Prefab kalender tidak ditemukan di {KalenderPrefabPath}.");
                return;
            }

            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedAdditively = !scene.IsValid() || !scene.isLoaded;

            if (openedAdditively)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            bool sceneChanged = false;

            Transform calendarScreen = FindInScene(scene, CalendarScreenName);
            if (calendarScreen == null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(kalenderPrefab, scene);
                instance.name = CalendarScreenName;
                calendarScreen = instance.transform;
                sceneChanged = true;
            }

            sceneChanged |= EnsureCanvasVisible(calendarScreen);
            sceneChanged |= RemoveObsoleteExitHint(calendarScreen);
            sceneChanged |= EnsureDayMarksComponent(calendarScreen);

            if (calendarScreen.gameObject.activeSelf)
            {
                calendarScreen.gameObject.SetActive(false);
                sceneChanged = true;
            }

            if (sceneChanged)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            if (openedAdditively)
                EditorSceneManager.CloseScene(scene, true);

            AssetDatabase.SaveAssets();

            if (showLog)
                Debug.Log("Kalender Screen berhasil dipasang di scene gameplay.");
        }

        static bool EnsureCanvasVisible(Transform calendarScreen)
        {
            bool changed = false;

            Canvas canvas = calendarScreen.GetComponentInChildren<Canvas>(true);
            if (canvas == null)
                return false;

            if (canvas.transform is RectTransform rect && rect.localScale == Vector3.zero)
            {
                rect.localScale = Vector3.one;
                changed = true;
            }

            if (!canvas.overrideSorting || canvas.sortingOrder != CalendarSortingOrder)
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = CalendarSortingOrder;
                changed = true;
            }

            return changed;
        }

        static bool RemoveObsoleteExitHint(Transform calendarScreen)
        {
            Transform obsolete = FindRecursive(calendarScreen, ObsoleteExitHintName);
            if (obsolete == null)
                return false;

            Object.DestroyImmediate(obsolete.gameObject);
            return true;
        }

        /// <summary>
        /// Menempelkan CalendarDayMarksUI dan menghubungkannya ke objek
        /// "Tanda Silang Hari" yang sudah ada di dalam prefab kalender.
        /// </summary>
        static bool EnsureDayMarksComponent(Transform calendarScreen)
        {
            if (FindRecursive(calendarScreen, MarksRootName) == null)
            {
                Debug.LogWarning(
                    $"'{MarksRootName}' tidak ditemukan di dalam '{CalendarScreenName}'. " +
                    "Tanda silang tanggal tidak dapat dihubungkan otomatis.");
                return false;
            }

            if (calendarScreen.GetComponentInChildren<CalendarDayMarksUI>(true) != null)
                return false;

            calendarScreen.gameObject.AddComponent<CalendarDayMarksUI>();
            return true;
        }

        static Transform FindInScene(Scene scene, string objectName)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var result = FindRecursive(root.transform, objectName);
                if (result != null)
                    return result;
            }

            return null;
        }

        static Transform FindRecursive(Transform current, string objectName)
        {
            if (current.name == objectName)
                return current;

            foreach (Transform child in current)
            {
                var result = FindRecursive(child, objectName);
                if (result != null)
                    return result;
            }

            return null;
        }
    }
}
