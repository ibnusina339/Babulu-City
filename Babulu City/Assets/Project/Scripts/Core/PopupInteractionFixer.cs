using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BabuluCity.Core
{
    /// <summary>
    /// Merapikan area klik seluruh popup konfirmasi.
    ///
    /// Penyebab tombol salah tertekan: label di dalam tombol jauh lebih lebar
    /// daripada tombolnya sendiri. "Kembali Button" berukuran 100x33 (skala
    /// 0,355) tetapi anak teksnya berukuran 200x50 tanpa skala, sehingga teks
    /// itu menjulur menutupi tombol di sebelahnya. Karena Raycast Target pada
    /// TMP aktif secara default dan tombol aksi berada di urutan sibling lebih
    /// akhir, klik di pinggir tombol Kembali justru mengenai teks milik tombol
    /// aksi lalu diteruskan ke tombol aksi tersebut.
    ///
    /// Komponen ini tidak membuat UI baru, tidak mengganti nama GameObject, dan
    /// tidak memindahkan tata letak. Yang dilakukan hanya mematikan Raycast
    /// Target pada gambar/teks dekoratif, mematikan Navigation tombol, dan
    /// memastikan popup memblokir input ke UI di belakangnya.
    /// </summary>
    [DefaultExecutionOrder(500)]
    public sealed class PopupInteractionFixer : MonoBehaviour
    {
        [Tooltip("Nama container tombol pada popup. Parent dari objek ini dianggap sebagai satu popup.")]
        [SerializeField] string[] buttonContainerNames = { "MainBOX", "KembaliBOX" };

        [Tooltip("Graphic yang tetap menerima raycast karena tugasnya memblokir klik ke belakang.")]
        [SerializeField] string[] blockerNames = { "greyBG" };

        [Tooltip("Menulis peringatan bila ada dua tombol bersebelahan yang RectTransform-nya benar-benar bertumpuk.")]
        [SerializeField] bool warnOnOverlappingButtons = true;

        void Start()
        {
            Apply();
        }

        /// <summary>Dapat dipanggil ulang bila ada popup yang dibuat saat runtime.</summary>
        public void Apply()
        {
            foreach (Transform popupRoot in FindPopupRoots())
            {
                foreach (Button button in popupRoot.GetComponentsInChildren<Button>(true))
                    DisableNavigation(button);

                MuteDecorativeGraphics(popupRoot);
                EnsureBlocksRaycasts(popupRoot);

                if (warnOnOverlappingButtons)
                    WarnOnOverlap(popupRoot);
            }
        }

        /// <summary>
        /// Popup dikenali dari container tombolnya, lalu yang diproses adalah
        /// parent container tersebut supaya greyBG dan judul ikut tercakup.
        /// </summary>
        HashSet<Transform> FindPopupRoots()
        {
            var roots = new HashSet<Transform>();
            foreach (Transform candidate in FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (candidate is not RectTransform || candidate.parent == null)
                    continue;

                foreach (string containerName in buttonContainerNames)
                {
                    if (!candidate.name.Equals(containerName, System.StringComparison.OrdinalIgnoreCase))
                        continue;
                    roots.Add(candidate.parent);
                    break;
                }
            }
            return roots;
        }

        static void DisableNavigation(Button button)
        {
            // Tanpa ini tombol tetap bisa terpilih lewat arah keyboard dan ikut
            // aktif ketika pemain menekan Enter atau Space.
            Navigation navigation = button.navigation;
            if (navigation.mode == Navigation.Mode.None)
                return;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
        }

        /// <summary>
        /// Gambar dan teks hiasan tidak perlu menangkap klik. Yang tetap aktif
        /// hanya Graphic milik Selectable, target graphic-nya, dan peredam layar.
        /// </summary>
        void MuteDecorativeGraphics(Transform popupRoot)
        {
            var interactive = new HashSet<Graphic>();
            foreach (Selectable selectable in popupRoot.GetComponentsInChildren<Selectable>(true))
            {
                if (selectable.targetGraphic != null)
                    interactive.Add(selectable.targetGraphic);
                if (selectable.TryGetComponent(out Graphic own))
                    interactive.Add(own);
            }

            foreach (Graphic graphic in popupRoot.GetComponentsInChildren<Graphic>(true))
            {
                if (!graphic.raycastTarget || interactive.Contains(graphic) || IsBlocker(graphic.name))
                    continue;
                graphic.raycastTarget = false;
            }
        }

        bool IsBlocker(string objectName)
        {
            foreach (string blocker in blockerNames)
                if (objectName.Equals(blocker, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        /// <summary>
        /// CanvasGroup memastikan popup yang aktif menerima klik sepenuhnya dan
        /// popup di belakangnya tidak ikut menerima input.
        /// </summary>
        static void EnsureBlocksRaycasts(Transform popupRoot)
        {
            if (!popupRoot.TryGetComponent(out CanvasGroup group))
                group = popupRoot.gameObject.AddComponent<CanvasGroup>();
            group.interactable = true;
            group.blocksRaycasts = true;
        }

        /// <summary>
        /// Tata letak tidak diubah otomatis supaya desain tetap milik desainer.
        /// Bila ada tombol yang benar-benar bertumpuk, cukup dilaporkan agar
        /// dapat dirapikan langsung di Editor.
        /// </summary>
        static void WarnOnOverlap(Transform popupRoot)
        {
            foreach (Transform container in popupRoot)
            {
                var buttons = new List<RectTransform>();
                foreach (Transform child in container)
                    if (child is RectTransform rect && child.TryGetComponent(out Button _))
                        buttons.Add(rect);

                for (int i = 0; i < buttons.Count; i++)
                {
                    for (int j = i + 1; j < buttons.Count; j++)
                    {
                        if (!Overlaps(buttons[i], buttons[j]))
                            continue;
                        Debug.LogWarning(
                            $"Tombol '{buttons[i].name}' dan '{buttons[j].name}' pada popup " +
                            $"'{popupRoot.name}' memiliki RectTransform yang bertumpuk. " +
                            "Geser salah satunya di Editor agar area kliknya tidak berbagi.",
                            buttons[i]);
                    }
                }
            }
        }

        static bool Overlaps(RectTransform a, RectTransform b)
        {
            // Ukuran efektif memperhitungkan localScale tombol.
            float aHalfX = a.rect.width * Mathf.Abs(a.localScale.x) * 0.5f;
            float bHalfX = b.rect.width * Mathf.Abs(b.localScale.x) * 0.5f;
            float aHalfY = a.rect.height * Mathf.Abs(a.localScale.y) * 0.5f;
            float bHalfY = b.rect.height * Mathf.Abs(b.localScale.y) * 0.5f;

            return Mathf.Abs(a.anchoredPosition.x - b.anchoredPosition.x) < aHalfX + bHalfX &&
                   Mathf.Abs(a.anchoredPosition.y - b.anchoredPosition.y) < aHalfY + bHalfY;
        }
    }

    static class PopupInteractionFixerBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap() => SceneBootstrap.RunOnEverySceneLoad(Install);

        static void Install()
        {
            if (Object.FindAnyObjectByType<PopupInteractionFixer>(FindObjectsInactive.Include) != null)
                return;
            new GameObject("Popup Interaction Fixer").AddComponent<PopupInteractionFixer>();
        }
    }
}
