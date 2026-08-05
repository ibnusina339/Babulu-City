using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    /// Membuat scene Credits berisi teks kredit VENTRA yang menggulir ke atas
    /// (gaya credit film), lalu mendaftarkannya ke Build Settings. Berjalan
    /// otomatis setiap kali Editor reload script, mengikuti pola scaffolder
    /// lain di project ini. Menjalankan ulang lewat menu akan membangun ulang
    /// seluruh konten teks supaya perubahan pada daftar Lines langsung terlihat.
    /// </summary>
    [InitializeOnLoad]
    public static class CreditsSceneBuilder
    {
        const string ScenePath = "Assets/Project/Scenes/Credits.unity";
        const string EndingScenePath = "Assets/Project/Scenes/ENDING.unity";
        const string FontPath = "Assets/Tilemap/PublicPixel-rv0pA SDF.asset";

        // Objek penanda di dalam scene. Namanya membawa versi konten, sehingga
        // scene Credits lama otomatis dibangun ulang saat daftar Lines diubah.
        // Naikkan angkanya bila isi kredit diperbarui.
        const string ContentVersion = "CreditsContent.v2";

        static readonly Color Background = Hex("#0B0F1A");
        static readonly Color TitleColor = Hex("#F4BD61");
        static readonly Color RoleColor = Hex("#6C63FF");
        static readonly Color NameColor = Hex("#F4F7FF");
        static readonly Color SmallColor = Hex("#A4B0CC");

        enum Style { Title, Subtitle, Section, RoleLabel, Name, Body, Small, Final, Spacer, BigSpacer }

        // Konten kredit VENTRA. Ubah daftar ini lalu jalankan
        // Tools/BRIDA/Rebuild Credits Scene untuk memperbarui scene.
        static readonly (Style style, string text)[] Lines =
        {
            (Style.Title, "VENTRA"),
            (Style.Subtitle, "Business Venture Simulator"),
            (Style.Spacer, ""),
            (Style.Body, "Sebuah game tentang mimpi, usaha, sekolah, dan perjuangan seorang kakak\nuntuk membeli hadiah LEGO bagi adiknya."),
            (Style.BigSpacer, ""),

            (Style.Section, "DIBUAT OLEH"),
            (Style.Name, "PROJECT-B"),
            (Style.Small, "GAME DEVELOPMENT TEAM"),
            (Style.BigSpacer, ""),

            (Style.RoleLabel, "Lead Game Developer"),
            (Style.Name, "Muhammad Ibnu Sina"),
            (Style.Spacer, ""),
            (Style.RoleLabel, "Gameplay Programmer"),
            (Style.Name, "Muhammad Ibnu Sina"),
            (Style.Spacer, ""),
            (Style.RoleLabel, "Game System & Balancing"),
            (Style.Name, "Muhammad Ibnu Sina"),
            (Style.Spacer, ""),
            (Style.RoleLabel, "UI Implementation"),
            (Style.Name, "Muhammad Ibnu Sina"),
            (Style.Name, "Adhi Pratama Putra"),
            (Style.BigSpacer, ""),

            (Style.Small, "VISUAL DESIGN TEAM"),
            (Style.BigSpacer, ""),
            (Style.RoleLabel, "Lead Game Designer"),
            (Style.Name, "Adhi Pratama Putra"),
            (Style.Spacer, ""),
            (Style.RoleLabel, "Pixel Artist"),
            (Style.Name, "Adhi Pratama Putra"),
            (Style.Spacer, ""),
            (Style.RoleLabel, "UI/UX Designer"),
            (Style.Name, "Adhi Pratama Putra"),
            (Style.Name, "Muhammad Ibnu Sina"),
            (Style.Spacer, ""),
            (Style.RoleLabel, "Visual Concept & Illustration"),
            (Style.Name, "Adhi Pratama Putra"),
            (Style.Spacer, ""),
            (Style.RoleLabel, "Story & Narrative"),
            (Style.Name, "Adhi Pratama Putra"),
            (Style.BigSpacer, ""),

            (Style.Small, "RESEARCH TEAM"),
            (Style.BigSpacer, ""),
            (Style.RoleLabel, "Research Writer"),
            (Style.Name, "Muhammad Zulham Rizal"),
            (Style.Name, "Alifa Nur Azkia"),
            (Style.Name, "Annisa Aliya Dirennuang"),
            (Style.Spacer, ""),
            (Style.RoleLabel, "Data Collection & Analysis"),
            (Style.Name, "Muhammad Zulham Rizal"),
            (Style.Name, "Alifa Nur Azkia"),
            (Style.Name, "Annisa Aliya Dirennuang"),
            (Style.BigSpacer, ""),

            (Style.Section, "ANGGOTA TIM"),
            (Style.BigSpacer, ""),
            (Style.Name, "Muhammad Zulham Rizal"),
            (Style.RoleLabel, "Research Writer & Data Analyst"),
            (Style.Small, "Kelas XII-1"),
            (Style.Spacer, ""),
            (Style.Name, "Adhi Pratama Putra"),
            (Style.RoleLabel, "Lead Game Designer & Pixel Artist"),
            (Style.Small, "Kelas XII-1"),
            (Style.Spacer, ""),
            (Style.Name, "Muhammad Ibnu Sina"),
            (Style.RoleLabel, "Lead Game Developer"),
            (Style.Small, "Kelas XII-3"),
            (Style.Spacer, ""),
            (Style.Name, "Alifa Nur Azkia"),
            (Style.RoleLabel, "Research Writer & Data Analyst"),
            (Style.Small, "Kelas XII-3"),
            (Style.Spacer, ""),
            (Style.Name, "Annisa Aliya Dirennuang"),
            (Style.RoleLabel, "Research Writer & Data Analyst"),
            (Style.Small, "Kelas XII-1"),
            (Style.BigSpacer, ""),

            (Style.Section, "DIBUAT UNTUK"),
            (Style.BigSpacer, ""),
            (Style.Name, "Lomba Kreativitas dan Riset Inovasi"),
            (Style.Name, "BRIDA Kalimantan Timur"),
            (Style.Spacer, ""),
            (Style.Name, "Tahun 2026"),
            (Style.BigSpacer, ""),

            (Style.Section, "SOFTWARE DAN TOOLS"),
            (Style.BigSpacer, ""),
            (Style.RoleLabel, "Game Engine"),
            (Style.Name, "Unity"),
            (Style.Spacer, ""),
            (Style.RoleLabel, "Programming Language"),
            (Style.Name, "C#"),
            (Style.Spacer, ""),
            (Style.RoleLabel, "Pixel Art"),
            (Style.Name, "Piskel"),
            (Style.Spacer, ""),
            (Style.RoleLabel, "UI & Visual Design"),
            (Style.Name, "Piskel dan Canva"),
            (Style.Spacer, ""),
            (Style.RoleLabel, "AI Assistance"),
            (Style.Name, "Codex dan Claude Code"),
            (Style.Spacer, ""),
            (Style.RoleLabel, "Version Control"),
            (Style.Name, "Git dan GitHub"),
            (Style.BigSpacer, ""),

            (Style.Section, "SPECIAL THANKS"),
            (Style.BigSpacer, ""),
            (Style.Body, "Terima kasih kepada:"),
            (Style.BigSpacer, ""),

            (Style.Name, "Ustazah Safitri Indah"),
            (Style.Spacer, ""),
            (Style.Body, "Atas segala bimbingan, saran, dukungan, dan kepercayaan\nyang diberikan selama proses pengembangan VENTRA."),
            (Style.BigSpacer, ""),

            (Style.Name, "MAN Insan Cendekia Paser"),
            (Style.Spacer, ""),
            (Style.Body, "Atas fasilitas, kesempatan, dan dukungan yang diberikan\nkepada tim kami untuk terus belajar, berkarya, dan berinovasi."),
            (Style.BigSpacer, ""),

            (Style.Name, "Keluarga Kami"),
            (Style.Spacer, ""),
            (Style.Body, "Atas doa, kesabaran, dan dukungan yang selalu menemani kami\nselama proses pengerjaan."),
            (Style.BigSpacer, ""),

            (Style.Name, "Teman-Teman dan Game Tester"),
            (Style.Spacer, ""),
            (Style.Body, "Yang sudah mencoba game ini, memberikan masukan, menemukan bug,\ndan tetap sabar ketika beberapa fitur belum berjalan sesuai rencana."),
            (Style.BigSpacer, ""),

            (Style.Body, "Dan tentunya..."),
            (Style.Spacer, ""),
            (Style.Name, "Kamu, yang sudah memainkan VENTRA sampai akhir."),
            (Style.BigSpacer, ""),

            (Style.Section, "PESAN DARI TIM"),
            (Style.BigSpacer, ""),
            (Style.Body, "VENTRA dikembangkan dalam waktu kurang lebih dua minggu."),
            (Style.Spacer, ""),
            (Style.Body, "Di balik setiap pixel, tombol, revisi, bug, error, merge conflict, dan push\nGitHub larut malam, ada usaha kecil dari kami untuk menciptakan sebuah\ngame yang bukan hanya seru untuk dimainkan, tetapi juga membawa cerita\ndan pembelajaran."),
            (Style.Spacer, ""),
            (Style.Body, "Melalui VENTRA, kami ingin menunjukkan bahwa membangun usaha sejak\nmuda bukan hanya tentang mengejar keuntungan. Ada waktu yang harus\ndibagi, tanggung jawab yang harus dijaga, dan orang-orang yang menjadi\nalasan kita untuk terus berusaha."),
            (Style.Spacer, ""),
            (Style.Body, "Karena pada akhirnya..."),
            (Style.Spacer, ""),
            (Style.Body, "Kesuksesan bukan hanya soal seberapa banyak uang yang berhasil didapatkan."),
            (Style.Spacer, ""),
            (Style.Body, "Kesuksesan juga tentang siapa yang ikut tersenyum ketika kita berhasil."),
            (Style.BigSpacer, ""),

            (Style.Section, "THANK YOU FOR PLAYING"),
            (Style.Spacer, ""),
            (Style.Title, "VENTRA"),
            (Style.BigSpacer, ""),
            (Style.Subtitle, "Build Your Dream."),
            (Style.Subtitle, "Balance Your Life."),
            (Style.BigSpacer, ""),
            (Style.Small, "© 2026 PROJECT-B"),
            (Style.Small, "MAN Insan Cendekia Paser"),
            (Style.BigSpacer, ""),
            (Style.BigSpacer, ""),

            (Style.Final, "Press any key to continue..."),
        };

        static CreditsSceneBuilder()
        {
            EditorApplication.delayCall += CreateIfMissing;
        }

        [MenuItem("Tools/BRIDA/Rebuild Credits Scene")]
        public static void BuildFromMenu() => Build(true, forceRebuildContent: true);

        static void CreateIfMissing()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                Build(false, forceRebuildContent: false);
        }

        static void Build(bool showLog, bool forceRebuildContent)
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
                EnsureBuildSettings();
                return;
            }

            // Scene Credits versi lama tidak punya penanda versi, jadi isinya
            // dibangun ulang tanpa perlu menjalankan menu secara manual.
            bool sceneIsOpen = SceneManager.GetSceneByPath(ScenePath).isLoaded;
            Scene target = sceneIsOpen
                ? SceneManager.GetSceneByPath(ScenePath)
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            bool upToDate = FindInScene(target, ContentVersion) != null;
            if (upToDate && !forceRebuildContent)
            {
                if (!sceneIsOpen)
                    EditorSceneManager.CloseScene(target, true);
                if (showLog)
                    Debug.Log($"Scene Credits sudah versi terbaru ({ContentVersion}).");
                EnsureBuildSettings();
                return;
            }

            foreach (GameObject root in target.GetRootGameObjects())
                Object.DestroyImmediate(root);
            BuildScene(target);
            EditorSceneManager.MarkSceneDirty(target);
            EditorSceneManager.SaveScene(target);
            if (!sceneIsOpen)
                EditorSceneManager.CloseScene(target, true);
            AssetDatabase.SaveAssets();
            Debug.Log($"Scene Credits dibangun ulang ke {ContentVersion}: {ScenePath}");

            EnsureBuildSettings();
        }

        static GameObject FindInScene(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == objectName)
                    return root;
            }
            return null;
        }

        static void BuildScene(Scene scene)
        {
            TMP_FontAsset pixelFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (pixelFont == null)
                Debug.LogWarning($"Font pixel tidak ditemukan di {FontPath}. Teks kredit memakai font TMP default.");

            // Penanda versi konten, dibaca Build() agar scene lama tahu kapan
            // harus dibangun ulang.
            GameObject versionMarker = new GameObject(ContentVersion);
            SceneManager.MoveGameObjectToScene(versionMarker, scene);

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

            RectTransform viewport = RectObject("Viewport", canvasRect, V(0f, 0f), V(1f, 1f));
            viewport.gameObject.AddComponent<RectMask2D>();

            RectTransform textRect = RectObject("CreditsText", viewport, V(.12f, 1f), V(.88f, 1f));
            textRect.pivot = new Vector2(.5f, 1f);
            TextMeshProUGUI creditsText = textRect.gameObject.AddComponent<TextMeshProUGUI>();
            creditsText.text = BuildRichText();
            creditsText.font = pixelFont;
            creditsText.alignment = TextAlignmentOptions.Top;
            creditsText.textWrappingMode = TextWrappingModes.NoWrap;
            creditsText.overflowMode = TextOverflowModes.Overflow;
            creditsText.raycastTarget = false;
            creditsText.richText = true;

            TMP_Text shiftHint = TextObject(
                "ShiftHint", canvasRect, "TAHAN SHIFT UNTUK MEMPERCEPAT", 16, SmallColor,
                FontStyles.Bold, TextAlignmentOptions.BottomRight, V(.55f, .01f), V(.98f, .06f));
            shiftHint.font = pixelFont;

            CreditsController controller = canvasObject.AddComponent<CreditsController>();
            controller.viewport = viewport;
            controller.creditsText = creditsText;
            controller.shiftHint = shiftHint.gameObject;
        }

        static string BuildRichText()
        {
            var builder = new StringBuilder();
            for (int i = 0; i < Lines.Length; i++)
            {
                (Style style, string text) = Lines[i];
                AppendStyled(builder, style, text);
                if (i < Lines.Length - 1)
                    builder.Append('\n');
            }
            return builder.ToString();
        }

        static void AppendStyled(StringBuilder builder, Style style, string text)
        {
            switch (style)
            {
                case Style.Title:
                    Append(builder, text, 72, TitleColor, bold: true);
                    break;
                case Style.Subtitle:
                    Append(builder, text, 28, NameColor, bold: false);
                    break;
                case Style.Section:
                    Append(builder, text, 38, TitleColor, bold: true);
                    break;
                case Style.RoleLabel:
                    Append(builder, text, 24, RoleColor, bold: true);
                    break;
                case Style.Name:
                    Append(builder, text, 22, NameColor, bold: false);
                    break;
                case Style.Body:
                    Append(builder, text, 20, NameColor, bold: false);
                    break;
                case Style.Small:
                    Append(builder, text, 18, SmallColor, bold: false);
                    break;
                case Style.Final:
                    Append(builder, text, 30, TitleColor, bold: true);
                    break;
                case Style.Spacer:
                    Append(builder, " ", 18, Background, bold: false);
                    break;
                case Style.BigSpacer:
                    Append(builder, " ", 36, Background, bold: false);
                    break;
            }
        }

        static void Append(StringBuilder builder, string text, float size, Color color, bool bold)
        {
            string colorHex = ColorUtility.ToHtmlStringRGB(color);
            string content = bold ? $"<b>{text}</b>" : text;
            builder.Append($"<size={size}><color=#{colorHex}>{content}</color></size>");
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
