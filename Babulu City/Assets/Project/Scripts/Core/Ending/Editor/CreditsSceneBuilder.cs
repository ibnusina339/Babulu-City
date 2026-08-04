using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BabuluCity.Ending.EditorTools
{
    /// <summary>
    /// Membuat scene Credits sederhana berisi placeholder kredit dan tombol
    /// kembali, lalu mendaftarkannya (bersama scene ENDING yang sudah ada
    /// tapi belum terdaftar) ke Build Settings. Berjalan otomatis setiap kali
    /// Editor reload script, mengikuti pola scaffolder lain di project ini.
    /// </summary>
    [InitializeOnLoad]
    public static class CreditsSceneBuilder
    {
        const string ScenePath = "Assets/Project/Scenes/Credits.unity";
        const string EndingScenePath = "Assets/Project/Scenes/ENDING.unity";

        static readonly Color Background = Hex("#0B0F1A");
        static readonly Color TitleColor = Hex("#F4BD61");
        static readonly Color RoleColor = Hex("#6C63FF");
        static readonly Color NameColor = Hex("#F4F7FF");
        static readonly Color ClosingColor = Hex("#F4F7FF");
        static readonly Color ButtonColor = Hex("#192036");

        static readonly (string role, string placeholder)[] Roles =
        {
            ("GAME PROGRAMMER", "Nama Programmer"),
            ("GAME DESIGNER", "Nama Game Designer"),
            ("UI DESIGNER", "Nama UI Designer"),
            ("ART & ASSET", "Nama Artist"),
            ("MUSIC & SFX", "Nama Composer"),
            ("SPECIAL THANKS", "Nama Spesial"),
        };

        static CreditsSceneBuilder()
        {
            EditorApplication.delayCall += CreateIfMissing;
        }

        [MenuItem("Tools/BRIDA/Create Credits Scene")]
        public static void BuildFromMenu() => Build(true);

        static void CreateIfMissing()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                Build(false);
        }

        static void Build(bool showLog)
        {
            bool assetExists = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null;
            if (!assetExists)
            {
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                BuildScene(scene);
                EditorSceneManager.SaveScene(scene, ScenePath);
                EditorSceneManager.CloseScene(scene, true);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                if (showLog)
                    Debug.Log($"Scene Credits dibuat: {ScenePath}");
            }
            else if (showLog)
            {
                Debug.Log($"Scene Credits sudah ada: {ScenePath}");
            }

            EnsureBuildSettings();
        }

        static void BuildScene(Scene scene)
        {
            GameObject eventSystem = new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule));
            SceneManager.MoveGameObjectToScene(eventSystem, scene);
            eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();

            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera));
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0, 0, -10);
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Background;

            GameObject canvasObject = new GameObject(
                "CreditsCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            SceneManager.MoveGameObjectToScene(canvasObject, scene);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = .5f;
            RectTransform canvasRect = (RectTransform)canvasObject.transform;

            PanelObject("Background", canvasRect, Background, V(0f, 0f), V(1f, 1f));

            TextObject("Title", canvasRect, "CREDITS", 64, TitleColor, FontStyles.Bold,
                TextAlignmentOptions.Center, V(.1f, .88f), V(.9f, .97f));

            RectTransform rolesRoot = RectObject("Roles", canvasRect, V(.15f, .20f), V(.85f, .86f));
            float rowHeight = 1f / Roles.Length;
            for (int i = 0; i < Roles.Length; i++)
            {
                float top = 1f - i * rowHeight;
                float bottom = top - rowHeight;
                RectTransform row = RectObject($"Role.{i}", rolesRoot, V(0f, bottom), V(1f, top));
                TextObject("RoleLabel", row, Roles[i].role, 26, RoleColor, FontStyles.Bold,
                    TextAlignmentOptions.Center, V(0f, .48f), V(1f, 1f));
                TextObject("RoleName", row, Roles[i].placeholder, 22, NameColor, FontStyles.Normal,
                    TextAlignmentOptions.Center, V(0f, 0f), V(1f, .48f));
            }

            TextObject("Closing", canvasRect, "TERIMA KASIH TELAH BERMAIN", 30, ClosingColor,
                FontStyles.Bold, TextAlignmentOptions.Center, V(.1f, .10f), V(.9f, .18f));

            // Tombol kembali otomatis dihubungkan saat runtime oleh CreditsBootstrap
            // (CreditsController.cs), yang mencari Button pertama di scene "Credits".
            ButtonObject("Kembali Button", canvasRect, "KEMBALI KE MENU", ButtonColor,
                V(.40f, .02f), V(.60f, .07f));
        }

        static Button ButtonObject(
            string name, Transform parent, string label, Color background, Vector2 min, Vector2 max)
        {
            RectTransform rect = PanelObject(name, parent, background, min, max);
            Image image = rect.GetComponent<Image>();
            image.raycastTarget = true;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            TextObject("Label", rect, label, 20, NameColor, FontStyles.Bold,
                TextAlignmentOptions.Center, V(0f, 0f), V(1f, 1f));
            return button;
        }

        static TMP_Text TextObject(
            string name, Transform parent, string value, float size, Color color,
            FontStyles style, TextAlignmentOptions alignment, Vector2 min, Vector2 max)
        {
            RectTransform rect = RectObject(name, parent, min, max);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.fontStyle = style;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }

        static RectTransform PanelObject(string name, Transform parent, Color color, Vector2 min, Vector2 max)
        {
            RectTransform rect = RectObject(name, parent, min, max);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        static RectTransform RectObject(string name, Transform parent, Vector2 min, Vector2 max)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            return rect;
        }

        static Vector2 V(float x, float y) => new Vector2(x, y);

        static Color Hex(string value)
        {
            ColorUtility.TryParseHtmlString(value, out Color color);
            return color;
        }

        static void EnsureBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
            bool changed = false;
            changed |= EnsureSceneRegistered(scenes, ScenePath);
            changed |= EnsureSceneRegistered(scenes, EndingScenePath);
            if (changed)
                EditorBuildSettings.scenes = scenes.ToArray();
        }

        static bool EnsureSceneRegistered(List<EditorBuildSettingsScene> scenes, string path)
        {
            int index = scenes.FindIndex(item => item.path == path);
            if (index >= 0)
            {
                if (scenes[index].enabled)
                    return false;
                scenes[index] = new EditorBuildSettingsScene(path, true);
                return true;
            }
            scenes.Add(new EditorBuildSettingsScene(path, true));
            return true;
        }
    }
}
