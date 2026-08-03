#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using LarisID;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace IntegratedApps.Editor
{
    public static class MainLarisIDSceneIntegrator
    {
        const int IntegrationVersion = 3;
        const string InstagramYoutubeIconPath = "Assets/assetTama2/igYT.png";
        const string TikTokIconPath = "Assets/assetTama2/TIKTOK.ICON.png";

        [MenuItem("Tools/BRIDA/Integrate Laris.ID Into Main Scene")]
        public static void IntegrateCurrentMainScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
                throw new System.InvalidOperationException("Tidak ada scene aktif yang dapat diintegrasikan.");

            GameObject uiRoot = scene.GetRootGameObjects()
                .FirstOrDefault(item => item.name == "UI");
            if (uiRoot == null)
                throw new System.InvalidOperationException("Root GameObject 'UI' tidak ditemukan di scene aktif.");

            Transform larisRoot = uiRoot.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(item =>
                    item.name == "Laris.ID" &&
                    item.GetComponentsInChildren<Transform>(true).Any(child => child.name == "BG.Laris"));
            if (larisRoot == null)
                throw new System.InvalidOperationException(
                    "Prefab instance 'Laris.ID' tidak ditemukan di bawah UI. Pastikan desain sudah berada di scene Main.");

            GameObject systemRoot = scene.GetRootGameObjects()
                .FirstOrDefault(item => item.name == "LarisID System");
            if (systemRoot == null)
            {
                systemRoot = new GameObject("LarisID System");
                Undo.RegisterCreatedObjectUndo(systemRoot, "Create LarisID System");
                SceneManager.MoveGameObjectToScene(systemRoot, scene);
            }

            LarisIDManager manager = systemRoot.GetComponent<LarisIDManager>();
            if (manager == null)
                manager = Undo.AddComponent<LarisIDManager>(systemRoot);

            LarisIDManager oldUiManager = uiRoot.GetComponent<LarisIDManager>();
            if (oldUiManager != null && oldUiManager != manager)
                Undo.DestroyObjectImmediate(oldUiManager);

            MainLarisIDWindowUI controller = uiRoot.GetComponent<MainLarisIDWindowUI>();
            if (controller == null)
                controller = Undo.AddComponent<MainLarisIDWindowUI>(uiRoot);

            controller.integrationVersion = IntegrationVersion;
            controller.manager = manager;
            controller.windowRoot = larisRoot.gameObject;
            controller.produkLMWindow = uiRoot.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(item => item.name == "produk.LM")?.gameObject;
            controller.desktopTaskbar = uiRoot.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(item => item.name.Equals("taskbar", System.StringComparison.OrdinalIgnoreCase));
            Sprite[] instagramYoutubeIcons = AssetDatabase.LoadAllAssetsAtPath(InstagramYoutubeIconPath)
                .OfType<Sprite>()
                .ToArray();
            controller.youtubePromotionIcon = instagramYoutubeIcons
                .FirstOrDefault(sprite => sprite.name == "igYT_0");
            controller.instagramPromotionIcon = instagramYoutubeIcons
                .FirstOrDefault(sprite => sprite.name == "igYT_1");
            controller.tiktokPromotionIcon = AssetDatabase.LoadAllAssetsAtPath(TikTokIconPath)
                .OfType<Sprite>()
                .FirstOrDefault();

            EditorUtility.SetDirty(manager);
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log(
                "Mekanik Laris.ID dipasang ke scene Main tanpa mengubah layout desain. " +
                "Button, input, daftar dinamis, dan scroll dibuat dari GameObject yang sudah ada saat Play Mode.",
                controller);
        }

        [MenuItem("Tools/BRIDA/Test Laris.ID Runtime Mechanics")]
        public static void RunRuntimeSmokeTest()
        {
            if (!Application.isPlaying)
                throw new System.InvalidOperationException("Masuk Play Mode sebelum menjalankan smoke test Laris.ID.");

            MainLarisIDWindowUI controller = Resources
                .FindObjectsOfTypeAll<MainLarisIDWindowUI>()
                .FirstOrDefault(item => item.gameObject.scene.IsValid());
            if (controller == null)
                throw new System.InvalidOperationException("MainLarisIDWindowUI tidak aktif di scene.");

            if (!controller.gameObject.activeSelf)
                controller.gameObject.SetActive(true);

            LarisIDManager manager = controller.manager;
            manager.EnsureInitialized();
            controller.OpenWindow();

            while (manager.Marketplace.Products.Count < 10)
                manager.AddDummyProduct();
            foreach (LarisProduct product in manager.Marketplace.Products)
                manager.Marketplace.Publish(product, out _);

            for (int i = manager.Marketplace.DailyHistory.Count; i < 5; i++)
                manager.SimulateOneDay();

            IReadOnlyList<PromoterOffer> offers = manager.Marketplace.GetDailyPromotionOffers();
            int buttons = controller.windowRoot.GetComponentsInChildren<Button>(true).Length;
            int inputs = controller.windowRoot.GetComponentsInChildren<TMP_InputField>(true).Length;
            int scrolls = controller.windowRoot.GetComponentsInChildren<ScrollRect>(true).Length;

            Transform descriptionPanel = controller.windowRoot.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(item => item.name == "panel.deskripsi");
            TMP_Text descriptionBody = descriptionPanel?.GetComponentsInChildren<TMP_Text>(true)
                .FirstOrDefault(item => item.name == "Teks Deskripsi");
            Transform descriptionFrame = descriptionPanel?.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(item => item.name == "frameDeskripsi");
            TMP_Text descriptionTitle = descriptionFrame?.GetComponentInChildren<TMP_Text>(true);
            bool descriptionTargetWorks =
                descriptionBody != null &&
                descriptionTitle != null &&
                descriptionBody != descriptionTitle;

            Transform desktopProductApp = controller.transform.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(item => item.name == "ProdukLM App");
            Button desktopProductButton = desktopProductApp?.GetComponent<Button>();
            bool productAppWorks = desktopProductButton != null;
            if (productAppWorks)
            {
                controller.OpenWindow();
                desktopProductButton.onClick.Invoke();
                productAppWorks =
                    controller.produkLMWindow != null &&
                    controller.produkLMWindow.activeSelf &&
                    !controller.windowRoot.activeSelf;
                controller.produkLMWindow.SetActive(false);
                controller.OpenWindow();
            }

            Transform exitTransform = controller.windowRoot.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(item => item.name == "Icon.Exit");
            Button exitButton = exitTransform?.GetComponentsInChildren<Button>(true).LastOrDefault();
            bool exitWorks = exitButton != null;
            if (exitWorks)
            {
                exitButton.onClick.Invoke();
                exitWorks = !controller.windowRoot.activeSelf;
                controller.OpenWindow();
            }

            bool valid =
                offers.Count >= 6 && offers.Count <= 8 &&
                offers.All(item => item.platform == PromotionPlatform.YouTube ||
                                   item.platform == PromotionPlatform.Instagram ||
                                   item.platform == PromotionPlatform.TikTok) &&
                manager.Marketplace.DailyHistory.Count == 5 &&
                buttons >= 20 && inputs >= 4 && scrolls >= 3 &&
                descriptionTargetWorks && productAppWorks && exitWorks;
            if (!valid)
                throw new System.InvalidOperationException(
                    $"Smoke test gagal: offers={offers.Count}, history={manager.Marketplace.DailyHistory.Count}, " +
                    $"buttons={buttons}, inputs={inputs}, scrolls={scrolls}, " +
                    $"description={descriptionTargetWorks}, productApp={productAppWorks}, exit={exitWorks}.");

            Debug.Log(
                $"Laris.ID smoke test berhasil: {manager.Marketplace.Products.Count} produk, " +
                $"{manager.Marketplace.DailyHistory.Count} hari, {offers.Count} promotor, " +
                $"{buttons} tombol, {inputs} input, {scrolls} area scroll.",
                controller);
        }

        [MenuItem("Tools/BRIDA/Test Laris.ID Runtime Mechanics", true)]
        static bool ValidateRuntimeSmokeTest() => Application.isPlaying;

    }
}
#endif
