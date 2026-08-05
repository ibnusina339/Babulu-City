using System;
using System.Collections;
using System.Collections.Generic;
using BabuluCity.Core;
using BabuluCity.SaveSystem;
using IntegratedApps;
using LarisID;
using ProdukLM;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace BabuluCity.Tutorial
{
    /// <summary>
    /// Menjalankan tutorial hari pertama memakai panel chat yang sudah dibuat
    /// desainer di dalam GameObject "TutorialBox". Panel dicari lewat angka di
    /// depan namanya ("1. Tahap Greeting" sampai "23. Cannot Leave"), jadi teks,
    /// ekspresi maskot, dan posisinya tetap milik scene tanpa perlu di-drag ke
    /// Inspector.
    ///
    /// Aturan umum tiap langkah:
    /// - Spasi menutup chat. Langkah yang tidak menunggu aksi apa pun langsung
    ///   pindah ke chat berikutnya.
    /// - Selama menunggu aksi pemain, chat yang sama muncul lagi setiap
    ///   <see cref="reminderSeconds"/> detik.
    /// - Bila pemain memaksa keluar dari layar yang sedang dipandu, chat
    ///   "23. Cannot Leave" muncul dan seluruh hitungan pengingat berhenti
    ///   supaya chat tutorial tidak menumpuk.
    ///
    /// Komponen ini hanya membaca status sistem lain (laptop, ProdukLM,
    /// Laris.ID, jam permainan) sehingga alur permainan tidak berubah.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FirstDayTutorialController : MonoBehaviour
    {
        /// <summary>Layar yang wajib tetap terbuka selama satu langkah berjalan.</summary>
        enum Requirement
        {
            None,
            Laptop,
            ProdukLM,
            LarisID
        }

        sealed class Step
        {
            public int panel;
            public Requirement requirement = Requirement.None;

            /// <summary>Tampilkan chat "Cannot Leave" bila layarnya ditinggalkan.</summary>
            public bool warnWhenLeaving;

            /// <summary>Ulangi chat selama pemain belum melakukan aksinya.</summary>
            public bool remind = true;

            /// <summary>Jeda sebelum chat muncul pertama kali.</summary>
            public float showDelay;

            /// <summary>Syarat pindah ke chat berikutnya. Null berarti cukup spasi.</summary>
            public Func<bool> advanceWhen;

            /// <summary>
            /// Syarat chat boleh tampil. Saat bernilai false chat disembunyikan
            /// sementara tanpa peringatan, lalu muncul lagi begitu true.
            /// </summary>
            public Func<bool> hold;
        }

        const int WarningPanel = 23;

        [Header("Pengaturan")]
        [Tooltip("Jeda sebelum chat tutorial yang sama ditampilkan ulang.")]
        [SerializeField, Min(1f)] float reminderSeconds = 15f;
        [Tooltip("Jeda sebelum chat muncul dari layar kosong. Chat yang langsung " +
                 "menggantikan chat lain, misalnya rangkaian 13-14-15, tidak memakai jeda ini.")]
        [SerializeField, Min(0f)] float appearDelay = 1f;
        [Tooltip("Jeda sebelum chat perkenalan UI muncul setelah pemain membuka laptop.")]
        [SerializeField, Min(0f)] float laptopIntroDelay = 1f;
        [Tooltip("Jeda sebelum chat pertama muncul, menunggu save game selesai dimuat.")]
        [SerializeField, Min(0f)] float startupDelay = 0.75f;
        [Tooltip("Lama chat 'Cannot Leave' tampil sebelum kembali ke chat langkah yang sedang berjalan.")]
        [SerializeField, Min(0.5f)] float warningSeconds = 4f;
        [Tooltip("Cetak perpindahan langkah ke Console untuk pengecekan.")]
        [SerializeField] bool logSteps;

        readonly Dictionary<int, GameObject> panels = new();
        List<Step> steps;

        int stepIndex;
        int shownPanel;
        int pendingPanel;
        float pendingDelay;
        float hiddenSeconds;
        float warningSecondsLeft;
        bool panelVisible;
        bool warningVisible;
        bool holdSuspended;
        bool popupSuspended;
        bool running;
        int startDayNumber = 1;

        bool movementLocked;

        LaptopProximityController laptop;
        MainProdukLMWindowUI produkWindow;
        MainLarisIDWindowUI larisWindow;
        LarisIDManager larisManager;
        ProdukLMGenerationLoadingUI generationLoading;
        GameClockUI clock;
        PlayerMovement player;
        GameObject larisProductsPage;
        GameObject larisPromotionPage;
        GameObject sleepScreenRoot;
        GameObject sleepBlackScreen;
        GameObject calendarScreen;
        GameObject backToStartPopup;

        void Awake()
        {
            CollectPanels();
            HideAllPanels();
            BuildSteps();
        }

        IEnumerator Start()
        {
            // Save game dipulihkan satu frame setelah scene dimuat, jadi nomor
            // hari baru boleh dibaca sesudah proses itu selesai.
            float waited = 0f;
            while (waited < startupDelay || GameSaveManager.IsRestoring)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            if (panels.Count == 0)
            {
                Debug.LogWarning(
                    "Tutorial hari pertama dimatikan: panel chat di dalam 'TutorialBox' tidak ditemukan.",
                    this);
                enabled = false;
                yield break;
            }

            sleepScreenRoot = FindSceneObject("Sleep Screen");
            sleepBlackScreen = FindSceneObject("Sleep Black Screen");
            calendarScreen = FindSceneObject("Kalender Screen");
            backToStartPopup = FindSceneObject("BacktoStartScreen");

            startDayNumber = Clock != null ? Clock.CurrentDayNumber : 1;
            if (startDayNumber > 1)
            {
                // Tutorial hanya berlaku pada hari pertama.
                enabled = false;
                yield break;
            }

            running = true;
            stepIndex = 0;
            // Mulai sekarang laptop dan tombol exit aplikasi menanyakan izin
            // tutorial sebelum benar-benar menutup layar.
            TutorialGuard.Handler = HandleGuardedAction;
            BeginStep();
        }

        void OnDisable()
        {
            if (TutorialGuard.Handler == HandleGuardedAction)
                TutorialGuard.Handler = null;

            running = false;
            UpdateMovementLock();
        }

        /// <summary>
        /// Pemain berhenti berjalan selama satu chat masih tampil, jadi chatnya
        /// harus ditutup dengan spasi dulu. Penguncian hanya dipasang ketika
        /// pemain memang sedang bebas bergerak, dan hanya dilepas kalau tidak
        /// ada layar lain yang juga sedang menahannya.
        /// </summary>
        void LateUpdate() => UpdateMovementLock();

        void UpdateMovementLock()
        {
            bool heldByOthers = MovementHeldByOtherScreen;
            bool shouldLock = running && shownPanel != 0 && !heldByOthers;
            if (shouldLock == movementLocked)
                return;

            PlayerMovement movement = Player;
            if (movement == null)
                return;

            movementLocked = shouldLock;
            if (shouldLock)
            {
                movement.StopMovement();
                return;
            }

            // Laptop, popup keluar, layar tidur, dan kalender memakai penahan
            // gerak yang sama. Jangan mengembalikan gerak milik mereka.
            if (!heldByOthers)
                movement.ResumeMovement();
        }

        bool MovementHeldByOtherScreen =>
            LaptopOpen ||
            IsActive(backToStartPopup) ||
            IsActive(sleepScreenRoot) ||
            IsActive(calendarScreen);

        /// <summary>
        /// Dipanggil laptop dan window aplikasi sebelum menutup atau berpindah
        /// aplikasi. Mengembalikan true berarti aksinya ditahan dan chat
        /// "Cannot Leave" ditampilkan.
        /// </summary>
        bool HandleGuardedAction(TutorialAction action)
        {
            if (!running)
                return false;

            Step step = steps[stepIndex];
            if (!step.warnWhenLeaving || !IsActionBlocked(step.requirement, action))
                return false;

            // Pengaman: bila tugas langkah ini sudah tidak mungkin diselesaikan
            // hari ini, pemain tetap boleh keluar supaya tidak terkunci.
            if (!StepStillAchievable(step))
                return false;

            ShowWarning();
            return true;
        }

        /// <summary>
        /// Menahan pemain hanya sah selama tugasnya masih bisa dituntaskan.
        /// Kuota produksi harian yang habis atau saldo promosi yang kurang
        /// membuat penjagaan dilepas.
        /// </summary>
        bool StepStillAchievable(Step step)
        {
            switch (step.panel)
            {
                case 6:
                case 7:
                case 8:
                case 9:
                    ProjectFlowManager flow = ProjectFlowManager.Instance;
                    return flow == null || flow.HasGeneratedResult || flow.CanCreateProductToday;

                case 17:
                    return AnyAffordablePromotion();

                default:
                    return true;
            }
        }

        bool AnyAffordablePromotion()
        {
            LarisMarketplaceService shop = Shop;
            if (shop == null)
                return true;

            foreach (PromoterOffer offer in shop.GetDailyPromotionOffers())
            {
                if (offer.cost <= shop.Balance)
                    return true;
            }

            return false;
        }

        static bool IsActionBlocked(Requirement requirement, TutorialAction action)
        {
            return requirement switch
            {
                // Pemain masih harus berada di desktop laptop.
                Requirement.Laptop => action == TutorialAction.CloseLaptop,

                // Tugasnya ada di ProdukLM. Selain menutup laptop dan menutup
                // ProdukLM, pindah ke Laris.ID dan membuka ulang ProdukLM lewat
                // ikon desktop juga ditahan karena keduanya mengosongkan progres
                // yang sedang disusun pemain.
                Requirement.ProdukLM => action == TutorialAction.CloseLaptop ||
                                        action == TutorialAction.CloseProdukLM ||
                                        action == TutorialAction.OpenProdukLM ||
                                        action == TutorialAction.OpenLarisID,

                Requirement.LarisID => action == TutorialAction.CloseLaptop ||
                                       action == TutorialAction.CloseLarisID ||
                                       action == TutorialAction.OpenLarisID ||
                                       action == TutorialAction.OpenProdukLM,

                _ => false
            };
        }

        void Update()
        {
            if (!running)
                return;

            float delta = Time.unscaledDeltaTime;
            Step step = steps[stepIndex];

            // Popup keluar game boleh muncul kapan saja. Chat tutorial disimpan
            // dulu, lalu diulang begitu popupnya ditutup.
            if (IsActive(backToStartPopup))
            {
                HideStepPanel();
                popupSuspended = true;
                hiddenSeconds = 0f;
                return;
            }

            if (popupSuspended)
            {
                popupSuspended = false;
                ShowStepPanel();
            }

            // Chat yang sedang menunggu jedanya belum boleh diganggu langkah lain.
            if (pendingPanel != 0)
            {
                pendingDelay -= delta;
                if (pendingDelay > 0f)
                    return;

                ResolvePendingPanel();
                return;
            }

            if (!RequirementSatisfied(step.requirement))
            {
                if (step.warnWhenLeaving)
                    ShowWarning();
                else
                    HideStepPanel();
                hiddenSeconds = 0f;
                return;
            }

            // Chat "Cannot Leave" ditahan sebentar, lalu chat langkah yang
            // sedang berjalan muncul lagi. Spasi mempercepatnya.
            if (warningVisible)
            {
                warningSecondsLeft -= delta;
                if (warningSecondsLeft > 0f && !SpacePressed())
                    return;

                ShowStepPanel();
                return;
            }

            if (step.advanceWhen != null && step.advanceWhen())
            {
                Advance();
                return;
            }

            if (step.hold != null && !step.hold())
            {
                HideStepPanel();
                holdSuspended = true;
                hiddenSeconds = 0f;
                return;
            }

            if (holdSuspended)
            {
                holdSuspended = false;
                ShowStepPanel();
            }

            if (SpacePressed())
            {
                if (step.advanceWhen == null)
                {
                    Advance();
                    return;
                }

                if (panelVisible)
                    HideStepPanel();
            }

            if (panelVisible || !step.remind)
                return;

            hiddenSeconds += delta;
            if (hiddenSeconds >= reminderSeconds)
                ShowStepPanel();
        }

        // ------------------------------------------------------------------
        // Daftar langkah
        // ------------------------------------------------------------------

        void BuildSteps()
        {
            steps = new List<Step>
            {
                // 1-3: perkenalan di kamar sampai pemain membuka laptop.
                new Step { panel = 1 },
                new Step { panel = 2 },
                new Step { panel = 3, advanceWhen = () => LaptopOpen },

                // 4-5: perkenalan desktop lalu menunggu ProdukLM dibuka.
                new Step
                {
                    panel = 4,
                    requirement = Requirement.Laptop,
                    warnWhenLeaving = true,
                    showDelay = laptopIntroDelay
                },
                new Step
                {
                    panel = 5,
                    requirement = Requirement.Laptop,
                    warnWhenLeaving = true,
                    advanceWhen = () => ProdukLMOpen
                },

                // 6-9: alur membuat produk di ProdukLM.
                new Step
                {
                    panel = 6,
                    requirement = Requirement.ProdukLM,
                    warnWhenLeaving = true,
                    remind = false, // Tahap pilih produk tidak dihitung detiknya.
                    advanceWhen = () => ProdukLMStage2
                },
                new Step
                {
                    panel = 7,
                    requirement = Requirement.ProdukLM,
                    warnWhenLeaving = true,
                    // Proses generate memakan waktu sendiri, jadi pengingat
                    // dihentikan selama layar loading berjalan.
                    hold = () => !IsGeneratingProduct,
                    advanceWhen = () => ProdukLMStage3
                },
                new Step
                {
                    panel = 8,
                    requirement = Requirement.ProdukLM,
                    warnWhenLeaving = true
                },
                new Step
                {
                    panel = 9,
                    requirement = Requirement.ProdukLM,
                    warnWhenLeaving = true,
                    // Tombol "Buat Ulang" mengembalikan pemain ke Tahap 1.
                    // Chat 9 muncul lagi setelah hasil produk tampil kembali.
                    hold = () => ProdukLMStage3,
                    advanceWhen = () => ProductSaved
                },

                // 10: keluar dari ProdukLM lalu membuka Laris.ID.
                new Step
                {
                    panel = 10,
                    requirement = Requirement.Laptop,
                    warnWhenLeaving = true,
                    advanceWhen = () => LarisIDOpen
                },

                // 11-17: alur menjual dan mempromosikan produk di Laris.ID.
                new Step
                {
                    panel = 11,
                    requirement = Requirement.LarisID,
                    warnWhenLeaving = true,
                    advanceWhen = () => IsActive(LarisProductsPage)
                },
                new Step
                {
                    panel = 12,
                    requirement = Requirement.LarisID,
                    warnWhenLeaving = true,
                    advanceWhen = AnyProductOnSale
                },
                new Step
                {
                    panel = 13,
                    requirement = Requirement.LarisID,
                    warnWhenLeaving = true
                },
                new Step
                {
                    panel = 14,
                    requirement = Requirement.LarisID,
                    warnWhenLeaving = true
                },
                new Step
                {
                    panel = 15,
                    requirement = Requirement.LarisID,
                    warnWhenLeaving = true,
                    advanceWhen = () => IsActive(LarisPromotionPage)
                },
                new Step
                {
                    panel = 16,
                    requirement = Requirement.LarisID,
                    warnWhenLeaving = true,
                    remind = false // Langsung disambung chat 17.
                },
                new Step
                {
                    panel = 17,
                    requirement = Requirement.LarisID,
                    warnWhenLeaving = true,
                    advanceWhen = AnyProductPromoted
                },

                // 18-20: menutup laptop lalu tidur untuk pindah hari.
                new Step { panel = 18 },
                new Step { panel = 19, advanceWhen = () => !LaptopOpen },
                new Step
                {
                    panel = 20,
                    advanceWhen = NextDayReady,
                    // Chat ditahan selama layar hitam transisi tidur tampil.
                    hold = () => !IsActive(sleepBlackScreen)
                },

                // 21-22: penutup tutorial di hari berikutnya.
                new Step { panel = 21 },
                new Step { panel = 22 }
            };
        }

        // ------------------------------------------------------------------
        // Perpindahan langkah
        // ------------------------------------------------------------------

        void BeginStep()
        {
            Step step = steps[stepIndex];
            hiddenSeconds = 0f;
            holdSuspended = false;
            popupSuspended = false;
            warningVisible = false;

            if (logSteps)
                Debug.Log($"[Tutorial] Langkah {step.panel}.", this);

            ShowStepPanel();
        }

        void Advance()
        {
            stepIndex++;
            if (stepIndex >= steps.Count)
            {
                Finish();
                return;
            }

            BeginStep();
        }

        void Finish()
        {
            running = false;
            CancelPending();
            HideAllPanels();
            panelVisible = false;
            warningVisible = false;
            UpdateMovementLock();
            enabled = false;

            if (logSteps)
                Debug.Log("[Tutorial] Tutorial hari pertama selesai.", this);
        }

        // ------------------------------------------------------------------
        // Tampilan panel
        // ------------------------------------------------------------------

        void ShowStepPanel()
        {
            Step step = steps[stepIndex];
            ScheduleShow(step.panel, step.showDelay);
        }

        void HideStepPanel()
        {
            CancelPending();
            HideShownPanel();
            panelVisible = false;
            warningVisible = false;
        }

        void ShowWarning()
        {
            if (warningVisible)
            {
                warningSecondsLeft = warningSeconds;
                return;
            }

            if (pendingPanel == WarningPanel)
                return;

            ScheduleShow(WarningPanel, 0f);
        }

        /// <summary>
        /// Menjadwalkan satu chat muncul. Chat yang muncul dari layar kosong
        /// diberi jeda <see cref="appearDelay"/>, sedangkan chat yang langsung
        /// menggantikan chat lain muncul tanpa jeda supaya rangkaian beruntun
        /// tetap mengalir.
        /// </summary>
        void ScheduleShow(int number, float minimumDelay)
        {
            pendingPanel = number;
            pendingDelay = Mathf.Max(minimumDelay, shownPanel == 0 ? appearDelay : 0f);
            panelVisible = false;
            if (number != WarningPanel)
                warningVisible = false;

            // Chat lama dimatikan lebih dulu supaya jedanya benar-benar kosong.
            if (pendingDelay > 0f)
                HideShownPanel();
        }

        void ResolvePendingPanel()
        {
            int number = pendingPanel;
            CancelPending();
            ShowPanel(number);

            // Panel yang tidak ada di scene dianggap belum tampil supaya
            // pengingat tetap mencoba lagi, bukan menganggapnya selesai.
            bool shown = shownPanel == number;

            if (number == WarningPanel)
            {
                warningVisible = shown;
                warningSecondsLeft = warningSeconds;
                panelVisible = false;
                return;
            }

            panelVisible = shown;
            warningVisible = false;
            hiddenSeconds = 0f;
        }

        void CancelPending()
        {
            pendingPanel = 0;
            pendingDelay = 0f;
        }

        /// <summary>Hanya satu chat yang boleh tampil supaya tidak menumpuk.</summary>
        void ShowPanel(int number)
        {
            if (shownPanel == number)
                return;

            HideShownPanel();
            if (!panels.TryGetValue(number, out GameObject panel) || panel == null)
                return;

            panel.SetActive(true);
            shownPanel = number;
        }

        void HideShownPanel()
        {
            if (shownPanel != 0 &&
                panels.TryGetValue(shownPanel, out GameObject panel) && panel != null)
                panel.SetActive(false);

            shownPanel = 0;
        }

        void HideAllPanels()
        {
            foreach (GameObject panel in panels.Values)
            {
                if (panel != null)
                    panel.SetActive(false);
            }

            shownPanel = 0;
        }

        void CollectPanels()
        {
            Transform box = FindSceneTransform("TutorialBox");
            if (box == null)
                return;

            foreach (Transform candidate in box.GetComponentsInChildren<Transform>(true))
            {
                if (candidate == box || !TryParsePanelNumber(candidate.name, out int number))
                    continue;
                if (!panels.ContainsKey(number))
                    panels.Add(number, candidate.gameObject);
            }
        }

        /// <summary>
        /// Nama panel selalu diawali nomor urut, misalnya "10. Buat PRODUKLM pt5".
        /// </summary>
        static bool TryParsePanelNumber(string objectName, out int number)
        {
            number = 0;
            if (string.IsNullOrEmpty(objectName))
                return false;

            int digits = 0;
            while (digits < objectName.Length && char.IsDigit(objectName[digits]))
                digits++;

            if (digits == 0)
                return false;
            if (digits < objectName.Length && objectName[digits] != '.' && objectName[digits] != ' ')
                return false;

            return int.TryParse(objectName.Substring(0, digits), out number);
        }

        // ------------------------------------------------------------------
        // Pembacaan status permainan
        // ------------------------------------------------------------------

        bool RequirementSatisfied(Requirement requirement)
        {
            return requirement switch
            {
                Requirement.Laptop => LaptopOpen,
                Requirement.ProdukLM => ProdukLMOpen,
                Requirement.LarisID => LarisIDOpen,
                _ => true
            };
        }

        LaptopProximityController Laptop =>
            laptop != null
                ? laptop
                : laptop = FindAnyObjectByType<LaptopProximityController>(FindObjectsInactive.Include);

        MainProdukLMWindowUI ProdukWindow =>
            produkWindow != null
                ? produkWindow
                : produkWindow = FindAnyObjectByType<MainProdukLMWindowUI>(FindObjectsInactive.Include);

        MainLarisIDWindowUI LarisWindow =>
            larisWindow != null
                ? larisWindow
                : larisWindow = FindAnyObjectByType<MainLarisIDWindowUI>(FindObjectsInactive.Include);

        LarisIDManager LarisManager
        {
            get
            {
                if (larisManager != null)
                    return larisManager;

                MainLarisIDWindowUI window = LarisWindow;
                if (window != null && window.manager != null)
                    return larisManager = window.manager;

                return larisManager = FindAnyObjectByType<LarisIDManager>(FindObjectsInactive.Include);
            }
        }

        GameClockUI Clock =>
            clock != null
                ? clock
                : clock = FindAnyObjectByType<GameClockUI>(FindObjectsInactive.Include);

        PlayerMovement Player =>
            player != null
                ? player
                : player = FindAnyObjectByType<PlayerMovement>(FindObjectsInactive.Include);

        bool LaptopOpen => Laptop != null && Laptop.IsLaptopOpened;

        bool ProdukLMOpen
        {
            get
            {
                MainProdukLMWindowUI window = ProdukWindow;
                return window != null && IsActive(window.windowRoot);
            }
        }

        bool LarisIDOpen
        {
            get
            {
                MainLarisIDWindowUI window = LarisWindow;
                return window != null && IsActive(window.windowRoot);
            }
        }

        bool ProdukLMStage2
        {
            get
            {
                ProjectFlowManager flow = ProjectFlowManager.Instance;
                return flow != null && IsActive(flow.slotAndLibraryPanel);
            }
        }

        bool ProdukLMStage3
        {
            get
            {
                ProjectFlowManager flow = ProjectFlowManager.Instance;
                return flow != null && IsActive(flow.resultPanel) && flow.HasGeneratedResult;
            }
        }

        ProdukLMGenerationLoadingUI GenerationLoading =>
            generationLoading != null
                ? generationLoading
                : generationLoading = FindAnyObjectByType<ProdukLMGenerationLoadingUI>(
                    FindObjectsInactive.Include);

        bool IsGeneratingProduct
        {
            get
            {
                ProdukLMGenerationLoadingUI loading = GenerationLoading;
                return loading != null && loading.IsGenerating;
            }
        }

        bool ProductSaved
        {
            get
            {
                ProjectFlowManager flow = ProjectFlowManager.Instance;
                return flow != null && flow.LastResultSaved;
            }
        }

        GameObject LarisProductsPage =>
            larisProductsPage != null
                ? larisProductsPage
                : larisProductsPage = FindLarisPage("Produk.TAB");

        GameObject LarisPromotionPage =>
            larisPromotionPage != null
                ? larisPromotionPage
                : larisPromotionPage = FindLarisPage("Promosi.TAB");

        GameObject FindLarisPage(string pageName)
        {
            MainLarisIDWindowUI window = LarisWindow;
            if (window == null || window.windowRoot == null)
                return null;

            foreach (Transform candidate in window.windowRoot.GetComponentsInChildren<Transform>(true))
            {
                if (candidate.name.Equals(pageName, StringComparison.OrdinalIgnoreCase))
                    return candidate.gameObject;
            }

            return null;
        }

        bool AnyProductOnSale()
        {
            LarisMarketplaceService shop = Shop;
            if (shop == null)
                return false;

            foreach (LarisProduct product in shop.Products)
            {
                if (product.status == ProductStatus.Active)
                    return true;
            }

            return false;
        }

        bool AnyProductPromoted()
        {
            LarisMarketplaceService shop = Shop;
            if (shop == null)
                return false;

            foreach (LarisProduct product in shop.Products)
            {
                if (product.IsPromoted)
                    return true;
            }

            return false;
        }

        LarisMarketplaceService Shop
        {
            get
            {
                LarisIDManager manager = LarisManager;
                return manager != null ? manager.Marketplace : null;
            }
        }

        /// <summary>
        /// Hari sudah berganti dan layar hitam transisi tidur sudah selesai,
        /// jadi chat penutup tidak tertutup layar hitam.
        /// </summary>
        bool NextDayReady()
        {
            GameClockUI gameClock = Clock;
            if (gameClock == null || gameClock.CurrentDayNumber <= startDayNumber)
                return false;

            return !IsActive(sleepBlackScreen);
        }

        // ------------------------------------------------------------------
        // Utilitas
        // ------------------------------------------------------------------

        static bool IsActive(GameObject target) => target != null && target.activeInHierarchy;

        static bool SpacePressed()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.spaceKey.wasPressedThisFrame)
                return false;

            // Spasi saat mengetik nama produk atau harga adalah karakter biasa,
            // bukan perintah melanjutkan tutorial.
            return !IsEditingText();
        }

        static bool IsEditingText()
        {
            GameObject selected = EventSystem.current != null
                ? EventSystem.current.currentSelectedGameObject
                : null;
            if (selected == null)
                return false;

            TMP_InputField input = selected.TryGetComponent(out TMP_InputField field)
                ? field
                : selected.GetComponentInParent<TMP_InputField>();
            return input != null && input.isFocused;
        }

        static GameObject FindSceneObject(string objectName)
        {
            Transform found = FindSceneTransform(objectName);
            return found != null ? found.gameObject : null;
        }

        static Transform FindSceneTransform(string objectName)
        {
            foreach (Transform candidate in FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (candidate.name.Equals(objectName, StringComparison.OrdinalIgnoreCase))
                    return candidate;
            }

            return null;
        }
    }

    static class FirstDayTutorialBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap() => SceneBootstrap.RunOnEverySceneLoad(Install);

        static void Install()
        {
            if (UnityEngine.Object.FindAnyObjectByType<FirstDayTutorialController>(
                    FindObjectsInactive.Include) != null)
                return;

            // Hanya dipasang di scene yang memang punya panel chat tutorial.
            if (!HasTutorialBox())
                return;

            new GameObject("First Day Tutorial").AddComponent<FirstDayTutorialController>();
        }

        static bool HasTutorialBox()
        {
            foreach (Transform candidate in UnityEngine.Object.FindObjectsByType<Transform>(
                         FindObjectsInactive.Include))
            {
                if (candidate.name.Equals("TutorialBox", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
