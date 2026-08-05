using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProdukLM
{
    // Taruh di GameObject panel kanan (card library). Butuh referensi ke
    // container (misal punya GridLayoutGroup) dan prefab kartu (pakai CardUI.cs).
    public class CardLibraryManager : MonoBehaviour
    {
        public Transform cardContainer;
        public CardUI cardPrefab;
        public CardData[] allCards; // isi semua CardData di sini lewat Inspector, atau load dari Resources

        ProjectFlowManager flow;
        GameObject savedMessage;
        Button backToStartButton;

        void OnEnable()
        {
            ProjectFlowManager.OnInstanceReady += Bind;
            Bind(GetComponentInParent<ProjectFlowManager>(true) ?? ProjectFlowManager.Instance);
        }

        void OnDisable()
        {
            ProjectFlowManager.OnInstanceReady -= Bind;
            Unbind();
        }

        void OnDestroy()
        {
            if (backToStartButton != null)
                backToStartButton.onClick.RemoveListener(BackToStart);
        }

        void Bind(ProjectFlowManager manager)
        {
            if (manager == null || manager == flow)
                return;

            Unbind();
            flow = manager;
            flow.OnSlotChanged += Refresh;
            Refresh();
        }

        void Unbind()
        {
            if (flow != null)
                flow.OnSlotChanged -= Refresh;

            flow = null;
        }

        void Refresh()
        {
            if (flow == null)
                return;

            var state = flow.State;
            var nextSlot = state.GetNextEmptySlot();
            ClearCards();

            EnsureSavedConfirmation();
            bool showSaved = flow.LastResultSaved;
            if (savedMessage != null)
                savedMessage.SetActive(showSaved);
            if (backToStartButton != null)
                backToStartButton.gameObject.SetActive(showSaved);

            if (showSaved)
                return;

            if (nextSlot == null)
                return; // semua slot sudah terisi

            if (cardContainer == null || cardPrefab == null)
            {
                Debug.LogError(
                    $"{nameof(CardLibraryManager)} pada '{name}' belum memiliki container atau prefab kartu.",
                    this);
                return;
            }

            var relevantCards = (allCards ?? System.Array.Empty<CardData>())
                .Where(c => c != null && c.slotType == nextSlot.Value);
            foreach (var card in relevantCards)
            {
                var instance = Instantiate(cardPrefab, cardContainer);
                instance.gameObject.SetActive(true);
                instance.SetData(card);
            }
        }

        void EnsureSavedConfirmation()
        {
            if (cardContainer == null || savedMessage != null)
                return;

            Transform existingMessage = cardContainer.Find("ProdukTersimpanMessage");
            Transform existingButton = cardContainer.Find("KembaliKeTahap1Button");
            if (existingMessage != null && existingButton != null)
            {
                savedMessage = existingMessage.gameObject;
                if (!existingButton.TryGetComponent(out backToStartButton))
                    backToStartButton = existingButton.gameObject.AddComponent<Button>();
                if (backToStartButton.targetGraphic == null &&
                    existingButton.TryGetComponent(out Graphic buttonGraphic))
                    backToStartButton.targetGraphic = buttonGraphic;
                backToStartButton.onClick.RemoveListener(BackToStart);
                backToStartButton.onClick.AddListener(BackToStart);
                return;
            }

            TMP_FontAsset font = flow.slotAndLibraryPanel
                .GetComponentsInChildren<TMP_Text>(true)
                .Select(text => text.font)
                .FirstOrDefault(candidate => candidate != null);

            savedMessage = new GameObject(
                "ProdukTersimpanMessage",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            savedMessage.transform.SetParent(cardContainer, false);
            TMP_Text message = savedMessage.GetComponent<TMP_Text>();
            message.text = "PRODUK SUDAH TERSIMPAN KE LARIS.ID";
            message.font = font;
            message.fontSize = 20f;
            message.color = Color.white;
            message.alignment = TextAlignmentOptions.Center;
            message.textWrappingMode = TextWrappingModes.Normal;

            GameObject buttonObject = new GameObject(
                "KembaliKeTahap1Button",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(cardContainer, false);
            Image background = buttonObject.GetComponent<Image>();
            background.color = new Color(0.12f, 0.29f, 0.52f, 1f);
            backToStartButton = buttonObject.GetComponent<Button>();
            backToStartButton.targetGraphic = background;
            backToStartButton.onClick.AddListener(BackToStart);

            GameObject labelObject = new GameObject(
                "Text",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(buttonObject.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            TMP_Text label = labelObject.GetComponent<TMP_Text>();
            label.text = "KEMBALI KE TAHAP 1";
            label.font = font;
            label.fontSize = 16f;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;

            savedMessage.SetActive(false);
            buttonObject.SetActive(false);
        }

        void BackToStart()
        {
            flow?.BackToStart();
        }

        void ClearCards()
        {
            if (cardContainer == null)
                return;

            foreach (Transform child in cardContainer)
            {
                // Dekorasi milik layout desainer tetap aman bila nanti
                // ditambahkan ke container yang sama.
                if (child.GetComponent<CardUI>() == null)
                    continue;

                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
        }
    }
}
