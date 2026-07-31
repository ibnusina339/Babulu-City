using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProdukLM.Editor
{
    [InitializeOnLoad]
    public static class ResultPanelScaffolder
    {
        const string ScenePath = "Assets/Project/Scenes/ProdukLM Test.unity";
        const string ResultPanelName = "Result Panel";
        const string ScreenRootName = "ResultScreenRoot";
        const string TemplatePrefabPath = "Assets/Project/Prefabs/UI/ProdukLMResultPanelTemplate.prefab";

        static readonly Color32 BackgroundColor = new(12, 17, 32, 255);
        static readonly Color32 PanelColor = new(25, 32, 54, 255);
        static readonly Color32 AccentColor = new(108, 99, 255, 255);
        static readonly Color32 SecondaryAccentColor = new(49, 210, 190, 255);
        static readonly Color32 PrimaryTextColor = new(244, 247, 255, 255);
        static readonly Color32 SecondaryTextColor = new(164, 176, 204, 255);

        static ResultPanelScaffolder()
        {
            EditorApplication.delayCall += BuildAutomatically;
        }

        [MenuItem("Tools/ProdukLM/Build Result Panel")]
        public static void BuildFromMenu()
        {
            BuildResultPanel(true);
        }

        [MenuItem("Tools/ProdukLM/Reset Daily Product Limit (Testing)")]
        public static void ResetDailyLimitForTesting()
        {
            PlayerPrefs.DeleteKey("ProdukLM.DailyProductDate");
            PlayerPrefs.DeleteKey("ProdukLM.DailyProductCount");
            PlayerPrefs.Save();
            Debug.Log("Kuota produksi harian ProdukLM sudah di-reset untuk testing.");
        }

        static void BuildAutomatically()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                BuildResultPanel(false);
        }

        static void BuildResultPanel(bool showLog)
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedAdditively = !scene.IsValid() || !scene.isLoaded;

            if (openedAdditively)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            Transform resultPanel = FindInScene(scene, ResultPanelName);
            if (resultPanel == null)
            {
                if (openedAdditively)
                    EditorSceneManager.CloseScene(scene, true);
                Debug.LogError($"Tidak menemukan '{ResultPanelName}' di {ScenePath}.");
                return;
            }

            Transform screenRoot = resultPanel.Find(ScreenRootName);
            bool sceneChanged = false;

            if (screenRoot == null)
            {
                BuildHierarchy(resultPanel);
                screenRoot = resultPanel.Find(ScreenRootName);
                sceneChanged = true;
            }

            Transform optionPanel = FindInScene(scene, "Option Select");
            sceneChanged |= MigrateToThreeStatTemplate(resultPanel, screenRoot);
            sceneChanged |= EnsureNavigationButtons(screenRoot, optionPanel);
            sceneChanged |= EnsureDailyLimitLabel(
                optionPanel,
                0.72f,
                0.91f,
                0.98f,
                0.98f,
                TextAlignmentOptions.Center);
            sceneChanged |= EnsureTemplatePrefab(screenRoot);
            EnsureDailyLimitOnTemplatePrefab();

            if (sceneChanged)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            if (openedAdditively)
                EditorSceneManager.CloseScene(scene, true);

            AssetDatabase.SaveAssets();

            if (showLog)
                Debug.Log("Result Panel ProdukLM berhasil dibuat dan disimpan.");
        }

        static void BuildHierarchy(Transform resultPanel)
        {
            var scaler = resultPanel.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
            }

            var screenRoot = CreateRect(ScreenRootName, resultPanel);
            Stretch(screenRoot);

            var background = CreatePanel("Background", screenRoot, BackgroundColor);
            Stretch(background.rectTransform);

            var title = CreateText(
                "Title",
                screenRoot,
                "PRODUK BERHASIL DIBUAT!",
                42,
                PrimaryTextColor,
                TextAlignmentOptions.Center,
                FontStyles.Bold);
            SetAnchors(title.rectTransform, 0.05f, 0.91f, 0.95f, 0.98f);

            var promptPanel = CreatePanel("PromptCard", screenRoot, PanelColor);
            SetAnchors(promptPanel.rectTransform, 0.05f, 0.76f, 0.95f, 0.89f);

            var promptTitle = CreateText(
                "PromptTitle",
                promptPanel.transform,
                "PROMPT AKHIR",
                18,
                SecondaryAccentColor,
                TextAlignmentOptions.Left,
                FontStyles.Bold);
            SetAnchors(promptTitle.rectTransform, 0.035f, 0.60f, 0.25f, 0.90f);

            var finalPrompt = CreateText(
                "FinalPromptText",
                promptPanel.transform,
                string.Empty,
                22,
                PrimaryTextColor,
                TextAlignmentOptions.Left);
            SetAnchors(finalPrompt.rectTransform, 0.035f, 0.12f, 0.965f, 0.62f);

            var qualityPanel = CreatePanel("QualityCard", screenRoot, PanelColor);
            SetAnchors(qualityPanel.rectTransform, 0.05f, 0.35f, 0.35f, 0.73f);

            var productName = CreateText(
                "ProductNameText",
                qualityPanel.transform,
                "Produk Digital",
                30,
                PrimaryTextColor,
                TextAlignmentOptions.Center,
                FontStyles.Bold);
            SetAnchors(productName.rectTransform, 0.08f, 0.79f, 0.92f, 0.94f);

            var qualityTitle = CreateText(
                "QualityTitle",
                qualityPanel.transform,
                "QUALITY",
                18,
                SecondaryTextColor,
                TextAlignmentOptions.Center,
                FontStyles.Bold);
            SetAnchors(qualityTitle.rectTransform, 0.10f, 0.64f, 0.90f, 0.75f);

            var qualityValue = CreateText(
                "QualityValueText",
                qualityPanel.transform,
                "0%",
                72,
                SecondaryAccentColor,
                TextAlignmentOptions.Center,
                FontStyles.Bold);
            SetAnchors(qualityValue.rectTransform, 0.10f, 0.34f, 0.90f, 0.65f);

            var qualityLabel = CreateText(
                "QualityLabelText",
                qualityPanel.transform,
                "Cukup",
                24,
                PrimaryTextColor,
                TextAlignmentOptions.Center,
                FontStyles.Bold);
            SetAnchors(qualityLabel.rectTransform, 0.10f, 0.22f, 0.90f, 0.35f);

            var qualitySlider = CreateSlider("QualitySlider", qualityPanel.transform, SecondaryAccentColor);
            SetAnchors(qualitySlider.GetComponent<RectTransform>(), 0.10f, 0.10f, 0.90f, 0.18f);

            var statsPanel = CreatePanel("StatsCard", screenRoot, PanelColor);
            SetAnchors(statsPanel.rectTransform, 0.38f, 0.35f, 0.95f, 0.73f);

            var statsTitle = CreateText(
                "StatsTitle",
                statsPanel.transform,
                "STATISTIK PRODUK",
                24,
                PrimaryTextColor,
                TextAlignmentOptions.Left,
                FontStyles.Bold);
            SetAnchors(statsTitle.rectTransform, 0.05f, 0.82f, 0.95f, 0.96f);

            CreateStatRow(statsPanel.transform, "Relevansi", 0.48f, out var relevansiSlider, out var relevansiText);
            CreateStatRow(statsPanel.transform, "Nilai Jual", 0.18f, out var nilaiJualSlider, out var nilaiJualText);

            var feedbackPanel = CreatePanel("FeedbackPanel", screenRoot, PanelColor);
            SetAnchors(feedbackPanel.rectTransform, 0.05f, 0.07f, 0.72f, 0.31f);

            var feedbackTitle = CreateText(
                "FeedbackTitle",
                feedbackPanel.transform,
                "ANALISIS KOMBINASI",
                22,
                PrimaryTextColor,
                TextAlignmentOptions.Left,
                FontStyles.Bold);
            SetAnchors(feedbackTitle.rectTransform, 0.04f, 0.72f, 0.96f, 0.92f);

            var feedbackContainer = CreateRect("FeedbackContainer", feedbackPanel.transform);
            SetAnchors(feedbackContainer, 0.04f, 0.10f, 0.96f, 0.68f);
            feedbackContainer.gameObject.AddComponent<RectMask2D>();
            var feedbackLayout = feedbackContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            feedbackLayout.spacing = 8;
            feedbackLayout.childControlHeight = true;
            feedbackLayout.childControlWidth = true;
            feedbackLayout.childForceExpandHeight = false;
            feedbackLayout.childForceExpandWidth = true;

            var feedbackTemplate = CreateText(
                "FeedbackLineTemplate",
                screenRoot,
                string.Empty,
                19,
                SecondaryTextColor,
                TextAlignmentOptions.Left);
            feedbackTemplate.gameObject.SetActive(false);

            var retryButton = CreateButton("BackToBuilderButton", screenRoot, "KEMBALI KE BUILDER");
            SetAnchors(retryButton.GetComponent<RectTransform>(), 0.76f, 0.12f, 0.94f, 0.21f);
            retryButton.gameObject.AddComponent<BackButtonUI>();

            var hintText = CreateText(
                "HintText",
                screenRoot,
                "Gunakan hasil analisis untuk menyusun kombinasi yang lebih kuat.",
                17,
                SecondaryTextColor,
                TextAlignmentOptions.Center);
            SetAnchors(hintText.rectTransform, 0.74f, 0.05f, 0.96f, 0.11f);

            EnsureDailyLimitLabel(
                screenRoot,
                0.74f,
                0.23f,
                0.96f,
                0.30f,
                TextAlignmentOptions.Center);

            var resultUI = screenRoot.GetComponent<StatsResultUI>();
            if (resultUI == null)
                resultUI = screenRoot.gameObject.AddComponent<StatsResultUI>();

            resultUI.productNameText = productName;
            resultUI.finalPromptText = finalPrompt;
            resultUI.qualityLabelText = qualityLabel;
            resultUI.qualitySlider = qualitySlider;
            resultUI.qualityText = qualityValue;
            resultUI.relevansiSlider = relevansiSlider;
            resultUI.relevansiText = relevansiText;
            resultUI.nilaiJualSlider = nilaiJualSlider;
            resultUI.nilaiJualText = nilaiJualText;
            resultUI.feedbackContainer = feedbackContainer;
            resultUI.feedbackLinePrefab = feedbackTemplate;

            EditorUtility.SetDirty(resultUI);
        }

        static bool MigrateToThreeStatTemplate(Transform resultPanel, Transform screenRoot)
        {
            bool changed = false;

            string[] removedObjects =
            {
                "EstetikaLabel",
                "EstetikaSlider",
                "EstetikaValueText",
                "ProfesionalismeLabel",
                "ProfesionalismeSlider",
                "ProfesionalismeValueText",
            };

            foreach (string objectName in removedObjects)
            {
                Transform target = FindRecursive(screenRoot, objectName);
                if (target == null)
                    continue;

                Object.DestroyImmediate(target.gameObject);
                changed = true;
            }

            var oldResultUI = resultPanel.GetComponent<StatsResultUI>();
            var resultUI = screenRoot.GetComponent<StatsResultUI>();

            if (resultUI == null)
            {
                resultUI = screenRoot.gameObject.AddComponent<StatsResultUI>();
                changed = true;
            }

            if (oldResultUI != null && oldResultUI != resultUI)
            {
                resultUI.productNameText = oldResultUI.productNameText;
                resultUI.finalPromptText = oldResultUI.finalPromptText;
                resultUI.qualityLabelText = oldResultUI.qualityLabelText;
                resultUI.qualitySlider = oldResultUI.qualitySlider;
                resultUI.qualityText = oldResultUI.qualityText;
                resultUI.relevansiSlider = oldResultUI.relevansiSlider;
                resultUI.relevansiText = oldResultUI.relevansiText;
                resultUI.nilaiJualSlider = oldResultUI.nilaiJualSlider;
                resultUI.nilaiJualText = oldResultUI.nilaiJualText;
                resultUI.feedbackContainer = oldResultUI.feedbackContainer;
                resultUI.feedbackLinePrefab = oldResultUI.feedbackLinePrefab;

                Object.DestroyImmediate(oldResultUI);
                changed = true;
            }

            changed |= AssignIfDifferent(
                resultUI,
                FindComponent<TMP_Text>(screenRoot, "ProductNameText"),
                FindComponent<TMP_Text>(screenRoot, "FinalPromptText"),
                FindComponent<TMP_Text>(screenRoot, "QualityLabelText"),
                FindComponent<Slider>(screenRoot, "QualitySlider"),
                FindComponent<TMP_Text>(screenRoot, "QualityValueText"),
                FindComponent<Slider>(screenRoot, "RelevansiSlider"),
                FindComponent<TMP_Text>(screenRoot, "RelevansiValueText"),
                FindComponent<Slider>(screenRoot, "Nilai JualSlider"),
                FindComponent<TMP_Text>(screenRoot, "Nilai JualValueText"),
                FindRecursive(screenRoot, "FeedbackContainer"),
                FindComponent<TMP_Text>(screenRoot, "FeedbackLineTemplate"));

            changed |= SetStatRowLayout(screenRoot, "Relevansi", 0.48f);
            changed |= SetStatRowLayout(screenRoot, "Nilai Jual", 0.18f);

            if (changed)
                EditorUtility.SetDirty(resultUI);

            return changed;
        }

        static bool AssignIfDifferent(
            StatsResultUI resultUI,
            TMP_Text productName,
            TMP_Text finalPrompt,
            TMP_Text qualityLabel,
            Slider qualitySlider,
            TMP_Text qualityText,
            Slider relevansiSlider,
            TMP_Text relevansiText,
            Slider nilaiJualSlider,
            TMP_Text nilaiJualText,
            Transform feedbackContainer,
            TMP_Text feedbackTemplate)
        {
            bool changed = false;

            changed |= Assign(ref resultUI.productNameText, productName);
            changed |= Assign(ref resultUI.finalPromptText, finalPrompt);
            changed |= Assign(ref resultUI.qualityLabelText, qualityLabel);
            changed |= Assign(ref resultUI.qualitySlider, qualitySlider);
            changed |= Assign(ref resultUI.qualityText, qualityText);
            changed |= Assign(ref resultUI.relevansiSlider, relevansiSlider);
            changed |= Assign(ref resultUI.relevansiText, relevansiText);
            changed |= Assign(ref resultUI.nilaiJualSlider, nilaiJualSlider);
            changed |= Assign(ref resultUI.nilaiJualText, nilaiJualText);
            changed |= Assign(ref resultUI.feedbackContainer, feedbackContainer);
            changed |= Assign(ref resultUI.feedbackLinePrefab, feedbackTemplate);

            return changed;
        }

        static bool Assign<T>(ref T current, T next) where T : Object
        {
            if (current == next)
                return false;

            current = next;
            return true;
        }

        static bool SetStatRowLayout(Transform root, string label, float bottom)
        {
            const float height = 0.18f;
            bool changed = false;

            var labelRect = FindRecursive(root, label + "Label") as RectTransform;
            var sliderRect = FindRecursive(root, label + "Slider") as RectTransform;
            var valueRect = FindRecursive(root, label + "ValueText") as RectTransform;

            changed |= SetAnchorsIfDifferent(labelRect, 0.05f, bottom, 0.30f, bottom + height);
            changed |= SetAnchorsIfDifferent(sliderRect, 0.31f, bottom + 0.045f, 0.84f, bottom + height - 0.045f);
            changed |= SetAnchorsIfDifferent(valueRect, 0.85f, bottom, 0.96f, bottom + height);
            return changed;
        }

        static bool SetAnchorsIfDifferent(
            RectTransform rect,
            float minX,
            float minY,
            float maxX,
            float maxY)
        {
            if (rect == null)
                return false;

            var min = new Vector2(minX, minY);
            var max = new Vector2(maxX, maxY);
            bool changed = rect.anchorMin != min ||
                           rect.anchorMax != max ||
                           rect.offsetMin != Vector2.zero ||
                           rect.offsetMax != Vector2.zero;

            if (changed)
                SetAnchors(rect, minX, minY, maxX, maxY);

            return changed;
        }

        static bool EnsureTemplatePrefab(Transform screenRoot)
        {
            if (screenRoot == null ||
                AssetDatabase.LoadAssetAtPath<GameObject>(TemplatePrefabPath) != null)
                return false;

            var prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(
                screenRoot.gameObject,
                TemplatePrefabPath,
                InteractionMode.AutomatedAction);

            if (prefab == null)
            {
                Debug.LogError($"Gagal membuat prefab template di {TemplatePrefabPath}.");
                return false;
            }

            Debug.Log($"Template Result UI dibuat di {TemplatePrefabPath}.");
            return true;
        }

        static void EnsureDailyLimitOnTemplatePrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(TemplatePrefabPath) == null)
                return;

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(TemplatePrefabPath);
            try
            {
                bool changed = EnsureDailyLimitLabel(
                    prefabRoot.transform,
                    0.74f,
                    0.23f,
                    0.96f,
                    0.30f,
                    TextAlignmentOptions.Center);

                if (changed)
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, TemplatePrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        static bool EnsureDailyLimitLabel(
            Transform parent,
            float minX,
            float minY,
            float maxX,
            float maxY,
            TextAlignmentOptions alignment)
        {
            if (parent == null)
                return false;

            Transform existing = parent.Find("DailyLimitText");
            TMP_Text text;
            bool changed = false;

            if (existing == null)
            {
                text = CreateText(
                    "DailyLimitText",
                    parent,
                    "Sisa produksi hari ini: 5/5",
                    18,
                    SecondaryAccentColor,
                    alignment,
                    FontStyles.Bold);
                changed = true;
            }
            else
            {
                text = existing.GetComponent<TMP_Text>();
                if (text == null)
                {
                    text = existing.gameObject.AddComponent<TextMeshProUGUI>();
                    text.fontSize = 18;
                    text.color = SecondaryAccentColor;
                    text.alignment = alignment;
                    text.fontStyle = FontStyles.Bold;
                    text.raycastTarget = false;
                    changed = true;
                }
            }

            if (text.GetComponent<DailyLimitUI>() == null)
            {
                text.gameObject.AddComponent<DailyLimitUI>();
                changed = true;
            }

            changed |= SetAnchorsIfDifferent(
                text.rectTransform,
                minX,
                minY,
                maxX,
                maxY);
            return changed;
        }

        static bool EnsureNavigationButtons(Transform screenRoot, Transform optionPanel)
        {
            bool changed = false;

            if (screenRoot != null)
            {
                Transform resultButton = screenRoot.Find("BackToBuilderButton") ??
                                         screenRoot.Find("BuatLagiButton");

                if (resultButton != null)
                {
                    if (resultButton.name != "BackToBuilderButton")
                    {
                        resultButton.name = "BackToBuilderButton";
                        changed = true;
                    }

                    var oldBack = resultButton.GetComponent<BackToStartButtonUI>();
                    if (oldBack != null)
                    {
                        Object.DestroyImmediate(oldBack);
                        changed = true;
                    }

                    if (resultButton.GetComponent<BackButtonUI>() == null)
                    {
                        resultButton.gameObject.AddComponent<BackButtonUI>();
                        changed = true;
                    }

                    var label = resultButton.GetComponentInChildren<TMP_Text>(true);
                    if (label != null && label.text != "KEMBALI KE BUILDER")
                    {
                        label.text = "KEMBALI KE BUILDER";
                        changed = true;
                    }
                }
            }

            if (optionPanel != null)
            {
                Transform cancelTransform = optionPanel.Find("CancelButton");
                if (cancelTransform == null)
                {
                    var cancelButton = CreateButton(
                        "CancelButton",
                        optionPanel,
                        "BATAL / KEMBALI");
                    SetAnchors(
                        cancelButton.GetComponent<RectTransform>(),
                        0.02f,
                        0.91f,
                        0.16f,
                        0.98f);
                    cancelButton.gameObject.AddComponent<BackButtonUI>();
                    cancelButton.transform.SetAsLastSibling();
                    changed = true;
                }
                else if (cancelTransform.GetComponent<BackButtonUI>() == null)
                {
                    cancelTransform.gameObject.AddComponent<BackButtonUI>();
                    changed = true;
                }
            }

            return changed;
        }

        static void CreateStatRow(
            Transform parent,
            string label,
            float bottom,
            out Slider slider,
            out TMP_Text valueText)
        {
            const float height = 0.14f;

            var labelText = CreateText(
                label + "Label",
                parent,
                label,
                20,
                PrimaryTextColor,
                TextAlignmentOptions.Left,
                FontStyles.Bold);
            SetAnchors(labelText.rectTransform, 0.05f, bottom, 0.30f, bottom + height);

            slider = CreateSlider(label + "Slider", parent, AccentColor);
            SetAnchors(slider.GetComponent<RectTransform>(), 0.31f, bottom + 0.035f, 0.84f, bottom + height - 0.035f);

            valueText = CreateText(
                label + "ValueText",
                parent,
                "0%",
                22,
                SecondaryAccentColor,
                TextAlignmentOptions.Center,
                FontStyles.Bold);
            SetAnchors(valueText.rectTransform, 0.85f, bottom, 0.96f, bottom + height);
        }

        static Slider CreateSlider(string name, Transform parent, Color fillColor)
        {
            var root = CreateRect(name, parent);
            var slider = root.gameObject.AddComponent<Slider>();
            slider.minValue = 0;
            slider.maxValue = 100;
            slider.wholeNumbers = true;
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;

            var background = CreatePanel("Background", root, new Color32(51, 59, 82, 255));
            Stretch(background.rectTransform);

            var fillArea = CreateRect("Fill Area", root);
            Stretch(fillArea);

            var fill = CreatePanel("Fill", fillArea, fillColor);
            Stretch(fill.rectTransform);

            slider.fillRect = fill.rectTransform;
            slider.targetGraphic = background;
            return slider;
        }

        static Button CreateButton(string name, Transform parent, string label)
        {
            var panel = CreatePanel(name, parent, AccentColor);
            var button = panel.gameObject.AddComponent<Button>();
            button.targetGraphic = panel;

            var text = CreateText(
                "Label",
                panel.transform,
                label,
                20,
                Color.white,
                TextAlignmentOptions.Center,
                FontStyles.Bold);
            Stretch(text.rectTransform);
            return button;
        }

        static Image CreatePanel(string name, Transform parent, Color color)
        {
            var rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        static TMP_Text CreateText(
            string name,
            Transform parent,
            string content,
            float fontSize,
            Color color,
            TextAlignmentOptions alignment,
            FontStyles style = FontStyles.Normal)
        {
            var rect = CreateRect(name, parent);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.fontStyle = style;
            text.raycastTarget = false;
            return text;
        }

        static RectTransform CreateRect(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.layer = parent.gameObject.layer;
            var rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        static void SetAnchors(
            RectTransform rect,
            float minX,
            float minY,
            float maxX,
            float maxY)
        {
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static void Stretch(RectTransform rect)
        {
            SetAnchors(rect, 0, 0, 1, 1);
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

        static T FindComponent<T>(Transform root, string objectName) where T : Component
        {
            Transform target = FindRecursive(root, objectName);
            return target != null ? target.GetComponent<T>() : null;
        }
    }
}
