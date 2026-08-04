#if UNITY_EDITOR
using System.Linq;
using IntegratedApps;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BabuluCity.EditorTools
{
    public static class ProgressionSceneSetup
    {
        const string StartPath = "Assets/Project/Scenes/StartScreen.unity";
        const string MainPath = "Assets/Project/Scenes/Main.unity";
        const string EndingPath = "Assets/Project/Scenes/ENDING.unity";
        const string CreditsPath = "Assets/Project/Scenes/Credits.unity";

        [MenuItem("BRIDA/Setup Progression Scenes")]
        public static void Apply()
        {
            SetupStartScreen();
            TMP_FontAsset font = SetupEnding();
            SetupCredits(font);
            SetupMainClock();
            EnsureBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Progression scene setup selesai.");
        }

        static void SetupStartScreen()
        {
            Scene scene = EditorSceneManager.OpenScene(StartPath, OpenSceneMode.Single);
            Transform ui = Find(scene, "UI");
            if (ui == null || Find(scene, "NewGameConfirmPopup") != null)
                return;

            GameObject popup = CreateImage("NewGameConfirmPopup", ui, new Color(0f, 0f, 0f, .72f));
            Stretch((RectTransform)popup.transform);
            GameObject panel = CreateImage("Panel", popup.transform, new Color(.035f, .075f, .13f, 1f));
            RectTransform panelRect = (RectTransform)panel.transform;
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(.5f, .5f);
            panelRect.sizeDelta = new Vector2(680f, 330f);
            panelRect.anchoredPosition = Vector2.zero;

            TMP_FontAsset font = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include)
                .FirstOrDefault(text => text.gameObject.scene == scene)?.font;
            CreateText("Title", panel.transform,
                "SAVE GAME DITEMUKAN\nMulai ulang akan mengganti progres sebelumnya.",
                font, 28f, new Vector2(0f, 70f), new Vector2(610f, 120f));

            Button source = Object.FindObjectsByType<Button>(FindObjectsInactive.Include)
                .FirstOrDefault(button => button.gameObject.scene == scene && button.name == "Mulai Game Button");
            CreateClonedButton(source, panel.transform, "Confirm New Game", "MULAI ULANG",
                new Vector2(-165f, -85f));
            CreateClonedButton(source, panel.transform, "Cancel New Game", "BATAL",
                new Vector2(165f, -85f));
            popup.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        static TMP_FontAsset SetupEnding()
        {
            Scene scene = EditorSceneManager.OpenScene(EndingPath, OpenSceneMode.Single);
            TMP_Text existing = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include)
                .FirstOrDefault(text => text.gameObject.scene == scene);
            TMP_FontAsset font = existing?.font;
            if (Find(scene, "Credit Button") == null)
            {
                Transform parent = Find(scene, "Ending panel");
                GameObject buttonObject = CreateImage("Credit Button", parent, new Color(.08f, .20f, .38f, 1f));
                buttonObject.AddComponent<Button>();
                RectTransform rect = (RectTransform)buttonObject.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
                rect.sizeDelta = new Vector2(300f, 70f);
                rect.anchoredPosition = new Vector2(0f, -360f);
                CreateText("Text (TMP)", buttonObject.transform, "CREDIT", font, 24f, Vector2.zero, rect.sizeDelta);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            return font;
        }

        static void SetupCredits(TMP_FontAsset font)
        {
            Scene scene = System.IO.File.Exists(CreditsPath)
                ? EditorSceneManager.OpenScene(CreditsPath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (Find(scene, "Credits Canvas") == null)
            {
                GameObject canvasObject = new GameObject("Credits Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                Canvas canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);

                GameObject background = CreateImage("Background", canvasObject.transform, new Color(.015f, .035f, .07f, 1f));
                Stretch((RectTransform)background.transform);
                CreateText("Credit Title", background.transform, "BRIDA — BABULU CITY", font, 48f,
                    new Vector2(0f, 350f), new Vector2(1200f, 100f));
                CreateText("Credit Content", background.transform,
                    "PROJECT TEAM\n\nIBNU  •  TAMA  •  ALIFA  •  RIZAL  •  DIREN\n\nGame Design  •  Programming  •  Art  •  UI/UX\n\nTerima kasih sudah bermain!\n\n(Teks credit ini dapat kamu ganti langsung di scene Credits)",
                    font, 28f, new Vector2(0f, 20f), new Vector2(1300f, 560f));
                GameObject back = CreateImage("Back Button", background.transform, new Color(.08f, .20f, .38f, 1f));
                back.AddComponent<Button>();
                RectTransform backRect = (RectTransform)back.transform;
                backRect.anchorMin = backRect.anchorMax = new Vector2(.5f, .5f);
                backRect.sizeDelta = new Vector2(340f, 72f);
                backRect.anchoredPosition = new Vector2(0f, -390f);
                CreateText("Text (TMP)", back.transform, "KEMBALI KE MENU", font, 22f, Vector2.zero, backRect.sizeDelta);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, CreditsPath);
        }

        static void SetupMainClock()
        {
            Scene scene = EditorSceneManager.OpenScene(MainPath, OpenSceneMode.Single);
            GameClockUI clock = Object.FindObjectsByType<GameClockUI>(FindObjectsInactive.Include)
                .FirstOrDefault(item => item.gameObject.scene == scene);
            if (clock != null)
            {
                clock.startDate = "02/08/2026";
                clock.startHour = 20;
                clock.endHour = 24;
                clock.realSecondsPerGameMinute = 2f;
                EditorUtility.SetDirty(clock);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        static void EnsureBuildSettings()
        {
            string[] paths = { StartPath, MainPath, EndingPath, CreditsPath };
            var scenes = EditorBuildSettings.scenes.ToList();
            foreach (string path in paths)
                if (scenes.All(scene => scene.path != path))
                    scenes.Add(new EditorBuildSettingsScene(path, true));
            // StartScreen selalu menjadi entry point build.
            scenes = scenes.OrderBy(scene => scene.path == StartPath ? 0 : 1).ToList();
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        static GameObject CreateImage(string name, Transform parent, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return go;
        }

        static TMP_Text CreateText(string name, Transform parent, string value, TMP_FontAsset font,
            float size, Vector2 position, Vector2 dimensions)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.sizeDelta = dimensions;
            rect.anchoredPosition = position;
            TMP_Text text = go.GetComponent<TMP_Text>();
            text.text = value;
            text.font = font;
            text.fontSize = size;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            return text;
        }

        static void CreateClonedButton(Button source, Transform parent, string name, string label, Vector2 position)
        {
            GameObject go;
            if (source != null)
                go = Object.Instantiate(source.gameObject, parent);
            else
            {
                go = CreateImage(name, parent, new Color(.08f, .20f, .38f, 1f));
                go.AddComponent<Button>();
            }
            go.name = name;
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(280f, 70f);
            rect.anchoredPosition = position;
            TMP_Text text = go.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
                text.text = label;
            go.GetComponent<Button>().onClick.RemoveAllListeners();
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static Transform Find(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                    if (child.name == objectName)
                        return child;
            return null;
        }
    }
}
#endif
